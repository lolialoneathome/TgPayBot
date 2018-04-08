using System.Threading.Tasks;

namespace PayBot.Configuration
{
    public interface IConfigService
    {
        Config Config { get; }
        Task UpdateConfig(Config config);
    }
}
