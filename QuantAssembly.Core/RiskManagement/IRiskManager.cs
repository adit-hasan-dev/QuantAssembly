using QuantAssembly.Common.Models;
using QuantAssembly.Core.Models;

namespace QuantAssembly.Core.RiskManagement
{
    public interface IRiskManager
    {
        /// <summary> 
        /// Computes the position size and updates the quantity property of the provided position object.
        /// </summary> 
        /// <param name="marketData">Market data for the symbol.</param>
        /// <param name="IndicatorData">Indicator data.</param>
        /// <param name="accountData">Account data including total portfolio value and liquidity.</param>
        /// <param name="position">The position object to be updated. The object is updated in place</param>
        /// <returns>True if the position size was successfully computed; otherwise, false.</returns>
        bool ComputePositionSize(
            MarketData marketData,
            IndicatorData IndicatorData,
            AccountData accountData,
            Position position);

        bool shouldHaltNewTrades(AccountData accountData);
    }
}