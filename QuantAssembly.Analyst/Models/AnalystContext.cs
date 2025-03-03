using QuantAssembly.Analyst.Models;
using QuantAssembly.Common.Models;
using QuantAssembly.Common.Pipeline;

namespace QuantAssembly.Analyst
{
    public class CandidateData
    {
        public Company company { get; set; }
        public AnalystMarketData marketData { get; set; }
        public IndicatorData indicatorData { get; set; }
        public OptionsContractData optionsContractData { get; set; }
    }
    public class AnalystContext : PipelineContext
    {
        public List<Company> companies { get; set; } = new List<Company>();
        public List<AnalystMarketData> marketData { get; set; } = new List<AnalystMarketData>();
        public List<IndicatorData> indicatorData { get; set; } = new List<IndicatorData>();
        public List<OptionsContractData> optionsContractData { get; set; } = new List<OptionsContractData>();
        public List<CandidateData> candidates { get; set; } = new List<CandidateData>();
        public string composedOutput { get; set; } = string.Empty;
    }
}