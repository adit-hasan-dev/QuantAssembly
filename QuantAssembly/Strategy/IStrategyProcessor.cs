using QuantAssembly.Common.Models;
using QuantAssembly.Models;

namespace QuantAssembly.Strategy
{
    public interface IStrategyProcessor
    {
        void LoadStrategyFromFile(string symbol, string filePath);
        void LoadStrategyFromContent(string symbol, string filePath);
        Strategy GetStrategy(string symbol);
        void SetStrategyStateForSymbol(string symbol, StrategyState state);
        IList<string> GetLoadedInstruments();
        bool ShouldOpen(MarketData marketData, AccountData accountData, HistoricalMarketData histData, string symbol);
        bool ShouldClose(MarketData marketData, AccountData accountData, HistoricalMarketData histData, Position position);
    }
}