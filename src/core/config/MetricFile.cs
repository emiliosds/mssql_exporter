namespace mssql_exporter.core.config
{
    public class MetricFile
    {
        public MetricConfig[] Configs { get; set; }

        public int MillisecondTimeout { get; set; }

        public string ServerPort { get; set; }
    }

    public class MetricConfig
    {
        public string DataSource { get; set; }

        public MetricQuery[] Queries { get; set; }
    }
}