namespace QuantAssembly.Common.Constants
{
    public enum TimePeriod
    {
        Day,
        FiveDays,
        OneMonth,
        ThreeMonths,
        SixMonths,
        OneYear
    }

    public enum StepSize
    {
        OneMinute,
        ThirtyMinutes,
        OneHour,
        OneDay,
        OneWeek,
        OneMonth
    }

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