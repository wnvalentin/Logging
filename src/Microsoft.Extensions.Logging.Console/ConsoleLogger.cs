// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions.Internal;
using Microsoft.Extensions.Logging.Console.Internal;

namespace Microsoft.Extensions.Logging.Console
{
    public class ConsoleLogger : ILogger
    {
        private static readonly string _loglevelPadding = ": ";
        /// <summary>
        /// message信息的前导空白符
        /// </summary>
        private static readonly string _messagePadding;
        private static readonly string _newLineWithMessagePadding;

        // ConsoleColor does not have a value to specify the 'Default' color
        private readonly ConsoleColor? DefaultConsoleColor = null;

        /// <summary>
        /// 采用 生产者/消费者模式 对日志信息进行处理
        /// </summary>
        private readonly ConsoleLoggerProcessor _queueProcessor;
        private Func<string, LogLevel, bool> _filter;

        [ThreadStatic]
        private static StringBuilder _logStringBuilder;

        /// <summary>
        /// Logger的名称，即日志类型Category
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 当前控制台。IConsole是与平台无关的抽象控制台，由不同平台的 core SDK 负责处理平台实现。
        /// </summary>
        public IConsole Console
        {
            get { return _queueProcessor.Console; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _queueProcessor.Console = value;
            }
        }

        /// <summary>
        /// 日志的类型和等级过滤器
        /// </summary>
        public Func<string, LogLevel, bool> Filter
        {
            get { return _filter; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _filter = value;
            }
        }
        
        /// <summary>
        /// 上下文
        /// </summary>
        [Obsolete("Changing this property has no effect. Use " + nameof(ConsoleLoggerOptions) + "." + nameof(ConsoleLoggerOptions.IncludeScopes) + " instead")]
        public bool IncludeScopes { get; set; }

        /// <summary>
        /// IExternalScopeProvider接口提供Scope对象的存储（采用单向链表）
        /// </summary>
        internal IExternalScopeProvider ScopeProvider { get; set; }

        public bool DisableColors { get; set; }


        #region 构造函数
        static ConsoleLogger()
        {
            var logLevelString = GetLogLevelString(LogLevel.Information);
            _messagePadding = new string(' ', logLevelString.Length + _loglevelPadding.Length);
            _newLineWithMessagePadding = Environment.NewLine + _messagePadding;
        }

        public ConsoleLogger(string name, Func<string, LogLevel, bool> filter, bool includeScopes)
            : this(name, filter, includeScopes ? new LoggerExternalScopeProvider() : null, new ConsoleLoggerProcessor())
        {
        }

        public ConsoleLogger(string name, Func<string, LogLevel, bool> filter, IExternalScopeProvider scopeProvider)
            : this(name, filter, scopeProvider, new ConsoleLoggerProcessor())
        {
        }

        internal ConsoleLogger(string name, Func<string, LogLevel, bool> filter, IExternalScopeProvider scopeProvider, ConsoleLoggerProcessor loggerProcessor)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Filter = filter ?? ((category, logLevel) => true);//filter为null时直接返回true，即不进行过滤
            ScopeProvider = scopeProvider;
            _queueProcessor = loggerProcessor;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console = new WindowsLogConsole();
            }
            else
            {
                Console = new AnsiLogConsole(new AnsiSystemConsole());
            }
        }
        #endregion

        /// <summary>
        /// 记录日志信息的入口
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="logLevel"></param>
        /// <param name="eventId"></param>
        /// <param name="state"></param>
        /// <param name="exception"></param>
        /// <param name="formatter"></param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))//首先判断日志等级是否被禁用
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);

            if (!string.IsNullOrEmpty(message) || exception != null)
            {
                WriteMessage(logLevel, Name, eventId.Id, message, exception);
            }
        }

        /// <summary>
        /// 构造日志信息并送入并发集合中
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="logName"></param>
        /// <param name="eventId"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public virtual void WriteMessage(LogLevel logLevel, string logName, int eventId, string message, Exception exception)
        {
            var logStringBuilder = _logStringBuilder;
            _logStringBuilder = null;

            if (logStringBuilder == null)
            {
                logStringBuilder = new StringBuilder();
            }

            var logLevelColors = default(ConsoleColors);
            var logLevelString = string.Empty;

            // Example:
            // INFO: ConsoleApp.Program[10]
            //       Request received

            logLevelColors = GetLogLevelConsoleColors(logLevel);
            logLevelString = GetLogLevelString(logLevel);
            // category and event id
            logStringBuilder.Append(_loglevelPadding);
            logStringBuilder.Append(logName);
            logStringBuilder.Append("[");
            logStringBuilder.Append(eventId);
            logStringBuilder.AppendLine("]");

            // 获取并附件上下文信息
            GetScopeInformation(logStringBuilder);

            if (!string.IsNullOrEmpty(message))
            {
                // message
                logStringBuilder.Append(_messagePadding);

                var len = logStringBuilder.Length;
                logStringBuilder.AppendLine(message);
                logStringBuilder.Replace(Environment.NewLine, _newLineWithMessagePadding, len, message.Length);
            }

            // Example:
            // System.InvalidOperationException
            //    at Namespace.Class.Function() in File:line X
            if (exception != null)
            {
                // exception message
                logStringBuilder.AppendLine(exception.ToString());
            }

            if (logStringBuilder.Length > 0)
            {
                var hasLevel = !string.IsNullOrEmpty(logLevelString);
                // Queue log message 加入日志处理队列，会被适时地打印
                _queueProcessor.EnqueueMessage(new LogMessageEntry()
                {
                    Message = logStringBuilder.ToString(),
                    MessageColor = DefaultConsoleColor,
                    LevelString = hasLevel ? logLevelString : null,
                    LevelBackground = hasLevel ? logLevelColors.Background : null,
                    LevelForeground = hasLevel ? logLevelColors.Foreground : null
                });
            }

            logStringBuilder.Clear();
            if (logStringBuilder.Capacity > 1024)
            {
                logStringBuilder.Capacity = 1024;
            }
            _logStringBuilder = logStringBuilder;
        }

        /// <summary>
        /// 判断某一等级是否被启用
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel == LogLevel.None)
            {
                return false;
            }
            //使用过滤器验证logLevel是否可用
            return Filter(Name, logLevel);
        }

        public IDisposable BeginScope<TState>(TState state) => ScopeProvider?.Push(state) ?? NullScope.Instance;

        private static string GetLogLevelString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return "trce";
                case LogLevel.Debug:
                    return "dbug";
                case LogLevel.Information:
                    return "info";
                case LogLevel.Warning:
                    return "warn";
                case LogLevel.Error:
                    return "fail";
                case LogLevel.Critical:
                    return "crit";
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }

        private ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
        {
            if (DisableColors)
            {
                return new ConsoleColors(null, null);
            }

            // We must explicitly set the background color if we are setting the foreground color,
            // since just setting one can look bad on the users console.
            switch (logLevel)
            {
                case LogLevel.Critical:
                    return new ConsoleColors(ConsoleColor.White, ConsoleColor.Red);
                case LogLevel.Error:
                    return new ConsoleColors(ConsoleColor.Black, ConsoleColor.Red);
                case LogLevel.Warning:
                    return new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black);
                case LogLevel.Information:
                    return new ConsoleColors(ConsoleColor.DarkGreen, ConsoleColor.Black);
                case LogLevel.Debug:
                    return new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black);
                case LogLevel.Trace:
                    return new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black);
                default:
                    return new ConsoleColors(DefaultConsoleColor, DefaultConsoleColor);
            }
        }

        /// <summary>
        /// 获取并附加上下文信息
        /// </summary>
        /// <param name="stringBuilder"></param>
        private void GetScopeInformation(StringBuilder stringBuilder)
        {
            var scopeProvider = ScopeProvider;
            if (scopeProvider != null)
            {
                var initialLength = stringBuilder.Length;

                scopeProvider.ForEachScope((scope, state) =>
                {
                    var (builder, length) = state;
                    var first = length == builder.Length;
                    builder.Append(first ? "--> " : " --> ").Append(scope);
                }, (stringBuilder, initialLength));

                if (stringBuilder.Length > initialLength)
                {
                    stringBuilder.Insert(initialLength, _messagePadding);
                    stringBuilder.AppendLine();
                }
            }
        }

        private readonly struct ConsoleColors
        {
            public ConsoleColors(ConsoleColor? foreground, ConsoleColor? background)
            {
                Foreground = foreground;
                Background = background;
            }

            public ConsoleColor? Foreground { get; }

            public ConsoleColor? Background { get; }
        }

        private class AnsiSystemConsole : IAnsiSystemConsole
        {
            public void Write(string message)
            {
                System.Console.Write(message);
            }

            public void WriteLine(string message)
            {
                System.Console.WriteLine(message);
            }
        }
    }
}