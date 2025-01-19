using Newtonsoft.Json.Linq;

namespace QuantAssembly.Common.Config
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
        string LedgerFilePath { get; } 
        string CacheFolderPath { get; }
        Dictionary<string, string> TickerStrategyMap { get; } 
        int PollingIntervalInMs { get; } 
        RiskManagementConfig RiskManagement { get; } 
        public Dictionary<string, JObject> CustomProperties { get; } 

    }
}