using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Provides extension methods 
    /// </summary>
    public static class EventSourceBridgeLoggerFactoryExtensions
    {
        /// <summary>
        /// Attaches a listener to the specified <see cref="EventSource"/> and pipes the events sent on that source to the logger providers.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to forward events to</param>
        /// <returns>The logger factory</returns>
        public static ILoggerFactory ImportEventSource(this ILoggerFactory loggerFactory)
        {
            // TODO: Caching/sharing the listener.
            // TODO: Support filtering event sources.
            new LoggerEventListener(loggerFactory);

            return loggerFactory;
        }
    }
}
