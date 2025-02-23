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
        SignalType EvaluateOpenSignal(MarketData marketData, AccountData accountData, IndicatorData histData, string symbol);
        SignalType EvaluateCloseSignal(MarketData marketData, IndicatorData histData, Position position);
    }
}