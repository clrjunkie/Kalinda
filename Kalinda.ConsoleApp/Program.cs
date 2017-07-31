using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Kalinda.Server;
using Kalinda.Extensions;
using System.Diagnostics;

namespace Kalinda.ConsoleApp
{
    class Program
    {
        // Run VS as Administrator to create performance counters.
        static readonly HttpServerPerf HttpServerPerf = new HttpServerPerf("Kalinda");
        static int NumberOfHandledRequests;


        static void Main(string[] args)
        {
            var host = "kalinda";
            var port = 8080;
            var path = "/api";

            var httpServer = new HttpServer(host, port, false, path);

            // One "Server Task" represents all the pending async operations within a single request. 
            // In particular, each "Server Task" starts out with 1 pending async operation submitted for accepting a request.
            // This setting does not serve to limit the total number of async operations within the HTTP Server process. 

            // httpServer.MaxServerTasks = 100;

            httpServer.RequestCompleted += HttpServer_OnRequestCompleted;
            httpServer.ServerTaskCountChanged += HttpServer_OnServerTaskCountChanged;
            httpServer.OnRequest = context => OnRequest(context);

            httpServer.Start();

            Console.WriteLine($"Host:{host} Port:{port} Path:{path}");

            Console.ReadKey();

            Console.WriteLine($"Waiting for {httpServer.ServerTaskCount} Server Tasks to complete... ");

            httpServer.Stop();

            Debug.Assert(httpServer.ServerTaskCount == 0, $"{httpServer.ServerTaskCount} outstanding Server Tasks");

            Console.WriteLine($"Server Stopped. Handled {NumberOfHandledRequests} Requests");

            Console.WriteLine("Disposing...");
            // Dealing with in progress Responses is a "Hard Problem" (How long should we wait?)
            Thread.Sleep(10 * 1000);

            httpServer.Dispose();
            Console.WriteLine("Server Disposed.");

            Console.ReadKey();
        }

        private static async Task OnRequest(HttpListenerContext context)
        {
            // var input = await context.Request.ReadInputStringAsync();
            // byte[] buffer = Encoding.UTF8.GetBytes(input);

            // Simulate some work...
            await Task.Delay(150);

            await context.Response.WriteOutputStringAsync("Kalinda: Hello World!");
        }

        private static void HttpServer_OnRequestCompleted(object sender, RequestCompletedEventArgs e)
        {
            if (!e.Success)
            {
                Console.WriteLine($"Request Failed: {e.Exception.Message}");
                return;
            }

            Interlocked.Increment(ref NumberOfHandledRequests);

            HttpServerPerf.RequestCompleted(e.RequestDurationTickCount);
        }

        private static void HttpServer_OnServerTaskCountChanged(object sender, int current)
        {
            HttpServerPerf.ServerTaskCountChanged(current);
        }
    }
}