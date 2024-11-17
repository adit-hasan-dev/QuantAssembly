namespace QuantAssembly.Config
{
    public interface IConfig
    {
        string AccountId { get; }
        bool EnableDebugLog { get; }
        double MaxPortfolioEngagement { get; }
        double MaxSingleTradeAllocation { get; }
        double GlobalStopLoss { get; }
        string[] Tickers { get; }
    }
}