using Newtonsoft.Json.Linq;

namespace QuantAssembly.Config
{
    public class RiskManagementConfig 
    { 
        public double MaxDrawDownPercentage { get; set; } 
        public double GlobalStopLoss { get; set; } 
    }

    public interface IConfig 
    { 
        string AccountId { get; } 
        bool EnableDebugLog { get; } 
        string LogFilePath { get; } 
        Dictionary<string, string> TickerStrategyMap { get; } 
        string LedgerFilePath { get; } 
        int PollingIntervalInMs { get; } 
        RiskManagementConfig RiskManagement { get; } 
        public Dictionary<string, JObject> CustomProperties { get; } 

    }
}