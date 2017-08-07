using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Kalinda.Configuration;

namespace Kalinda.Server
{
    public class HttpServer
    {
        private readonly object _lock = new object();
        private readonly object _lockEventHandler = new object();

        private HttpServerState _state = HttpServerState.Stopped;

        private int _totalServerTasks;
        private int _maxServerTasks = 1000;
        private int _minServerTasks = Environment.ProcessorCount / 2;

        private int _shutdownTimeoutMilli = 120 * 1000;
        private long _httpRequestQueueLength = 1000;
        private bool _sslEnabled;
        private string _listenerUrl;
        private DnsEndPoint _dnsEndPoint;

        private readonly HttpListener _listener;

        private CountdownEvent _shutdownEvent;
        private TaskCompletionSource<int> _shutdownCompletionSource;

        private EventHandler<Exception> _listenerErrorEvent;
        private EventHandler<int> _serverTaskCountChangedEvent;
        private EventHandler<RequestCompletedEventArgs> _requestCompletedEvent;

        private Func<HttpListenerContext, Task> _OnRequest;

        // Constructors

        public HttpServer(string host, string absolutePath)
            : this(new HttpServerConfiguration { Host = host, AbsolutePath = absolutePath })
        { }

        public HttpServer(string host, int port, string absolutePath)
            : this(new HttpServerConfiguration { Host = host, Port = port, AbsolutePath = absolutePath })
        { }

        public HttpServer(string host, int port, bool sslEnabled, string absolutePath)
            : this(new HttpServerConfiguration { Host = host, Port = port, SslEnabled = sslEnabled, AbsolutePath = absolutePath })
        { }

        public HttpServer(HttpServerConfiguration config)
        {
            EnsureConfig(config);

            _listener = new HttpListener();

            ApplyConfiguration(config);

            var schema = _sslEnabled ? "https://" : "http://";
            var host = _dnsEndPoint.Host;
            var port = _dnsEndPoint.Port != 80 ? ":" + _dnsEndPoint.Port : string.Empty;

            _listenerUrl = new Uri($"{schema}{host}{port}{config.AbsolutePath}").AbsoluteUri;
            _listenerUrl += _listenerUrl[_listenerUrl.Length - 1] != '/' ? "/" : string.Empty;

            _listener.Prefixes.Add(_listenerUrl);
        }

        // Server Lifecycle

        public void Start()
        {
            EnsureHttpServerState(HttpServerState.Stopped);

            _shutdownEvent = new CountdownEvent(1);
            _shutdownCompletionSource = new TaskCompletionSource<int>();

            _listener.Start();

            HttpApi.SetRequestQueueLength(_listener, _httpRequestQueueLength);

            _totalServerTasks = _minServerTasks;

            for (var i = 0; i < _minServerTasks; ++i)
            {
                PostAcceptRequestToIOCP();
            }

            SetHttpServerState(HttpServerState.Running);
        }

        public void Stop()
        {
            EnsureHttpServerState(HttpServerState.Running);

            _shutdownCompletionSource.SetResult(0);
            _shutdownEvent.Signal();

            if (!_shutdownEvent.Wait(TimeSpan.FromMilliseconds(_shutdownTimeoutMilli)))
            {
                ThrowInvalidOperationException("Dispose timeout elapsed");
            }

            _shutdownEvent.Dispose();
            _shutdownEvent = null;

            Trace.Assert(_totalServerTasks == 0, "One or more active Accept");

            _listener.Stop();

            SetHttpServerState(HttpServerState.Stopped);
        }

        public void Dispose()
        {
            EnsureHttpServerState(HttpServerState.Stopped);

            _listener.Abort();

            SetHttpServerState(HttpServerState.Disposed);
        }

        // Request Processing

        private async void AcceptRequest()
        {
            try
            {
                var contextTask = _listener.GetContextAsync();

                await Task.WhenAny(new Task[] { contextTask, _shutdownCompletionSource.Task });

                if (_shutdownCompletionSource.Task.IsCompleted)
                {
                    lock (_lock)
                    {
                        _totalServerTasks--;
                    }

                    return;
                }

                var newAccept = false;

                lock (_lock)
                {
                    if (_totalServerTasks < _maxServerTasks)
                    {
                        _totalServerTasks++;

                        OnServerTaskCountChanged(_totalServerTasks);

                        newAccept = true;
                    }
                }

                if (newAccept)
                {
                    PostAcceptRequestToIOCP();
                }

                await ProcessRequest(contextTask.Result);

                newAccept = false;

                lock (_lock)
                {
                    if (_totalServerTasks - 1 < _minServerTasks)
                    {
                        newAccept = true;
                    }
                    else
                    {
                        OnServerTaskCountChanged(--_totalServerTasks);
                    }
                }

                if (newAccept)
                {
                    PostAcceptRequestToIOCP();
                }
            }
            catch (Exception e)
            {
                OnServerError(e);
            }
            finally
            {
                _shutdownEvent.Signal();
            }
        }

        private async Task ProcessRequest(HttpListenerContext context)
        {
            // Begin Process Request Scope

            var eventArgs = new RequestCompletedEventArgs();

            bool abort = false;

            try
            {
                if (_OnRequest != null)
                {
                    var startTime = Stopwatch.GetTimestamp();

                    await _OnRequest(context);
                    
                    eventArgs.RequestDurationTickCount = Stopwatch.GetTimestamp() - startTime;
                    eventArgs.Url = context.Request.Url;
                }
            }
            catch (HttpListenerException)
            {
                abort = true;
            }
            catch (Exception e)
            {
                eventArgs.Exception = e;

                try
                {
                    context.Response.Headers.Clear();
                    context.Response.ContentLength64 = 0;
                    context.Response.StatusCode = 500;
                }
                catch (Exception)
                {
                    abort = true;
                }
            }
            finally
            {
                try
                {
                    if (abort)
                    {
                        context.Response.Abort();
                    }
                    else
                    {
                        context.Response.Close();
                    }
                }
                catch (Exception)
                {
                }
            }

            // End Handle Request Scope

            OnRequestCompleted(eventArgs);
        }

        private unsafe void PostAcceptRequestToIOCP()
        {
            _shutdownEvent.AddCount();

            Overlapped ov = new Overlapped();
            NativeOverlapped* nov = ov.Pack(AcceptRequestIOCompletionCallback, null);

            ThreadPool.UnsafeQueueNativeOverlapped(nov);
        }

        private unsafe void AcceptRequestIOCompletionCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
        {
            Overlapped.Free(pOverlapped);

            AcceptRequest();
        }

        // Events

        public Func<HttpListenerContext, Task> OnRequest
        {
            set
            {
                EnsureHttpServerState(HttpServerState.Stopped);

                _OnRequest = value;
            }
        }

        public event EventHandler<Exception> ServerError
        {
            add
            {
                EnsureHttpServerState(HttpServerState.Stopped);

                _listenerErrorEvent += value;
            }
            remove
            {
                EnsureHttpServerState(HttpServerState.Stopped);

                if (_listenerErrorEvent != null)
                {
                    _listenerErrorEvent -= value;
                }
            }
        }

        public event EventHandler<RequestCompletedEventArgs> RequestCompleted
        {
            add
            {
                EnsureHttpServerState(HttpServerState.Stopped);

                _requestCompletedEvent += value;
            }
            remove
            {
                EnsureHttpServerState(HttpServerState.Stopped);

                if (_requestCompletedEvent != null)
                {
                    _requestCompletedEvent -= value;
                }
            }
        }

        public event EventHandler<int> ServerTaskCountChanged
        {
            add
            {
                EnsureHttpServerState(HttpServerState.Stopped);

                _serverTaskCountChangedEvent += value;
            }
            remove
            {
                EnsureHttpServerState(HttpServerState.Stopped);

                if (_serverTaskCountChangedEvent != null)
                {
                    _serverTaskCountChangedEvent -= value;
                }
            }
        }

        private void OnRequestCompleted(RequestCompletedEventArgs e)
        {
            try
            {
                _requestCompletedEvent?.Invoke(this, e);
            }
            catch (Exception)
            {
            }
        }

        private void OnServerTaskCountChanged(int count)
        {
            try
            {
                _serverTaskCountChangedEvent?.Invoke(this, count);
            }
            catch (Exception)
            {
            }
        }

        private void OnServerError(Exception e)
        {
            try
            {
                _listenerErrorEvent?.Invoke(this, e);
            }
            catch (Exception)
            {
            }
        }

        // Configuration

        private void ApplyConfiguration(HttpServerConfiguration config)
        {
            var host = config.Host ?? "localhost";
            var port = config.Port ?? 8080;

            _dnsEndPoint = new DnsEndPoint(host, port);

            if (config.SslEnabled)
            {
                _sslEnabled = true;
            }

            if (config.MinServerTasks > 0)
            {
                _minServerTasks = config.MinServerTasks;
            }
            else if (config.MinServerTasks < 0)
            {
                ThrowArgumentOutOfRangeException("MinServerTasks", "Positive Value Required");
            }

            if (config.MaxServerTasks > 0)
            {
                _maxServerTasks = config.MaxServerTasks;
            }
            else if (config.MaxServerTasks < 0)
            {
                ThrowArgumentOutOfRangeException("MaxServerTasks", "Positive Value Required");
            }

            if (_minServerTasks > _maxServerTasks)
            {
                ThrowInvalidOperationException("MinServerTasks is greater than MaxServerTasks");
            }

            if (config.ShutdownTimeout.HasValue)
            {
                _shutdownTimeoutMilli = config.ShutdownTimeout.Value;
            }

            if (config.DrainEntityBodyTimeout.HasValue)
            {
                DrainEntityBodyTimeout = TimeSpan.FromMilliseconds(config.DrainEntityBodyTimeout.Value);
            }

            if (config.EntityBodyArrivalTimeout.HasValue)
            {
                EntityBodyArrivalTimeout = TimeSpan.FromMilliseconds(config.EntityBodyArrivalTimeout.Value);
            }

            if (config.HeaderWait.HasValue)
            {
                HeaderWait = TimeSpan.FromMilliseconds(config.HeaderWait.Value);
            }

            if (config.IdleConnectionTimeout.HasValue)
            {
                IdleConnectionTimeout = TimeSpan.FromMilliseconds(config.IdleConnectionTimeout.Value);
            }

            if (config.RequestInQueueTimeout.HasValue)
            {
                RequestInQueueTimeout = TimeSpan.FromMilliseconds(config.RequestInQueueTimeout.Value);
            }

            if (config.MinSendBytesPerSecond.HasValue)
            {
                MinSendBytesPerSecond = config.MinSendBytesPerSecond.Value;
            }

            if (config.HttpRequestQueueLength.HasValue)
            {
                _httpRequestQueueLength = config.HttpRequestQueueLength.Value;
            }
        }

        public string ListenerUrl
        {
            get
            {
                return _listenerUrl;
            }
        }

        public TimeSpan DrainEntityBodyTimeout
        {
            get
            {
                return _listener.TimeoutManager.DrainEntityBody;
            }
            set
            {
                EnsureHttpServerState(HttpServerState.Stopped);

                _listener.TimeoutManager.DrainEntityBody = value;
            }
        }

        public TimeSpan EntityBodyArrivalTimeout
        {
            get
            {
                return _listener.TimeoutManager.EntityBody;
            }
            set
            {
                EnsureHttpServerState(HttpServerState.Stopped);

                _listener.TimeoutManager.EntityBody = value;
            }
        }

        public TimeSpan HeaderWait
        {
            get
            {
                return _listener.TimeoutManager.HeaderWait;
            }
            set
            {
                EnsureHttpServerState(HttpServerState.Stopped);

                _listener.TimeoutManager.HeaderWait = value;
            }
        }

        public TimeSpan IdleConnectionTimeout
        {
            get
            {
                return _listener.TimeoutManager.IdleConnection;
            }
            set
            {
                EnsureHttpServerState(HttpServerState.Stopped);

                _listener.TimeoutManager.IdleConnection = value;
            }
        }

        public long MinSendBytesPerSecond
        {
            get
            {
                return _listener.TimeoutManager.MinSendBytesPerSecond;
            }
            set
            {
                EnsureHttpServerState(HttpServerState.Stopped);

                _listener.TimeoutManager.MinSendBytesPerSecond = value;
            }
        }

        public TimeSpan RequestInQueueTimeout
        {
            get
            {
                return _listener.TimeoutManager.RequestQueue;
            }
            set
            {
                EnsureHttpServerState(HttpServerState.Stopped);

                _listener.TimeoutManager.RequestQueue = value;
            }
        }

        public long HttpRequestQueueLength
        {
            get
            {
                return _httpRequestQueueLength;
            }
            set
            {
                EnsureHttpServerState(HttpServerState.Stopped);

                _httpRequestQueueLength = value;
            }
        }

        public int MaxServerTasks
        {
            get
            {
                return _maxServerTasks;
            }
            set
            {
                EnsureHttpServerState(HttpServerState.Stopped);

                _maxServerTasks = value;
            }
        }

        public int MinServerTasks
        {
            get
            {
                return _minServerTasks;
            }
            set
            {
                EnsureHttpServerState(HttpServerState.Stopped);

                _minServerTasks = value;
            }
        }

        // State

        public int ServerTaskCount
        {
            get
            {
                lock (_lock)
                {
                    return _totalServerTasks;
                }
            }
        }

        public HttpServerState ServerState
        {
            get
            {
                lock (_lock)
                {
                    return _state;
                }
            }
        }

        // Helpers

        private void SetHttpServerState(HttpServerState state)
        {
            lock (_lock)
            {
                _state = state;
            }
        }

        private void EnsureHttpServerState(HttpServerState state)
        {
            lock (_lock)
            {
                if (_state != state)
                {
                    ThrowInvalidOperationException($"Server State: {_state}");
                }
            }
        }

        private static void EnsureConfig(HttpServerConfiguration config)
        {
            if (config == null)
            {
                throw new UriFormatException("missing config");
            }

            if (string.IsNullOrEmpty(config.AbsolutePath) || string.IsNullOrWhiteSpace(config.AbsolutePath))
            {
                throw new UriFormatException("missing absolute path");
            }

            if (!config.AbsolutePath.StartsWith("/"))
            {
                throw new UriFormatException("absolute path must start with forward slash");
            }
        }

        private void ThrowInvalidOperationException(string message)
        {
            throw new InvalidOperationException(message);
        }

        private void ThrowArgumentOutOfRangeException(string arg, string message)
        {
            throw new ArgumentOutOfRangeException(arg, message);
        }
    }
}