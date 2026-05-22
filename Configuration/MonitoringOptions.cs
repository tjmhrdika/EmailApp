namespace EmailApp.Configuration
{
    public class MonitoringOptions
    {
        public int CheckIntervalSeconds { get; set; } = 10;
        public int LookbackMinutes { get; set; } = 5;
    }
}
