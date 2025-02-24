using Newtonsoft.Json.Linq;
using QuantAssembly.Common.Config;

namespace QuantAssembly
{
    public class RiskManagementConfig 
    { 
        public double MaxDrawDownPercentage { get; set; } 
        public double GlobalStopLoss { get; set; } 
    }

    public class Config : BaseConfig
    { 
        public string AccountId { get; } 
        public bool EnableDebugLog { get; } 
        public string LogFilePath { get; } 
        public string LedgerFilePath { get; } 
        public string CacheFolderPath { get; }
        public Dictionary<string, string> TickerStrategyMap { get; } 
        public int PollingIntervalInMs { get; } 
        public RiskManagementConfig RiskManagement { get; } 
        public Dictionary<string, JObject> CustomProperties { get; } 

    }
}