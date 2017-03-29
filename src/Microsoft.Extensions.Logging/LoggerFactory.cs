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
        private List<Func<string, string, LogLevel, bool>> _filters;

        public LoggerFactory()
        {
            _filters = new List<Func<string, string, LogLevel, bool>>();
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
                    logger = new Logger(this, categoryName);
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

        public void AddFilter(Func<string, string, LogLevel, bool> filter)
        {
            lock (_sync)
            {
                _filters.Add(new Func<string, string, LogLevel, bool>(
                    (providerName, category, level) => filter(providerName, category, level)));
            }
        }

        public void AddFilter(IDictionary<string, LogLevel> filter)
        {
            lock (_sync)
            {
                foreach (var pair in filter)
                {
                    _filters.Add(new Func<string, string, LogLevel, bool>(
                        (providerName, category, level) =>
                        {
                            foreach (var prefix in GetKeyPrefixes(category))
                            {
                                if (string.Equals(pair.Key, prefix))
                                {
                                    return level >= pair.Value;
                                }
                            }

                            return true;
                        }));
                }
            }
        }

        public void AddFilter(string loggerName, IDictionary<string, LogLevel> filter)
        {
            lock (_sync)
            {
                foreach (var pair in filter)
                {
                    _filters.Add(new Func<string, string, LogLevel, bool>(
                        (providerName, category, level) =>
                        {
                            if (string.Equals(providerName, loggerName))
                            {
                                foreach (var prefix in GetKeyPrefixes(category))
                                {
                                    if (string.Equals(pair.Key, prefix))
                                    {
                                        return level >= pair.Value;
                                    }
                                }
                            }

                            return true;
                        }));
                }
            }
        }

        public void AddFilter(Func<string, bool> loggerNames, IDictionary<string, LogLevel> filter)
        {
            lock (_sync)
            {
                foreach (var pair in filter)
                {
                    _filters.Add(new Func<string, string, LogLevel, bool>(
                        (providerName, category, level) =>
                        {
                            if (loggerNames(providerName))
                            {
                                foreach (var prefix in GetKeyPrefixes(category))
                                {
                                    if (string.Equals(pair.Key, prefix))
                                    {
                                        return level >= pair.Value;
                                    }
                                }
                            }

                            return true;
                        }));
                }
            }
        }

        // TODO: Add this so AddConsole and friends can get the config to the logger?
        public IConfiguration Configuration => _configuration;

        internal KeyValuePair<ILoggerProvider, string>[] GetProviders()
        {
            return _providers;
        }

        internal bool IsEnabled(IEnumerable<string> providerNames, string categoryName, LogLevel currentLevel)
        {
            foreach (var providerName in providerNames)
            {
                if (string.IsNullOrEmpty(providerName))
                {
                    continue;
                }

                // filters from factory.AddFilter(...)
                foreach (var filter in _filters)
                {
                    if (!filter(providerName, categoryName, currentLevel))
                    {
                        return false;
                    }
                }

                if (_configuration == null)
                {
                    continue;
                }

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

            if (_defaultFilter == null)
            {
                return true;
            }

            // No specific filter for this logger, check defaults
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
            var logLevelSection = _configuration.GetSection("LogLevel");

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