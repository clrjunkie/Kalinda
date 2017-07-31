using System.Configuration;

namespace Kalinda.Configuration
{
    public class HttpServerConfiguration
    {
        public string Host { get; set; }
        public string AbsolutePath { get; set; }
        public bool SslEnabled { get; set; }
        public int? Port { get; set; }
        public int? ShutdownTimeout { get; set; }
        public int MaxServerTasks { get; set; }
        public int MinServerTasks { get; set; }
        public int? DrainEntityBodyTimeout { get; set; }
        public int? EntityBodyArrivalTimeout { get; set; }
        public int? HeaderWait { get; set; }
        public int? IdleConnectionTimeout { get; set; }
        public int? RequestInQueueTimeout { get; set; }
        public long? HttpRequestQueueLength { get; set; }
        public uint? MinSendBytesPerSecond { get; set; }


        public static HttpServerConfiguration AppConfig()
        {
            var section = ConfigurationManager.GetSection("kalinda") as KalindaConfigurationSection;
            var element = section.HttpServerElement;

            var config = new HttpServerConfiguration
            {
                Host = element.Host,
                AbsolutePath = element.AbsolutePath,
                SslEnabled = element.SslEnabled,
                Port = element.Port,
                ShutdownTimeout = element.ShutdownTimeout,
                MaxServerTasks = element.MaxServerTasks,
                MinServerTasks = element.MinServerTasks,
                DrainEntityBodyTimeout = element.DrainEntityBodyTimeout,
                EntityBodyArrivalTimeout = element.EntityBodyArrivalTimeout,
                HeaderWait = element.HeaderWait,
                IdleConnectionTimeout = element.IdleConnectionTimeout,
                RequestInQueueTimeout = element.RequestInQueueTimeout,
                HttpRequestQueueLength = element.HttpRequestQueueLength,
                MinSendBytesPerSecond = element.MinSendBytesPerSecond
            };

            return config;
        }
    }
}