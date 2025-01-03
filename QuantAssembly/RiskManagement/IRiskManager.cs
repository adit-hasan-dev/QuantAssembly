using QuantAssembly.Models;

namespace QuantAssembly.RiskManagement
{
    public interface IRiskManager
    {
        /// <summary> 
        /// Computes the position size and updates the quantity property of the provided position object.
        /// </summary> 
        /// <param name="marketData">Market data for the symbol.</param>
        /// <param name="historicalMarketData">Historical market data.</param>
        /// <param name="accountData">Account data including total portfolio value and liquidity.</param>
        /// <param name="position">The position object to be updated. The object is updated in place</param>
        /// <returns>True if the position size was successfully computed; otherwise, false.</returns>
        bool ComputePositionSize(
            MarketData marketData,
            HistoricalMarketData historicalMarketData,
            AccountData accountData,
            Position position);

        bool shouldHaltNewTrades(AccountData accountData);
    }
}