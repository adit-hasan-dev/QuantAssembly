using QuantAssembly.Common.Models;
using QuantAssembly.Core.Models;
using QuantAssembly.Models;

namespace QuantAssembly.Core.RiskManagement
{
    public class PercentageAccountValueRiskManagerConfig 
    { 
        public double MaxTradeRiskPercentage { get; set; }
    }

    public class PercentageAccountValueRiskManager : BaseRiskManager<PercentageAccountValueRiskManagerConfig>
    {
        public PercentageAccountValueRiskManager(IServiceProvider serviceProvider, RiskManagementConfig riskManagementConfig, PercentageAccountValueRiskManagerConfig config)
            : base(serviceProvider, riskManagementConfig, config)
        {
        }

        ///<inheritdoc/>
        public override bool ComputePositionSize(
            MarketData marketData,
            IndicatorData IndicatorData,
            AccountData accountData,
            Position position)
        {
            // The maximum amount of capital that should be allocated based on risk percentage
            // Doesn't represent the actual capital available
            double availableCapital = accountData.TotalPortfolioValue * customConfig.MaxTradeRiskPercentage;

            // Calculate the position size
            int quantity = (int)Math.Floor(availableCapital / marketData.LatestPrice);

            // Ensure we're not using more than available liquidity
            double positionCost = quantity * marketData.LatestPrice;
            if (positionCost > accountData.Liquidity)
            {
                quantity = (int)Math.Floor(accountData.Liquidity / marketData.LatestPrice);
            }

            // Check if we are exceeding the max drawdown percentage, if yes reduce quantity until
            // we are below the max
            double maxDrawDownCapitalForAccount = accountData.TotalPortfolioValue * maxDrawDownPercentage;
            double drawDownAmountAfterTrade = accountData.Equity + (quantity * marketData.LatestPrice);

            while (quantity > 0 && drawDownAmountAfterTrade > maxDrawDownCapitalForAccount) 
            { 
                quantity--; 
                drawDownAmountAfterTrade = accountData.Equity + (quantity * marketData.LatestPrice);
            }

            if (quantity <= 0)
            {
                logger.LogInfo("[PercentageAccountValueRiskManager::ComputePositionSize] Not enough capital to open position.");
                return false;
            }
            else
            {
                position.Quantity = quantity;
                return true;
            }
        }

        public bool ShouldHaltNewTrades(AccountData accountData)
        {
            if (accountData.TotalPortfolioValue <= globalStopLoss)
            {
                logger.LogInfo("[BaseRiskManager::ShouldHaltNewTrades] Global stop loss reached. Not opening any new positions");
                return true;
            }
            return false;
        }
    }
}