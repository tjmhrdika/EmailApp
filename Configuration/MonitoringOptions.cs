namespace EmailApp.Configuration
{
    public class MonitoringOptions
    {
        public int CheckIntervalSeconds { get; set; } = 15;
        public int LookbackMinutes { get; set; } = 5;
        public int ProcessingLookbackDays { get; set; } = 2;
        public string[] AlarmStates { get; set; } = ["UNACK_ALM", "UNACK_RTN"];
    }
}
