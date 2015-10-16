// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Logging.Test
{
    public class TestLogger : ILogger
    {
        private object _scope;
        private readonly TestSink _sink;
        private readonly string _name;
        private readonly bool _enabled;

        public TestLogger(string name, TestSink sink, bool enabled)
        {
            _sink = sink;
            _name = name;
            _enabled = enabled;
        }

        public string Name { get; set; }

        public IDisposable BeginScopeImpl(object state)
        {
            _scope = state;

            _sink.Begin(new BeginScopeContext()
            {
                LoggerName = _name,
                Scope = state,
            });

            return null;
        }

        /// <inheritdoc />
        public void Log<T>(LogLevel logLevel, int eventId, T state, Exception exception, Func<T, Exception, string> formatter)
        {
            _sink.Write(new WriteContext()
            {
                LogLevel = logLevel,
                EventId = eventId,
                State = state,
                Exception = exception,
                Formatter = (state2, exception2) => formatter((T)state2, exception2),
                LoggerName = _name,
                Scope = _scope
            });
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _enabled;
        }
    }
}