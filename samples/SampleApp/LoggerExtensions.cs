using System;
using Microsoft.Framework.Logging;

namespace SampleApp
{
    internal static class LoggerExtensions
    {
        private static Func<ILogger, string, IDisposable> _purchaceOrderScope = 
            LoggerMessage.DefineScope<string>("PO:{PurchaceOrder}");

        private static Action<ILogger, DateTimeOffset, int, Exception> _programStarting =
            LoggerMessage.Define<DateTimeOffset, int>(LogLevel.Information, 1, "Starting", "at '{StartTime}' and 0x{Hello:X} is hex of 42");

        private static Action<ILogger, DateTimeOffset, Exception> _programStopping =
            LoggerMessage.Define<DateTimeOffset>(LogLevel.Information, 2, "Stopping", "at '{StopTime}'");

        public static IDisposable PurchaceOrderScope(this ILogger logger, string purchaceOrder)
        {
            LoggerMessage.Define(LogLevel.Information, 1, "Starting", "at '{StartTime}' and 0x{Hello:X} is hex of 42", out _programStarting);

            return _purchaceOrderScope(logger, purchaceOrder);
        }

        public static void ProgramStarting(this ILogger logger, DateTimeOffset startTime, int hello, Exception exception = null)
        {
            _programStarting(logger, startTime, hello, exception);
        }

        public static void ProgramStopping(this ILogger logger, DateTimeOffset stopTime, Exception exception = null)
        {
            _programStopping(logger, stopTime, exception);
        }
    }
}

