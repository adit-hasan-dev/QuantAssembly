using Newtonsoft.Json.Linq;
using QuantAssembly.Common.Config;

namespace QuantAssembly.Core.Models
{
    /// <summary>
    ///  consolidate config for quantassembly
    /// </summary>
    public class Config : BaseConfig
    { 
        public string AccountId { get; init; } 
        public string CacheFolderPath { get; init; }
        public Dictionary<string, string> TickerStrategyMap { get; init; } 
        public int PollingIntervalInMs { get; init; } 
        public RiskManagementConfig RiskManagement { get; init; } 

    }
}