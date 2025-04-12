using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Models;
using QuantAssembly.Core.Models;

namespace QuantAssembly.Core.RiskManagement
{
    public abstract class BaseRiskManager<TConfig> : IRiskManager where TConfig : class
    {
        protected readonly ILogger logger;
        protected readonly double globalStopLoss;
        protected readonly double maxDrawDownPercentage;
        protected readonly TConfig customConfig;

        public BaseRiskManager(IServiceProvider serviceProvider, RiskManagementConfig riskManagementConfig, TConfig customConfig)
        {
            logger = serviceProvider.GetRequiredService<ILogger>();
            globalStopLoss = riskManagementConfig.GlobalStopLoss;
            maxDrawDownPercentage = riskManagementConfig.MaxDrawDownPercentage;

            this.customConfig = customConfig;
        }

        ///<inheritdoc/>
        public abstract bool ComputePositionSize(
            MarketData marketData,
            IndicatorData IndicatorData,
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