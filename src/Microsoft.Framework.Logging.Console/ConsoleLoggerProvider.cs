// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Framework.Logging.Console
{
    public class ConsoleLoggerProvider : ILoggerProvider
    {
        private static readonly Dictionary<string, Func<string, LogLevel, bool>> _levelFilters = new Dictionary<string, Func<string, LogLevel, bool>>(StringComparer.OrdinalIgnoreCase)
        {
            {"Debug",(name, logLevel) => logLevel >= LogLevel.Debug },
            {"Verbose",(name, logLevel) => logLevel >= LogLevel.Verbose },
            {"Information",(name, logLevel) => logLevel >= LogLevel.Information },
            {"Warning",(name, logLevel) => logLevel >= LogLevel.Warning },
            {"Error",(name, logLevel) => logLevel >= LogLevel.Error },
            {"Critical",(name, logLevel) => logLevel >= LogLevel.Critical },
            {"None",(name, logLevel) => false },
        };

        private readonly Func<string, LogLevel, bool> _filter;
        private readonly IConfiguration _configuration;

        private ConcurrentDictionary<string, ConsoleLogger> _loggers = new ConcurrentDictionary<string, ConsoleLogger>();

        public ConsoleLoggerProvider(Func<string, LogLevel, bool> filter)
        {
            _filter = filter;
        }

        public ConsoleLoggerProvider(IConfiguration configuration)
        {
            _configuration = configuration;
            _configuration.GetReloadToken().RegisterChangeCallback(OnConfigurationReload, null);
        }

        private void OnConfigurationReload(object obj)
        {
            _configuration.GetReloadToken().RegisterChangeCallback(OnConfigurationReload, null);
            foreach(var logger in _loggers.Values)
            {
                logger.Filter = DetermineFilter(logger.Name);
            }
        }

        public ILogger CreateLogger(string name)
        {
            return _loggers.GetOrAdd(name, CreateLoggerImplementation);
        }

        private ConsoleLogger CreateLoggerImplementation(string name)
        {
            return new ConsoleLogger(name, DetermineFilter(name));
        }

        private Func<string, LogLevel, bool> DetermineFilter(string name)
        {
            if (_filter != null)
            {
                return _filter;
            }
            if (_configuration != null)
            {
                foreach(var lookupName in LookupNames(name))
                {
                    var value = _configuration[lookupName];
                    if (!string.IsNullOrEmpty(value))
                    {
                        Func<string, LogLevel, bool> filter;
                        if (!_levelFilters.TryGetValue(value, out filter))
                        {
                            throw new ArgumentOutOfRangeException($"Configuration value '{value}' for category '{lookupName}' not supported.");
                        }
                        return filter;
                    }
                }
            }
            return (argName, argLevel) => false;
        }

        private IEnumerable<string> LookupNames(string name)
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

        public void Dispose()
        {
        }
    }
}
