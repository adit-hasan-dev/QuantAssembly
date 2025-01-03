namespace QuantAssembly.Models.Constants
{
    public enum MarketDataType
    {
        RealTime,
        Delayed
    }

    public enum InstrumentType
    {
        Stock,
        OptionsContract,
        Futures,
        Cryptocurrency
    }

    public enum OrderType
    {
        Market,
        Limit,
        StopLimit
    }

    public enum OrderAction
    {
        Buy,
        Sell
    }
}