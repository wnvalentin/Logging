using System;
using Microsoft.Framework.Logging;
using ILogger = Microsoft.Framework.Logging.ILogger;
using Microsoft.Framework.Configuration;
using Microsoft.Dnx.Runtime;
using Microsoft.AspNet.FileProviders;

namespace SampleApp
{
    public class Program
    {
        private readonly IConfigurationRoot _config;
        private readonly IFileProvider _files;
        private readonly ILogger _logger;
        private readonly CaptureData _capture = new CaptureData();

        public Program(IApplicationEnvironment env)
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(env.ApplicationBasePath)
                .AddJsonFile("logging.json")
                .Build();

            _files = new PhysicalFileProvider(env.ApplicationBasePath);
            var token = _files.Watch("logging.json");

            // a DI based application would get ILoggerFactory injected instead
            var factory = new LoggerFactory();

            // getting the logger immediately using the class's name is conventional
            _logger = factory.CreateLogger<Program>();

            // providers may be added to an ILoggerFactory at any time, existing ILoggers are updated
#if !DNXCORE50
            factory.AddNLog(new global::NLog.LogFactory());
            factory.AddEventLog();
#endif

            factory.AddConsole(_config);
            factory.AddProvider(_capture);

            token.RegisterChangeCallback(ReloadConfiguration, null);
        }

        private void ReloadConfiguration(object obj)
        {
            var token = _files.Watch("logging.json");
            try
            {
                _config.Reload();
                _logger.LogInformation("Logging reconfigured");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Logging reconfiguration failed", ex);
            }
            token.RegisterChangeCallback(ReloadConfiguration, null);
        }

        public void Main(string[] args)
        {
            _logger.LogInformation("Starting");

            var startTime = DateTimeOffset.UtcNow;
            _logger.LogInformation(1, "Started at '{StartTime}' and 0x{Hello:X} is hex of 42", startTime, 42);
            // or
            _logger.ProgramStarting(startTime, 42);

            using (_logger.PurchaceOrderScope("00655321"))
            {
                try
                {
                    throw new Exception("Boom");
                }
                catch (Exception ex)
                {
                    _logger.LogCritical("Unexpected critical error starting application", ex);
                    _logger.LogError("Unexpected error", ex);
                    _logger.LogWarning("Unexpected warning", ex);
                }

                using (_logger.BeginScopeImpl("Main"))
                {
                    Console.WriteLine("Hello World");

                    _logger.LogInformation("Waiting for user input");
                    var input = Console.ReadLine();
                    _logger.LogInformation("User typed '{input}' on the command line", input);
                }
            }

            var endTime = DateTimeOffset.UtcNow;
            _logger.LogInformation(2, "Stopping at '{StopTime}'", endTime);
            // or
            _logger.ProgramFinished(endTime);


            _logger.LogInformation("Stopping");

            _capture.Rewrite(Console.Out);
        }
    }
}
