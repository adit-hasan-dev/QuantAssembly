using QuantAssembly.Common.Config;
using QuantAssembly.Common.Constants;
using QuantAssembly.Core.Models;

namespace QuantAssembly.BackTesting.Models
{
    public class Config : BaseConfig
    {
        public string AccountId { get; init; } 
        public Dictionary<string, string> TickerStrategyMap { get; init;} 
        public string CacheFolderPath { get; init; }
        // These should be parsed from config
        public TimePeriod timePeriod { get; init; } = TimePeriod.OneYear;
        public StepSize stepSize { get; init; } = StepSize.OneHour;
        public RiskManagementConfig RiskManagement { get; init; } 


    }
}