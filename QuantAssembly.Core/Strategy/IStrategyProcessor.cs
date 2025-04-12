using QuantAssembly.Common.Models;
using QuantAssembly.Core.Models;

namespace QuantAssembly.Core.Strategy
{
    public interface IStrategyProcessor
    {
        void LoadStrategyFromFile(string symbol, string filePath);
        void LoadStrategyFromContent(string symbol, string filePath);
        Strategy GetStrategy(string symbol);
        Dictionary<string, Strategy> GetStrategies();
        void SetStrategyStateForSymbol(string symbol, StrategyState state);
        IList<string> GetLoadedInstruments();
        SignalType EvaluateOpenSignal(MarketData marketData, AccountData accountData, IndicatorData histData, string symbol);
        SignalType EvaluateCloseSignal(MarketData marketData, IndicatorData histData, Position position);
    }
}