using Microsoft.Framework.Logging.Internal;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Microsoft.Framework.Logging
{
    public static class LoggerMessage
    {
        public static void DefineScope(out Func<ILogger, IDisposable> scope, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            scope = logger => logger.BeginScopeImpl(new LogValues(formatter));
        }

        public static void DefineScope<T1>(out Func<ILogger, T1, IDisposable> scope, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            scope = (logger, arg1) => logger.BeginScopeImpl(new LogValues<T1>(formatter, arg1));
        }

        public static void DefineScope<T1, T2>(out Func<ILogger, T1, T2, IDisposable> scope, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            scope = (logger, arg1, arg2) => logger.BeginScopeImpl(new LogValues<T1, T2>(formatter, arg1, arg2));
        }

        public static void DefineScope<T1, T2, T3>(out Func<ILogger, T1, T2, T3, IDisposable> scope, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            scope = (logger, arg1, arg2, arg3) => logger.BeginScopeImpl(new LogValues<T1, T2, T3>(formatter, arg1, arg2, arg3));
        }

        public static void Define<T1>(out Action<ILogger, T1, Exception> message, LogLevel logLevel, int eventId, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            message = (logger, arg1, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<T1>(formatter, arg1), exception, LogValues<T1>.Callback);
                }
            };
        }

        public static void Define<T1, T2>(out Action<ILogger, T1, T2, Exception> message, LogLevel logLevel, int eventId, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            message = (logger, arg1, arg2, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<T1, T2>(formatter, arg1, arg2), exception, LogValues<T1, T2>.Callback);
                }
            };
        }


        public static void Define<T1, T2, T3>(out Action<ILogger, T1, T2, T3, Exception> message, LogLevel logLevel, int eventId, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            message = (logger, arg1, arg2, arg3, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<T1, T2, T3>(formatter, arg1, arg2, arg3), exception, LogValues<T1, T2, T3>.Callback);
                }
            };
        }

        public static void Define<T1>(out Action<ILogger, T1, Exception> message, LogLevel logLevel, int eventId, string eventName, string formatString)
//        public static void Define<T1>(LogLevel logLevel, int eventId, string eventName, string formatString, out Action<ILogger, T1, Exception> message)
        {
            var formatter = new LogValuesFormatter("{EventName}: " + formatString);
            Func<object, Exception, string> callback = (state, error) => formatter.Format(((LogValues<string, T1>)state).ToArray());

            message = (logger, arg1, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<string, T1>(formatter, eventName, arg1), exception, LogValues<string, T1>.Callback);
                }
            };
        }

        public static void Define<T1, T2>(LogLevel logLevel, int eventId, string eventName, string formatString, out Action<ILogger, T1, T2, Exception> message)
        {
            var formatter = new LogValuesFormatter("{EventName}: " + formatString);
            Func<object, Exception, string> callback = (state, error) => formatter.Format(((LogValues<string, T1, T2>)state).ToArray());

            message = (logger, arg1, arg2, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<string, T1, T2>(formatter, eventName, arg1, arg2), exception, LogValues<string, T1, T2>.Callback);
                }
            };
        }

        private class LogValues : ILogValues
        {
            public static Func<object, Exception, string> Callback = (state, exception) => ((LogValues)state)._formatter.Format(((LogValues)state).ToArray());

            private static IEnumerable<KeyValuePair<string, object>> _getValues = new KeyValuePair<string, object>[0];
            private static object[] _toArray = new object[0];

            private readonly LogValuesFormatter _formatter;

            public LogValues(LogValuesFormatter formatter)
            {
                _formatter = formatter;
            }

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    throw new IndexOutOfRangeException();
                }
            }

            public int Count => 0;

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => Enumerable.Empty<KeyValuePair<string, object>>().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public object[] ToArray() => _toArray;

            public override string ToString() => _formatter.Format(ToArray());
        }

        private class LogValues<T0> : ILogValues
        {
            public static Func<object, Exception, string> Callback = (state, exception) => ((LogValues<T0>)state)._formatter.Format(((LogValues<T0>)state).ToArray());

            private readonly LogValuesFormatter _formatter;
            private readonly T0 _value0;

            public LogValues(LogValuesFormatter formatter, T0 value0)
            {
                _formatter = formatter;
                _value0 = value0;
            }

            public int Count => 2;

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0: return new KeyValuePair<string, object>(_formatter.ValueNames[0], _value0);
                        case 1: return new KeyValuePair<string, object>("{OriginalFormat}", _formatter.OriginalFormat);
                    }
                    throw new IndexOutOfRangeException();
                }
            }

            IEnumerable<KeyValuePair<string,object>> Enumerate()
            {
                yield return this[0];
                yield return this[1];
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => Enumerate().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => Enumerate().GetEnumerator();

            public object[] ToArray() => new object[] { _value0 };

            public override string ToString() => _formatter.Format(ToArray());
        }

        private class LogValues<T0, T1> : ILogValues
        {
            public static Func<object, Exception, string> Callback = (state, exception) => ((LogValues<T0, T1>)state)._formatter.Format(((LogValues<T0, T1>)state).ToArray());

            private readonly LogValuesFormatter _formatter;
            private readonly T0 _value0;
            private readonly T1 _value1;

            public LogValues(LogValuesFormatter formatter, T0 value0, T1 value1)
            {
                _formatter = formatter;
                _value0 = value0;
                _value1 = value1;
            }

            public int Count => 3;

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0: return new KeyValuePair<string, object>(_formatter.ValueNames[0], _value0);
                        case 1: return new KeyValuePair<string, object>(_formatter.ValueNames[1], _value1);
                        case 2: return new KeyValuePair<string, object>("{OriginalFormat}", _formatter.OriginalFormat);
                    }
                    throw new IndexOutOfRangeException();
                }
            }

            IEnumerable<KeyValuePair<string, object>> Enumerate()
            {
                yield return this[0];
                yield return this[1];
                yield return this[2];
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => Enumerate().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => Enumerate().GetEnumerator();

            public object[] ToArray() => new object[] { _value0, _value1 };

            public override string ToString() => _formatter.Format(ToArray());
        }

        private class LogValues<T0, T1, T2> : ILogValues
        {
            public static Func<object, Exception, string> Callback = (state, exception) => ((LogValues<T0, T1, T2>)state)._formatter.Format(((LogValues<T0, T1, T2>)state).ToArray());

            private readonly LogValuesFormatter _formatter;
            private readonly T0 _value0;
            private readonly T1 _value1;
            private readonly T2 _value2;

            public LogValues(LogValuesFormatter formatter, T0 value0, T1 value1, T2 value2)
            {
                _formatter = formatter;
                _value0 = value0;
                _value1 = value1;
                _value2 = value2;
            }

            public int Count => 4;

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0: return new KeyValuePair<string, object>(_formatter.ValueNames[0], _value0);
                        case 1: return new KeyValuePair<string, object>(_formatter.ValueNames[1], _value1);
                        case 2: return new KeyValuePair<string, object>(_formatter.ValueNames[2], _value2);
                        case 3: return new KeyValuePair<string, object>("{OriginalFormat}", _formatter.OriginalFormat);
                    }
                    throw new IndexOutOfRangeException();
                }
            }

            IEnumerable<KeyValuePair<string, object>> Enumerate()
            {
                yield return this[0];
                yield return this[1];
                yield return this[2];
                yield return this[3];
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => Enumerate().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => Enumerate().GetEnumerator();

            public object[] ToArray() => new object[] { _value0, _value1, _value2 };

            public override string ToString() => _formatter.Format(ToArray());
        }
    }
}

