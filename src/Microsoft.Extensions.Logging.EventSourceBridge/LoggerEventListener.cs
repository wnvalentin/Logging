using System;
using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;

namespace Microsoft.Extensions.Logging
{
    internal class LoggerEventListener : EventListener
    {
        private readonly ILoggerFactory _loggerFactory;

        private ConcurrentDictionary<string, ILogger> _loggers = new ConcurrentDictionary<string, ILogger>();

        public LoggerEventListener(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            // For now, let's just import ALL THE EVENTSOURCES!
            Console.WriteLine($"Attaching to: {eventSource.Name}");
            EnableEvents(eventSource, EventLevel.Verbose);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            var logger = _loggers.GetOrAdd(eventData.EventSource.Name, name => _loggerFactory.CreateLogger(name));
            var eventName = eventData.Opcode == EventOpcode.Info ?
                eventData.EventName :
                $"{eventData.EventName}/{eventData.Opcode}";
            logger.Log(
                MapLogLevel(eventData.Level),
                new EventId(eventData.EventId, eventName),
                new EventData(eventData),
                exception: null,
                formatter: FormatEventData);
        }

        private LogLevel MapLogLevel(EventLevel level)
        {
            switch (level)
            {
                case EventLevel.Critical:
                    return LogLevel.Critical;
                case EventLevel.Error:
                    return LogLevel.Error;
                case EventLevel.Warning:
                    return LogLevel.Warning;
                case EventLevel.Informational:
                    return LogLevel.Information;
                case EventLevel.Verbose:
                    return LogLevel.Debug;
                default:
                    throw new InvalidOperationException($"Unknown EventLevel: {level}");
            }
        }

        // TODO: OMG TOARRAY I'M SORRY.
        private string FormatEventData(EventData evt, Exception ex) => string.IsNullOrEmpty(evt.Event.Message) ?
            GenerateMessage(evt) :
            string.Format(evt.Event.Message, evt.Event.Payload.ToArray());

        private string GenerateMessage(EventData evt)
        {
            var builder = new StringBuilder();
            builder.Append(evt.Event.EventName);
            builder.Append(":");
            foreach(var pair in evt)
            {
                builder.Append(pair.Key);
                builder.Append("=");
                builder.Append(pair.Value.ToString());
                builder.Append(",");
            }
            builder.Length -= 1;
            return builder.ToString();
        }
    }
}
