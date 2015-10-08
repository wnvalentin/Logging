using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Logging.Internal;

namespace BenchmarkApp
{
    public class Program
    {
        private readonly ILogger _logger;

        public Program()
        {
            _logger = new CustomLogger() { Enable = false };
        }

        public void Main(string[] args)
        {
            var requestId = Guid.NewGuid();
            var requestUrl = "http://test.com/api/values?p=10";
            var controller = "home";
            var action = "index";

            var sw = new Stopwatch();
            const int ITERS = 1000000;
            while (true)
            {
                Console.Write("A: ");
                sw.Restart();
                for (int i = 0; i < ITERS; i++)
                {
                    // Operation A
                    //_logger.LogVerbose("Request Id: {RequestId}", requestId);
                    //_logger.LogVerbose("Request Id: {RequestId} with Url {Url}", requestId, requestUrl);
                    _logger.LogVerbose("Request matched controller '{controller}' and action '{action}'.", controller, action);
                }
                var elapsedA = sw.Elapsed;
                Console.WriteLine(elapsedA);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Console.Write("B: ");
                sw.Restart();
                for (int i = 0; i < ITERS; i++)
                {
                    // Operation B
                    //_logger.RequestId(requestId);
                    //_logger.RequestIdAndUrl(requestId, requestUrl);
                    _logger.ActionMatched(controller, action);
                }
                var elapsedB = sw.Elapsed;
                Console.WriteLine(elapsedB);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Console.WriteLine("A/B     : {0}",
                    elapsedA.TotalMilliseconds /
                    elapsedB.TotalMilliseconds);
                Console.WriteLine("B/A     : {0}",
                    elapsedB.TotalMilliseconds /
                    elapsedA.TotalMilliseconds);
                Console.WriteLine("(A-B)/A : {0}",
                    Math.Abs((elapsedA.TotalMilliseconds -
                              elapsedB.TotalMilliseconds) /
                             elapsedA.TotalMilliseconds));
                Console.WriteLine("(B-A)/B : {0}",
                    Math.Abs((elapsedB.TotalMilliseconds -
                              elapsedA.TotalMilliseconds) /
                             elapsedB.TotalMilliseconds));

                Console.WriteLine();
            }
        }
    }

    public class CustomLogger : ILogger
    {
        public bool Enable { get; set; } = true;

        public IDisposable BeginScopeImpl(object state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return Enable;
        }

        public void Log(
            LogLevel logLevel, int eventId, object state,
            Exception exception, Func<object, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            //do nothing
        }
    }
}