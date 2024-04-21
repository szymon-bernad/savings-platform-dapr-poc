namespace SavingsPlatform.Dashboard.Api.Config
{
    public record ServicesConfig
    {
        public string SavingsPlatformApi { get; init; } = string.Empty;
        public string EventStoreApi { get; init; } = string.Empty;
    }
}
