namespace Common.Abstractions
{
    public class ResilienceSettings
    {
        public int RetryCount { get; set; } = 3;

        public int NumberOfErrorsBeforeBreakingCircuit { get; set; } = 3;

        public int NumberOfSecondsToKeepCircuitBroken { get; set; } = 5;

        public static ResilienceSettings Default =>
            new ResilienceSettings();
    }
}