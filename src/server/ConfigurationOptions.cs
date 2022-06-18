using mssql_exporter.core;

namespace mssql_exporter.server
{
    public class ConfigurationOptions : IConfigure
    {
        public string ConfigFile { get; set; } = @"C:\github\mssql_exporter\src\server\bin\Debug\net5.0\config.json";
        
        public string ConfigText { get; set; }

        public bool AddExporterMetrics { get; set; } = false;

        public string LogLevel { get; set; }

        public string LogFilePath { get; set; } = "mssqlexporter-log.txt";
    }
}