using QuantAssembly.Common.Config;
using QuantAssembly.Common.Constants;

namespace QuantAssembly.BackTesting.Models
{
    public class Config : BaseConfig
    {
        public string AccountId { get; } 
        public Dictionary<string, string> TickerStrategyMap { get; } 
        public string CacheFolderPath { get; }
        // These should be parsed from config
        public TimePeriod timePeriod { get; init; } = TimePeriod.OneYear;
        public StepSize stepSize { get; init; } = StepSize.OneHour;

    }
}