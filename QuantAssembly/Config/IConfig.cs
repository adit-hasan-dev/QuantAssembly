namespace QuantAssembly.Config
{
    public interface IConfig
    {
        string AccountId { get; }
        bool EnableDebugLog { get; }
        string LogFilePath { get; }
        string TransactionLogFile { get; }
        double MaxPortfolioEngagement { get; }
        double MaxSingleTradeAllocation { get; }
        double GlobalStopLoss { get; }
        Dictionary<string, string> TickerStrategyMap { get; }
        string LedgerFilePath { get; }
        int PollingIntervalInMs { get;}
    }
}