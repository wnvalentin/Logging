using System;
using System.Buffers;
using System.Diagnostics.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventSourceBridgeSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Trace);
                    builder.AddConsole();
                });

            // providers may be added to a LoggerFactory before any loggers are created


            var serviceProvider = serviceCollection.BuildServiceProvider();

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            loggerFactory.ImportEventSource();

            TestEventSource.Log.TestEvent("foo", 42);
            TestEventSource.Log.TestEvent("bar", 24);

            // ArrayPool has an EventSource
            var pool = ArrayPool<string>.Create();
            var pooledArray = pool.Rent(5);
            pool.Return(pooledArray);
            pooledArray = pool.Rent(5);

            Console.ReadLine();
        }

        private class TestEventSource : EventSource
        {
            internal static readonly TestEventSource Log = new TestEventSource();

            private TestEventSource()
            {
            }

            [Event(
                eventId: 1,
                Message = "The string value was {0} and the int value was {1}")]
            public void TestEvent(string stringValue, int intValue)
            {
                WriteEvent(1, stringValue, intValue);
            }
        }
    }
}
