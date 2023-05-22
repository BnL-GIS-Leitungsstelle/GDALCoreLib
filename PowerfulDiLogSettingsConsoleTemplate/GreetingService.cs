using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PowerfulDiLogSettingsConsoleTemplate
{
    public class GreetingService : IGreetingService
    {
        private readonly ILogger<GreetingService> _log;
        private readonly IConfiguration _config;

        public GreetingService(ILogger<GreetingService> log, IConfiguration config)
        {
            _log = log;
            _config = config;
        }


        public void Run()
        {
            for (int i = 0; i < _config.GetValue<int>("LoopTimes"); i++)
            {
                // log the numbers
                _log.LogWarning("Run number {runNumber}", i);  // structured logger stores var-ame and value extra
            }

        }
    }
}