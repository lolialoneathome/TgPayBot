namespace PayBot.Configuration
{
    public interface IConfigService
    {
        Config Config { get; }
        void UpdateConfig(Config config);
    }
}
