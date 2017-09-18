// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions.Internal;

namespace Microsoft.Extensions.Logging
{
    internal class Logger : ILogger, IMetricLogger
    {
        public LoggerInformation[] Loggers { get; set; }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var loggers = Loggers;
            if (loggers == null)
            {
                return;
            }

            List<Exception> exceptions = null;
            foreach (var loggerInfo in loggers)
            {
                if (!loggerInfo.IsEnabled(logLevel))
                {
                    continue;
                }

                try
                {
                    loggerInfo.Logger.Log(logLevel, eventId, state, exception, formatter);
                }
                catch (Exception ex)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(ex);
                }
            }

            if (exceptions != null && exceptions.Count > 0)
            {
                throw new AggregateException(
                    message: "An error occurred while writing to logger(s).", innerExceptions: exceptions);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            var loggers = Loggers;
            if (loggers == null)
            {
                return false;
            }

            List<Exception> exceptions = null;
            foreach (var loggerInfo in loggers)
            {
                if (!loggerInfo.IsEnabled(logLevel))
                {
                    continue;
                }

                try
                {
                    if (loggerInfo.Logger.IsEnabled(logLevel))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(ex);
                }
            }

            if (exceptions != null && exceptions.Count > 0)
            {
                throw new AggregateException(
                    message: "An error occurred while writing to logger(s).",
                    innerExceptions: exceptions);
            }

            return false;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            var loggers = Loggers;

            if (loggers == null)
            {
                return NullScope.Instance;
            }

            if (loggers.Length == 1)
            {
                return loggers[0].Logger.BeginScope(state);
            }

            var scope = new Scope(loggers.Length);
            List<Exception> exceptions = null;
            for (var index = 0; index < loggers.Length; index++)
            {
                try
                {
                    var disposable = loggers[index].Logger.BeginScope(state);
                    scope.SetDisposable(index, disposable);
                }
                catch (Exception ex)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(ex);
                }
            }

            if (exceptions != null && exceptions.Count > 0)
            {
                throw new AggregateException(
                    message: "An error occurred while writing to logger(s).", innerExceptions: exceptions);
            }

            return scope;
        }

        public IMetric DefineMetric(string name)
        {
            // REVIEW: Cache these by name? As long as the loggers list doesn't change, they should be cachable.
            // Users are only supposed to call this once per metric name, but if they do call it twice they'll get
            // two completely different metrics with the same name, which would be bad.

            var loggers = Loggers;

            if(loggers == null)
            {
                return NullMetric.Instance;
            }

            if(loggers.Length == 1)
            {
                return loggers[0].Logger.DefineMetric(name);
            }

            // REVIEW: Unlike with Scope, we can't use a fixed-size array
            // because the number of Metric-enabled loggers is not known up front (it could be though...)
            var metrics = new List<IMetric>();
            List<Exception> exceptions = null;
            for (var index = 0; index < loggers.Length; index += 1)
            {
                try
                {
                    // REVIEW: Could use the extension method, but it likely means having an array with a number of NullMetric pointers
                    // in it. That could add up...
                    if(loggers[index].Logger is IMetricLogger metricLogger)
                    {
                        metrics.Add(metricLogger.DefineMetric(name));
                    }
                }
                catch (Exception ex)
                {
                    if(exceptions == null && exceptions.Count > 0)
                    {
                        exceptions = new List<Exception>();
                    }
                    exceptions.Add(ex);
                }
            }

            if(exceptions?.Count == 0)
            {
                throw new AggregateException(message: "An error occurred while writing to logger(s).", innerExceptions: exceptions);
            }

            return new Metric(metrics);
        }

        private class Metric : IMetric
        {
            private List<IMetric> _metrics;

            public Metric(List<IMetric> metrics)
            {
                _metrics = metrics;
            }

            public void RecordValue(double value)
            {
                for(var index = 0; index < _metrics.Count; index += 1)
                {
                    _metrics[index].RecordValue(value);
                }
            }
        }

        private class Scope : IDisposable
        {
            private bool _isDisposed;

            private IDisposable _disposable0;
            private IDisposable _disposable1;
            private readonly IDisposable[] _disposable;

            public Scope(int count)
            {
                if (count > 2)
                {
                    _disposable = new IDisposable[count - 2];
                }
            }

            public void SetDisposable(int index, IDisposable disposable)
            {
                if (index == 0)
                {
                    _disposable0 = disposable;
                }
                else if (index == 1)
                {
                    _disposable1 = disposable;
                }
                else
                {
                    _disposable[index - 2] = disposable;
                }
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    if (_disposable0 != null)
                    {
                        _disposable0.Dispose();
                    }
                    if (_disposable1 != null)
                    {
                        _disposable1.Dispose();
                    }
                    if (_disposable != null)
                    {
                        var count = _disposable.Length;
                        for (var index = 0; index != count; ++index)
                        {
                            if (_disposable[index] != null)
                            {
                                _disposable[index].Dispose();
                            }
                        }
                    }

                    _isDisposed = true;
                }
            }
        }
    }
}
