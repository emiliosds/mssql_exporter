namespace mssql_exporter.core
{
    public interface IConfigure
    {
        /// <summary>
        /// Path to the file containing metric configuration.
        /// </summary>
        string ConfigFile { get; set; }
        
        /// <summary>
        /// The text containing metric configuration.
        /// </summary>
        string ConfigText { get; set; }

        /// <summary>
        /// If true, adds default Prometheus Exporter metrics.
        /// </summary>
        /// <example>false</example>
        bool AddExporterMetrics { get; set; }

        /// <summary>
        /// Gets or sets the log file path.
        /// </summary>
        /// <value>
        /// The log file path.
        /// </value>
        public string LogFilePath { get; set; }
    }
}
