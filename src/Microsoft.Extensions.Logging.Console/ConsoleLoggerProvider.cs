// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Console.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Console
{
    [ProviderAlias("Console")]
    public class ConsoleLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        /// <summary>
        /// 并发字典。用于缓存相同类型的 ConsoleLogger。
        /// </summary>
        private readonly ConcurrentDictionary<string, ConsoleLogger> _loggers = new ConcurrentDictionary<string, ConsoleLogger>();

        private readonly Func<string, LogLevel, bool> _filter;
        private IConsoleLoggerSettings _settings;
        private readonly ConsoleLoggerProcessor _messageQueue = new ConsoleLoggerProcessor();

        private static readonly Func<string, LogLevel, bool> trueFilter = (cat, level) => true;
        private static readonly Func<string, LogLevel, bool> falseFilter = (cat, level) => false;
        private IDisposable _optionsReloadToken;
        private bool _includeScopes;
        private bool _disableColors;
        private IExternalScopeProvider _scopeProvider;

        public ConsoleLoggerProvider(Func<string, LogLevel, bool> filter, bool includeScopes)
            : this(filter, includeScopes, false)
        {
        }

        public ConsoleLoggerProvider(Func<string, LogLevel, bool> filter, bool includeScopes, bool disableColors)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            _filter = filter;
            _includeScopes = includeScopes;
            _disableColors = disableColors;
        }

        public ConsoleLoggerProvider(IOptionsMonitor<ConsoleLoggerOptions> options)
        {
            // Filter would be applied on LoggerFactory level
            _filter = trueFilter;
            _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
            ReloadLoggerOptions(options.CurrentValue);
        }

        private void ReloadLoggerOptions(ConsoleLoggerOptions options)
        {
            _includeScopes = options.IncludeScopes;
            _disableColors = options.DisableColors;
            var scopeProvider = GetScopeProvider();
            foreach (var logger in _loggers.Values)
            {
                logger.ScopeProvider = scopeProvider;
                logger.DisableColors = options.DisableColors;
            }
        }

        public ConsoleLoggerProvider(IConsoleLoggerSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _settings = settings;
            //检查配置源是否有变，若有则回调 OnConfigurationReload 方法。
            if (_settings.ChangeToken != null)
            {
                _settings.ChangeToken.RegisterChangeCallback(OnConfigurationReload, null);
            }
        }

        private void OnConfigurationReload(object state)
        {
            try
            {
                // The settings object needs to change here, because the old one is probably holding on
                // to an old change token.
                _settings = _settings.Reload();

                _includeScopes = _settings?.IncludeScopes ?? false;

                var scopeProvider = GetScopeProvider();
                foreach (var logger in _loggers.Values)
                {
                    logger.Filter = GetFilter(logger.Name, _settings);
                    logger.ScopeProvider = scopeProvider;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error while loading configuration changes.{Environment.NewLine}{ex}");
            }
            finally
            {
                // The token will change each time it reloads, so we need to register again.
                if (_settings?.ChangeToken != null)
                {
                    _settings.ChangeToken.RegisterChangeCallback(OnConfigurationReload, null);
                }
            }
        }

        /// <summary>
        /// 创建Logger时，并不是直接创建，而是先从缓存中查找相同类型的Logger。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ILogger CreateLogger(string name)
        {
            return _loggers.GetOrAdd(name, CreateLoggerImplementation);
        }

        /// <summary>
        /// 具有相同name,includeScopes,disableColors,Filter属性的ConsoleLogger的一个实现。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private ConsoleLogger CreateLoggerImplementation(string name)
        {
            var includeScopes = _settings?.IncludeScopes ?? _includeScopes;
            var disableColors = _disableColors;

            return new ConsoleLogger(name, GetFilter(name, _settings), includeScopes? _scopeProvider: null, _messageQueue)
                {
                    DisableColors = disableColors
                };
        }

        private Func<string, LogLevel, bool> GetFilter(string name, IConsoleLoggerSettings settings)
        {
            if (_filter != null)
            {
                return _filter;
            }

            if (settings != null)
            {
                foreach (var prefix in GetKeyPrefixes(name))
                {
                    LogLevel level;
                    if (settings.TryGetSwitch(prefix, out level))
                    {
                        return (n, l) => l >= level;
                    }
                }
            }

            return falseFilter;
        }

        private IEnumerable<string> GetKeyPrefixes(string name)
        {
            while (!string.IsNullOrEmpty(name))
            {
                yield return name;
                var lastIndexOfDot = name.LastIndexOf('.');
                if (lastIndexOfDot == -1)
                {
                    yield return "Default";
                    break;
                }
                name = name.Substring(0, lastIndexOfDot);
            }
        }

        private IExternalScopeProvider GetScopeProvider()
        {
            if (_includeScopes && _scopeProvider == null)
            {
                _scopeProvider = new LoggerExternalScopeProvider();
            }
            return _includeScopes ? _scopeProvider : null;
        }

        public void Dispose()
        {
            _optionsReloadToken?.Dispose();
            _messageQueue.Dispose();
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }
    }
}
