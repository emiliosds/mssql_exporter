using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using mssql_exporter.core;
using mssql_exporter.core.config;
using Prometheus;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace mssql_exporter.server
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if ((args.Length >= 1 && args[0].Equals("serve", StringComparison.CurrentCulture)) || WindowsServiceHelpers.IsWindowsService())
            {
                RunWebServer(args.Where(a => !string.Equals("serve", a, StringComparison.InvariantCultureIgnoreCase)).ToArray());
            }
            else
            {
                Help();
            }
        }

        public static void Help()
        {
            Console.WriteLine("Commands");
            Console.WriteLine("   help");
            Console.WriteLine("   serve");
            Console.WriteLine("      -ConfigFile (metrics.json)");
            Console.WriteLine("      -AddExporterMetrics (false)");
            Console.WriteLine("      -ConfigText ()");
            Console.WriteLine(string.Empty);
            Console.WriteLine("Or environment variables:");
            Console.WriteLine("      PROMETHEUS_MSSQL_ConfigFile");
            Console.WriteLine("      PROMETHEUS_MSSQL_AddExporterMetrics");
            Console.WriteLine("      PROMETHEUS_MSSQL_ConfigText");
            Console.WriteLine("      PROMETHEUS_MSSQL_Serilog__MinimumLevel");
        }

        public static void RunWebServer(string[] args)
        {
            var switchMappings = new Dictionary<string, string>
            {
                {"-ConfigFile", "ConfigFile"},
                {"-AddExporterMetrics", "AddExporterMetrics"},
                {"-ConfigText", "ConfigText"}
            };

            var config = new ConfigurationBuilder()
                .AddJsonFile("config.json", true, false)
                .AddJsonFile("appsettings.json", true, false)
                .AddEnvironmentVariables("PROMETHEUS_MSSQL_")
                .AddCommandLine(args, switchMappings)
                .Build();

            IConfigure configurationBinding = new ConfigurationOptions();
            config.Bind(configurationBinding);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .Enrich.FromLogContext()
                //Logging setup above is a default in case load from configuration doesn't override.
                .ReadFrom.Configuration(config)
                .CreateLogger();

            //Log.Logger.Information("AddExporterMetrics {AddExporterMetrics}", configurationBinding.AddExporterMetrics);
            MetricFile metricFile;
            if (string.IsNullOrWhiteSpace(configurationBinding.ConfigText))
            {
                var filePath = TryGetAbsolutePath(configurationBinding.ConfigFile);
                try
                {
                    var fileText = File.ReadAllText(filePath);
                    //Log.Logger.Information("Reading ConfigText {ConfigText} from {FileName}", fileText, filePath);
                    metricFile = Parser.FromJson(fileText);
                }
                catch (Exception e)
                {
                    Log.Logger.Error(e, "Failed to read and parse text from {FileName}", filePath);
                    throw;
                }
            }
            else
            {
                try
                {
                    //Log.Logger.Information("Parsing ConfigText {ConfigText}", configurationBinding.ConfigText);
                    metricFile = Parser.FromJson(configurationBinding.ConfigText);
                }
                catch (Exception e)
                {
                    Log.Logger.Error(e, "Failed to parse text from ConfigText");
                    throw;
                }
            }

            var registry = (configurationBinding.AddExporterMetrics)
                ? Metrics.DefaultRegistry
                : new CollectorRegistry();

            foreach (var metricConfig in metricFile.Configs)
            {
                StartMetric(registry, metricConfig, metricFile);
            }

            CreateHostBuilder(args, registry, metricFile).Build().Run();
        }

        public static void StartMetric(CollectorRegistry registry, MetricConfig metricConfig, MetricFile metricFile)
        {
            if (string.IsNullOrWhiteSpace(metricConfig.DataSource))
            {
                Log.Logger.Error("Expected DataSource: SQL Server connectionString");
                return;
            }

            ConfigurePrometheus(Log.Logger, metricConfig, metricFile, registry);
        }

        public static OnDemandCollector ConfigurePrometheus(ILogger logger, MetricConfig metricConfig, MetricFile metricFile, CollectorRegistry registry)
        {
            return new OnDemandCollector(
                metricConfig.DataSource,
                metricFile.MillisecondTimeout,
                logger,
                registry,
                metricFactory => ConfigureMetrics(metricConfig, metricFactory, logger));
        }

        public static IEnumerable<IQuery> ConfigureMetrics(MetricConfig metricConfig, MetricFactory metricFactory, ILogger logger)
        {
            return metricConfig.Queries.Select(x => MetricQueryFactory.GetSpecificQuery(metricFactory, x, logger));
        }

        public static IHostBuilder CreateHostBuilder(string[] args, CollectorRegistry registry, MetricFile metricFile)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(builder => builder
                    .Configure(applicationBuilder => applicationBuilder.UseMetricServer("/metrics", registry))
                    .UseUrls($"http://*:{metricFile.ServerPort}"))
                .UseWindowsService()
                .UseSerilog();
        }

        private static string TryGetAbsolutePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            if (Path.IsPathFullyQualified(path))
            {
                return path;
            }

            return Path.Combine(AppContext.BaseDirectory, path);
        }
    }
}