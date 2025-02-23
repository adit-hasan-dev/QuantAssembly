using System;

namespace QuantAssembly.Models
{
    public enum SignalType
    {
        None,
        Entry,
        Exit,
        StopLoss,
        TakeProfit
    }

    public class Signal
    {
        public SignalType Type { get; set;} = SignalType.None;
        public Guid PositionGuid { get; set;} = Guid.Empty;
        public string SymbolName { get; set;} = string.Empty;
        public MarketData MarketData { get; set;} = new MarketData();
        public IndicatorData IndicatorData { get; set;} = new IndicatorData();
    }
}