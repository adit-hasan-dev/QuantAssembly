using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Models;
using QuantAssembly.Models;

namespace QuantAssembly.RiskManagement
{
    public abstract class BaseRiskManager<TConfig> : IRiskManager where TConfig : class
    {
        protected readonly ILogger logger;
        protected readonly double globalStopLoss;
        protected readonly double maxDrawDownPercentage;
        protected readonly TConfig customConfig;

        public BaseRiskManager(IServiceProvider serviceProvider)
        {
            var config = serviceProvider.GetRequiredService<IConfig>();
            logger = serviceProvider.GetRequiredService<ILogger>();
            globalStopLoss = config.RiskManagement.GlobalStopLoss;
            maxDrawDownPercentage = config.RiskManagement.MaxDrawDownPercentage;

            // Load the specific configuration
            var customProperties = config.CustomProperties;
            if (customProperties.TryGetValue(typeof(TConfig).Name, out var configObject))
            {
                customConfig = configObject.ToObject<TConfig>();
            }
            else
            {
                throw new InvalidOperationException($"Configuration for {typeof(TConfig).Name} not found.");
            }
        }

        ///<inheritdoc/>
        public abstract bool ComputePositionSize(
            MarketData marketData,
            HistoricalMarketData historicalMarketData,
            AccountData accountData,
            Position position);

        public bool shouldHaltNewTrades(AccountData accountData)
        {
            if (accountData.TotalPortfolioValue <= globalStopLoss)
            {
                logger.LogInfo("[BaseRiskManager::shouldHaltNewTrades] Global stop loss reached. Not opening any new positions");
                return true;
            }
            return false;
        }
    }
}