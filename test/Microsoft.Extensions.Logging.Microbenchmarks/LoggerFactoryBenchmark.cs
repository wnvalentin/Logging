using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Logging.Microbenchmarks
{
    [Config(typeof(CoreConfig))]
    public class LoggerFactoryBenchmark
    {
        private LoggerFactory _factory;
        private ILogger _logger;

        [Setup]
        public void Setup()
        {
            _factory = new LoggerFactory();
            //_factory.AddFilter(new Dictionary<string, LogLevel>
            //{
            //    { "bench", LogLevel.Information },
            //    //{ "Default", LogLevel.Information },
            //    //{ "bench.mark", LogLevel.Information },
            //    //{ "blah", LogLevel.Information }
            //});
            _factory.AddFilter("Console", "bench", LogLevel.Information);
            //_factory.AddFilter((n, c, l) =>
            //{
            //    if (string.Equals(n, "Console"))
            //    {
            //        return l >= LogLevel.Information;
            //    }
            //    return true;
            //});
            //_factory.AddFilter((n, c, l) =>
            //{
            //    if (c.StartsWith("bench.mark"))
            //    {
            //        return l >= LogLevel.Information;
            //    }

            //    return true;
            //});
            //_factory.AddFilter((n, c, l) =>
            //{
            //    if (c.StartsWith("bench"))
            //    {
            //        return l >= LogLevel.Information;
            //    }

            //    return true;
            //});
            //_factory.AddFilter((n, c, l) =>
            //{
            //    if (c.StartsWith("bench"))
            //    {
            //        return l >= LogLevel.Information;
            //    }

            //    return true;
            //});
            //_factory.AddFilter((n, c, l) =>
            //{
            //    if (c.StartsWith("bench"))
            //    {
            //        return l >= LogLevel.Information;
            //    }

            //    return true;
            //});
            //_factory.AddFilter((n, c, l) =>
            //{
            //    if (c.StartsWith("bench"))
            //    {
            //        return l >= LogLevel.Information;
            //    }

            //    return true;
            //});
            //_factory.AddFilter((n, c, l) =>
            //{
            //    if (c.StartsWith("bench"))
            //    {
            //        return l >= LogLevel.Information;
            //    }

            //    return true;
            //});
            //_factory.AddFilter((n, c, l) =>
            //{
            //    if (c.StartsWith("bench"))
            //    {
            //        return l >= LogLevel.Information;
            //    }

            //    return true;
            //});
            //_factory.AddFilter((n, c, l) =>
            //{
            //    if (c.StartsWith("bench"))
            //    {
            //        return l >= LogLevel.Information;
            //    }

            //    return true;
            //});
            //_factory.AddFilter((n, c, l) =>
            //{
            //    if (c.StartsWith("bench"))
            //    {
            //        return l >= LogLevel.Information;
            //    }

            //    return true;
            //});
            //_factory.AddFilter((n, c, l) =>
            //{
            //    if (c.StartsWith("bench"))
            //    {
            //        return l >= LogLevel.Information;
            //    }

            //    return true;
            //});
            //_factory.AddFilter((n, c, l) =>
            //{
            //    if (c.StartsWith("Default"))
            //    {
            //        return l >= LogLevel.Information;
            //    }

            //    return true;
            //});
            //_factory = _factory.WithFilter(new FilterLoggerSettings
            //{
            //    { "bench", LogLevel.Information }
            //});
            _factory.AddConsole();
            _logger = _factory.CreateLogger("bench");
        }

        [Benchmark]
        public void ElevenFilter()
        {
            _logger.LogInformation("test");
        }
    }
}
