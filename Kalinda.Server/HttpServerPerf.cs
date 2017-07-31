using System.Diagnostics;

namespace Kalinda.Server
{
    public class HttpServerPerf
    {
        private static readonly object Lock = new object();
        private static readonly string CategoryName = "Kalinda Http Server";

        private readonly PerformanceCounter _httpTotalServerTasksNumberOfItemsCounter;
        private readonly PerformanceCounter _httpRequestsRateCounter;
        private readonly PerformanceCounter _httpRequestAverageDurationCounter;
        private readonly PerformanceCounter _httpRequestAverageDurationBaseCounter;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpServerPerf"/> class.
        /// </summary>
        public HttpServerPerf(string instanceName)
        {
            if (PerformanceCounterCategory.Exists(CategoryName))
                PerformanceCounterCategory.Delete(CategoryName);

            if (!PerformanceCounterCategory.Exists(CategoryName))
            {
                var ccd1 = new CounterCreationData
                {
                    CounterName = "Total Server Tasks",
                    CounterType = PerformanceCounterType.NumberOfItems32
                };

                var ccd2 = new CounterCreationData
                {
                    CounterName = "Http Requests / second",
                    CounterType = PerformanceCounterType.RateOfCountsPerSecond32
                };

                var ccd3 = new CounterCreationData
                {
                    CounterName = "Average Http Request Duration",
                    CounterType = PerformanceCounterType.AverageTimer32
                };

                var ccd4 = new CounterCreationData
                {
                    CounterName = "Average Http Request Duration Base",
                    CounterType = PerformanceCounterType.AverageBase
                };

                CounterCreationDataCollection ccdc = new CounterCreationDataCollection { ccd1, ccd2, ccd3, ccd4 };

                PerformanceCounterCategory.Create(CategoryName, "General Server Statistics", PerformanceCounterCategoryType.MultiInstance, ccdc);

                var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"System\CurrentControlSet\Services\.NETFramework\Performance");
                if (key != null)
                {
                    key.SetValue("ProcessNameFormat", 1, Microsoft.Win32.RegistryValueKind.DWord);
                    key.Close();
                }
            }

            _httpTotalServerTasksNumberOfItemsCounter = new PerformanceCounter
            {
                CategoryName = CategoryName,
                CounterName = "Total Server Tasks",
                InstanceName = instanceName,
                ReadOnly = false,
                RawValue = 0
            };

            _httpRequestsRateCounter = new PerformanceCounter
            {
                CategoryName = CategoryName,
                CounterName = "Http Requests / second",
                InstanceName = instanceName,
                ReadOnly = false,
                RawValue = 0
            };

            _httpRequestAverageDurationCounter = new PerformanceCounter
            {
                CategoryName = CategoryName,
                CounterName = "Average Http Request Duration",
                InstanceName = instanceName,
                ReadOnly = false,
                RawValue = 0
            };

            _httpRequestAverageDurationBaseCounter = new PerformanceCounter
            {
                CategoryName = CategoryName,
                CounterName = "Average Http Request Duration Base",
                InstanceName = instanceName,
                ReadOnly = false,
                RawValue = 0
            };
        }

        /// <summary>
        /// Server Task count changed.
        /// </summary>
        /// <param name="count">current count</param>
        public void ServerTaskCountChanged(int count)
        {
            try
            {
                _httpTotalServerTasksNumberOfItemsCounter.RawValue = count;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Requests the completed.
        /// </summary>
        /// <param name="ticks">The duration.</param>
        public void RequestCompleted(long ticks)
        {
            try
            {
                lock (Lock)
                {
                    _httpRequestsRateCounter.Increment();
                    _httpRequestAverageDurationCounter.IncrementBy(ticks);
                    _httpRequestAverageDurationBaseCounter.Increment();
                }
            }
            catch
            {
            }
        }
    }
}