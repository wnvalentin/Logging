using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class LoggerMetricsTest
    {
        [Fact]
        public void RecordValue_WritesValueToRegisteredMetricsProviders()
        {
            var testSink1 = new TestSink();
            var testSink2 = new TestSink();
            var loggerFactory = TestLoggerBuilder.Create(builder => builder
                .AddProvider(new TestLoggerProvider(testSink1, isEnabled: true))
                .AddProvider(new TestLoggerProvider(testSink2, isEnabled: true)));

            var logger = loggerFactory.CreateLogger("test");
            var metric = logger.DefineMetric("test");
            metric.RecordValue(42.0);

            Assert.Collection(testSink1.Metrics,
                item =>
                {
                    Assert.Equal("test", item.Name);
                    Assert.Equal(42.0, item.Value);
                });

            Assert.Collection(testSink2.Metrics,
                item =>
                {
                    Assert.Equal("test", item.Name);
                    Assert.Equal(42.0, item.Value);
                });
        }

        [Fact]
        public void RecordValue_DoesntWriteMetricToProviderWithoutMetricsSupport()
        {
            var testSink1 = new TestSink();
            var testSink2 = new TestSink();
            var loggerFactory = TestLoggerBuilder.Create(builder => builder
                .AddProvider(new TestLoggerProviderWithoutMetrics(testSink1, isEnabled: true))
                .AddProvider(new TestLoggerProvider(testSink2, isEnabled: true)));

            var logger = loggerFactory.CreateLogger("test");
            var metric = logger.DefineMetric("test");
            metric.RecordValue(42.0);

            Assert.Empty(testSink1.Metrics);

            Assert.Collection(testSink2.Metrics,
                item =>
                {
                    Assert.Equal("test", item.Name);
                    Assert.Equal(42.0, item.Value);
                });
        }

        [Fact]
        public void AddProvider_UpdatesMetricOutputs()
        {
            var testSink1 = new TestSink();
            var testSink2 = new TestSink();
            var loggerFactory = TestLoggerBuilder.Create(builder => builder
                .AddProvider(new TestLoggerProvider(testSink1, isEnabled: true)));

            var logger = loggerFactory.CreateLogger("test");
            var metric = logger.DefineMetric("test");

            // This will only go to testSink1
            metric.RecordValue(42.0);

            // Now add a provider
            loggerFactory.AddProvider(new TestLoggerProvider(testSink2, isEnabled: true));

            // This should go to both sinks
            metric.RecordValue(24.0);

            Assert.Collection(testSink1.Metrics,
                item =>
                {
                    Assert.Equal("test", item.Name);
                    Assert.Equal(42.0, item.Value);
                },
                item =>
                {
                    Assert.Equal("test", item.Name);
                    Assert.Equal(24.0, item.Value);
                });

            Assert.Collection(testSink2.Metrics,
                item =>
                {
                    Assert.Equal("test", item.Name);
                    Assert.Equal(24.0, item.Value);
                });
        }

        [Fact]
        public void ConfigurationChanges_UpdateMetricOutputs()
        {
            var filterOptions = new LoggerFilterOptions();

            // Filter out TestLoggerProvider2
            filterOptions.Rules.Add(new LoggerFilterRule(
                providerName: typeof(TestLoggerProvider2).FullName,
                categoryName: null,
                logLevel: LogLevel.None,
                filter: null));

            var optionsMonitor = TestOptionsMonitor.Create(filterOptions);

            var testSink1 = new TestSink();
            var testSink2 = new TestSink();
            var loggerFactory = TestLoggerBuilder.Create(builder =>
            {
                builder.Services.AddSingleton<IOptionsMonitor<LoggerFilterOptions>>(optionsMonitor);
                builder
                    .AddProvider(new TestLoggerProvider(testSink1, isEnabled: true))
                    .AddProvider(new TestLoggerProvider2(testSink2));
            });

            var logger = loggerFactory.CreateLogger("test");
            var metric = logger.DefineMetric("test");

            // This will only go to testSink1 because of the filter
            metric.RecordValue(42.0);

            // Now remove the rule and update the options
            filterOptions.Rules.Clear();
            optionsMonitor.NotifyChanged();

            // This should go to both sinks
            metric.RecordValue(24.0);

            Assert.Collection(testSink1.Metrics,
                item =>
                {
                    Assert.Equal("test", item.Name);
                    Assert.Equal(42.0, item.Value);
                },
                item =>
                {
                    Assert.Equal("test", item.Name);
                    Assert.Equal(24.0, item.Value);
                });

            Assert.Collection(testSink2.Metrics,
                item =>
                {
                    Assert.Equal("test", item.Name);
                    Assert.Equal(24.0, item.Value);
                });
        }

        [Fact]
        public void DefineMetric_ReturnsSameInstanceForSameName()
        {
            // Users aren't supposed to do this, but we want to make sure it can work if they do.

            var testSink = new TestSink();
            var loggerFactory = TestLoggerBuilder.Create(builder => builder
                .AddProvider(new TestLoggerProvider(testSink, isEnabled: true)));

            var logger = loggerFactory.CreateLogger("test");

            var metric1 = logger.DefineMetric("test");
            var metric2 = logger.DefineMetric("test");

            // Should be the same instance
            Assert.Same(metric1, metric2);

            // Should both go to the same place
            metric1.RecordValue(42.0);
            metric2.RecordValue(24.0);

            Assert.Collection(testSink.Metrics,
                item =>
                {
                    Assert.Equal("test", item.Name);
                    Assert.Equal(42.0, item.Value);
                },
                item =>
                {
                    Assert.Equal("test", item.Name);
                    Assert.Equal(24.0, item.Value);
                });
        }
    }
}
