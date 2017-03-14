// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Summary description for LoggerFactory
    /// </summary>
    public class LoggerFactory : ILoggerFactory
    {
        private readonly Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>(StringComparer.Ordinal);
        private KeyValuePair<ILoggerProvider, string>[] _providers = new KeyValuePair<ILoggerProvider, string>[0];
        private volatile bool _disposed;
        private IConfiguration _configuration;
        private IChangeToken _changeToken;
        private IDisposable _changeTokenDispose;
        private Dictionary<string, LogLevel> _defaultFilter;
        private List<KeyValuePair<Func<string, bool>, Func<string, LogLevel, bool>>> _filters;

        public LoggerFactory()
        {
            _filters = new List<KeyValuePair<Func<string, bool>, Func<string, LogLevel, bool>>>();
        }

        public LoggerFactory(IConfiguration configuration)
            : this()
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _configuration = configuration;
            _changeToken = configuration.GetReloadToken();
            _changeTokenDispose = _changeToken.RegisterChangeCallback(OnConfigurationReload, null);

            LoadDefaultConfigValues();
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(LoggerFactory));
            }

            if (!_loggers.TryGetValue(categoryName, out var logger))
            {
                logger = new Logger(this, categoryName);
                _loggers[categoryName] = logger;
            }
            return logger;
        }

        public void AddProvider(ILoggerProvider provider)
        {
            AddProvider(null, provider);
        }

        public void AddProvider(string providerName, ILoggerProvider provider)
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(LoggerFactory));
            }

            if (_loggers.Count != 0)
            {
                throw new InvalidOperationException($"Cannot call {nameof(AddProvider)} after configuring logging.");
            }

            _providers = _providers.Concat(new[] { new KeyValuePair<ILoggerProvider, string>(provider, providerName) }).ToArray();
        }

        public void AddFilter(string loggerName, Func<string, LogLevel, bool> filter)
        {
            _filters.Add(new KeyValuePair<Func<string, bool>, Func<string, LogLevel, bool>>(s => string.Equals(s, loggerName), (s, l) => filter(s, l)));//(logName, catName, level) => string.Equals(logName, loggerName) && filter(catName, level));
        }

        public void AddFilter(Func<string, bool> loggerNames, Func<string, LogLevel, bool> filter)
        {
            _filters.Add(new KeyValuePair<Func<string, bool>, Func<string, LogLevel, bool>>(s => loggerNames(s), (s, l) => filter(s, l)));//(logName, catName, level) => loggerNames(logName) && filter(catName, level));
        }

        // TODO: Add this so AddConsole and friends can get the config to the logger?
        public IConfiguration Configuration => _configuration;

        internal KeyValuePair<ILoggerProvider, string>[] GetProviders()
        {
            return _providers;
        }

        internal bool IsEnabled(IEnumerable<string> loggerNames, string categoryName, LogLevel currentLevel)
        {
            // todo: awful n*m performance, use some kind of dictionary maybe
            foreach (var filter in _filters)
            {
                foreach (var loggerName in loggerNames)
                {
                    if (filter.Key(loggerName))
                    {
                        if (filter.Value(categoryName, currentLevel))
                        {
                            // todo: do we care about the config filter?
                            return true;
                        }
                        break;
                    }
                }
            }

            // todo: apply _filters even if configuration is null
            if (_configuration == null)
            {
                return true;
            }

            foreach (var loggerName in loggerNames)
            {
                if (string.IsNullOrEmpty(loggerName))
                {
                    continue;
                }

                // TODO: Caching
                var logLevelSection = _configuration.GetSection($"{loggerName}:LogLevel");
                if (logLevelSection != null)
                {
                    foreach (var prefix in GetKeyPrefixes(categoryName))
                    {
                        if (TryGetSwitch(logLevelSection[prefix], out var configLevel))
                        {
                            return currentLevel >= configLevel;
                        }
                    }
                }
            }

            // TODO: No specific filter for this logger, check defaults
            foreach (var prefix in GetKeyPrefixes(categoryName))
            {
                if (_defaultFilter.TryGetValue(prefix, out var defaultLevel))
                {
                    return currentLevel >= defaultLevel;
                }
            }

            return true;
        }

        private void OnConfigurationReload(object state)
        {
            _changeToken = _configuration.GetReloadToken();
            try
            {
                LoadDefaultConfigValues();

                //foreach (var logger in _loggers.Values)
                //{
                //    logger.Filter = GetFilter(logger.Name, _settings);
                //    logger.IncludeScopes = _settings.IncludeScopes;
                //}
            }
            catch (Exception /*ex*/)
            {
                //Console.WriteLine($"Error while loading configuration changes.{Environment.NewLine}{ex}");
            }
            finally
            {
                // The token will change each time it reloads, so we need to register again.
                _changeTokenDispose = _changeToken.RegisterChangeCallback(OnConfigurationReload, null);
            }
        }

        private bool TryGetSwitch(string value, out LogLevel level)
        {
            if (string.IsNullOrEmpty(value))
            {
                level = LogLevel.None;
                return false;
            }
            else if (Enum.TryParse(value, out level))
            {
                return true;
            }
            else
            {
                var message = $"Configuration value '{value}' is not supported.";
                throw new InvalidOperationException(message);
            }
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

        private void LoadDefaultConfigValues()
        {
            _defaultFilter = new Dictionary<string, LogLevel>();
            var logLevelSections = new List<IConfigurationSection>
            {
                _configuration.GetSection("LogLevel"), // TODO: Remove this section; here to support 1.0 legacy config files
                _configuration.GetSection("Default:LogLevel")
            };

            foreach (var logLevelSection in logLevelSections)
            {
                if (logLevelSection != null)
                {
                    foreach (var section in logLevelSection.AsEnumerable(true))
                    {
                        if (TryGetSwitch(section.Value, out var level))
                        {
                            _defaultFilter[section.Key] = level;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if the factory has been disposed.
        /// </summary>
        /// <returns>True when <see cref="Dispose"/> as been called</returns>
        protected virtual bool CheckDisposed() => _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _changeTokenDispose?.Dispose();

                foreach (var provider in _providers)
                {
                    try
                    {
                        provider.Key.Dispose();
                    }
                    catch
                    {
                        // Swallow exceptions on dispose
                    }
                }
            }
        }
    }
}