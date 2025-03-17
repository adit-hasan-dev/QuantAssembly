using QuantAssembly.Common;
using QuantAssembly.Common.Models;

namespace QuantAssembly.Analyst.Models
{
    public class TradeManagerCandidates
    {
        public string Symbol { get; set; }
        public TrendDirection TrendDirection { get; set; }
        public string Analysis { get; set; }
        public string Catalysts { get; set; }
        public double LatestPrice { get; set; }
        public double AskPrice { get; set; }
        public double BidPrice { get; set; }
        public IndicatorData IndicatorData { get; set; }
        public List<OptionsContractData> OptionsContracts{ get; set; }
    
    }
    public class TradeManagerRequestPayload
    {
        public List<TradeManagerCandidates> candidates { get; set; }
    }
}