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
        private readonly object _sync = new object();
        private volatile bool _disposed;
        private readonly IConfiguration _configuration;
        private IChangeToken _changeToken;
        private Dictionary<string, LogLevel> _defaultFilter;
        private Func<string, string, LogLevel, bool> _filters;
        private Dictionary<string, Func<string, LogLevel, bool>> _providerFilters = new Dictionary<string, Func<string, LogLevel, bool>>();
        private Dictionary<string, Func<string, LogLevel, bool>> _categoryFilters = new Dictionary<string, Func<string, LogLevel, bool>>();
        private static readonly Func<string, string, LogLevel, bool> _trueFilter = (providerName, category, level) => true;
        private static readonly Func<string, LogLevel, bool> _categoryTrueFilter = (n, l) => true;

        public LoggerFactory()
        {
            _filters = _trueFilter;
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
            _changeToken.RegisterChangeCallback(OnConfigurationReload, null);

            LoadDefaultConfigValues();
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(LoggerFactory));
            }

            Logger logger;
            lock (_sync)
            {
                if (!_loggers.TryGetValue(categoryName, out logger))
                {
                    Func<string, LogLevel, bool> filter = _categoryTrueFilter;
                    foreach (var prefix in GetKeyPrefixes(categoryName))
                    {
                        if (_categoryFilters.TryGetValue(prefix, out var categoryFilter))
                        {
                            var previousFilter = filter;
                            filter = (n, l) =>
                            {
                                if (previousFilter(n, l))
                                {
                                    return categoryFilter(n, l);
                                }

                                return false;
                            };
                        }
                    }
                    logger = new Logger(this, categoryName, filter);
                    _loggers[categoryName] = logger;
                }
            }

            return logger;
        }

        public void AddProvider(ILoggerProvider provider)
        {
            // REVIEW: Should we do the name resolution for our providers like this?
            var name = string.Empty;
            switch (provider.GetType().FullName)
            {
                case "Microsoft.Extensions.Logging.ConsoleLoggerProvider":
                    name = "Console";
                    break;
                case "Microsoft.Extensions.Logging.DebugLoggerProvider":
                    name = "Debug";
                    break;
                case "Microsoft.Extensions.Logging.AzureAppServices.Internal.AzureAppServicesDiagnosticsLoggerProvider":
                    name = "Azure";
                    break;
                case "Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider":
                    name = "EventLog";
                    break;
                case "Microsoft.Extensions.Logging.TraceSource.TraceSourceLoggerProvider":
                    name = "TraceSource";
                    break;
                case "Microsoft.Extensions.Logging.EventSource.EventSourceLoggerProvider":
                    name = "EventSource";
                    break;
            }

            AddProvider(name, provider);
        }

        public void AddProvider(string providerName, ILoggerProvider provider)
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(LoggerFactory));
            }

            lock (_sync)
            {
                _providers = _providers.Concat(new[] { new KeyValuePair<ILoggerProvider, string>(provider, providerName) }).ToArray();
            }
        }

        public void AddFilter(string providerName, string categoryName, Func<LogLevel, bool> filter)
        {
            foreach (var prefix in GetKeyPrefixes(categoryName))
            {
                if (_categoryFilters.TryGetValue(prefix, out var value))
                {
                    _categoryFilters[prefix] = (p, l) =>
                    {
                        if (value(p, l))
                        {
                            if (string.Equals(providerName, p))
                            {
                                return filter(l);
                            }

                            return true;
                        }

                        return false;
                    };
                }
                else
                {
                    _categoryFilters[prefix] = (p, l) =>
                    {
                        if (string.Equals(providerName, p))
                        {
                            return filter(l);
                        }

                        return true;
                    };
                }
            }
        }

        public void AddFilter(string providerName, string categoryName, LogLevel minLevel)
        {
            if (_categoryFilters.TryGetValue(categoryName, out var value))
            {
                _categoryFilters[categoryName] = (p, l) =>
                {
                    if (value(p, l))
                    {
                        if (string.Equals(providerName, p))
                        {
                            return l >= minLevel;
                        }

                        return true;
                    }

                    return false;
                };
            }
            else
            {
                _categoryFilters[categoryName] = (p, l) =>
                {
                    if (string.Equals(providerName, p))
                    {
                        return l >= minLevel;
                    }

                    return true;
                };
            }
        }

        public void AddFilter(string providerName, Func<string, LogLevel, bool> filter)
        {
            if (_providerFilters.TryGetValue(providerName, out var value))
            {
                _providerFilters[providerName] = (c, l) =>
                {
                    if (value(c, l))
                    {
                        return filter(c, l);
                    }

                    return false;
                };
            }
            else
            {
                _providerFilters[providerName] = (c, l) => filter(c, l);
            }
        }

        public void AddFilter(string providerName, Func<LogLevel, bool> filter)
        {
            if (_providerFilters.TryGetValue(providerName, out var value))
            {
                _providerFilters[providerName] = (c, l) =>
                {
                    if (value(c, l))
                    {
                        return filter(l);
                    }

                    return false;
                };
            }
            else
            {
                _providerFilters[providerName] = (c, l) => filter(l);
            }
        }

        public void AddFilter(Func<string, string, LogLevel, bool> filter)
        {
            lock (_sync)
            {
                var previousFilters = _filters;
                _filters = (providerName, category, level) =>
                {
                    if (previousFilters(providerName, category, level))
                    {
                        return filter(providerName, category, level);
                    }

                    return false;
                };
            }
        }

        public void AddFilter(IDictionary<string, LogLevel> filter)
        {
            lock (_sync)
            {
                var previousFilters = _filters;
                _filters = (providerName, category, level) =>
                {
                    if (previousFilters(providerName, category, level))
                    {
                        foreach (var prefix in GetKeyPrefixes(category))
                        {
                            if (filter.TryGetValue(prefix, out var logLevel))
                            {
                                return level >= logLevel;
                            }
                        }

                        return true;
                    }

                    return false;
                };
            }
        }

        public void AddFilter(string loggerName, IDictionary<string, LogLevel> filter)
        {
            lock (_sync)
            {
                var previousFilters = _filters;
                _filters = (providerName, category, level) =>
                {
                    if (previousFilters(providerName, category, level))
                    {
                        if (string.Equals(providerName, loggerName))
                        {
                            foreach (var prefix in GetKeyPrefixes(category))
                            {
                                if (filter.TryGetValue(prefix, out var logLevel))
                                {
                                    return level >= logLevel;
                                }
                            }
                        }

                        return true;
                    }

                    return false;
                };
            }
        }

        public void AddFilter(Func<string, bool> loggerNames, IDictionary<string, LogLevel> filter)
        {
            lock (_sync)
            {
                var previousFilters = _filters;
                _filters = (providerName, category, level) =>
                {
                    if (previousFilters(providerName, category, level))
                    {
                        if (loggerNames(providerName))
                        {
                            foreach (var prefix in GetKeyPrefixes(category))
                            {
                                if (filter.TryGetValue(prefix, out var logLevel))
                                {
                                    return level >= logLevel;
                                }
                            }
                        }

                        return true;
                    }

                    return false;
                };
            }
        }

        // TODO: Add this so AddConsole and friends can get the config to the logger?
        public IConfiguration Configuration => _configuration;

        internal KeyValuePair<ILoggerProvider, string>[] GetProviders()
        {
            return _providers;
        }

        internal bool IsEnabled(List<string> providerNames, string categoryName, LogLevel currentLevel)
        {
            //if (_filters != _trueFilter)
            {
                foreach (var providerName in providerNames)
                {
                    if (string.IsNullOrEmpty(providerName))
                    {
                        continue;
                    }

                    //if (_categoryFilters.TryGetValue(categoryName, out var categoryFilter))
                    //{
                    //    if (!categoryFilter(providerName, currentLevel))
                    //    {
                    //        return false;
                    //    }
                    //}

                    if (_providerFilters.TryGetValue(providerName, out var filter))
                    {
                        if (!filter(categoryName, currentLevel))
                        {
                            return false;
                        }
                    }

                    // filters from factory.AddFilter(...)
                    //if (!_filters(providerName, categoryName, currentLevel))
                    //{
                    //    return false;
                    //}
                }
            }

            if (_configuration != null)
            {
                // need to loop over this separately because _filters can apply to multiple providerNames
                // but the configuration prefers early providerNames and will early out if a match is found
                foreach (var providerName in providerNames)
                {
                    // TODO: Caching
                    var logLevelSection = _configuration.GetSection($"{providerName}:LogLevel");
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
            }

            if (_defaultFilter == null)
            {
                return true;
            }

            // get a local reference to the filter so that if the config is reloaded then `_defaultFilter`
            // doesn't change while we are accessing it
            var localDefaultFilter = _defaultFilter;

            // No specific filter for this logger, check defaults
            foreach (var prefix in GetKeyPrefixes(categoryName))
            {
                if (localDefaultFilter.TryGetValue(prefix, out var defaultLevel))
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
            }
            catch (Exception /*ex*/)
            {
                // TODO: Can we do anything?
                //Console.WriteLine($"Error while loading configuration changes.{Environment.NewLine}{ex}");
            }
            finally
            {
                // The token will change each time it reloads, so we need to register again.
                _changeToken.RegisterChangeCallback(OnConfigurationReload, null);
            }
        }

        private static bool TryGetSwitch(string value, out LogLevel level)
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

        private static IEnumerable<string> GetKeyPrefixes(string name)
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
            var replacementDefaultFilters = new Dictionary<string, LogLevel>();
            var logLevelSection = _configuration.GetSection("LogLevel");

            if (logLevelSection != null)
            {
                foreach (var section in logLevelSection.AsEnumerable(true))
                {
                    if (TryGetSwitch(section.Value, out var level))
                    {
                        replacementDefaultFilters[section.Key] = level;
                    }
                }
            }

            _defaultFilter = replacementDefaultFilters;
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