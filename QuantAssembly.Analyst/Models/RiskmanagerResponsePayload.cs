namespace QuantAssembly.Analyst.Models
{
    public class OptionsContractRecommendation
    {
        public string ContractSymbol { get; set; }
        public string ContractType { get; set; } // e.g., Call, Put
        public string Reasoning { get; set; }
        public string Risks { get; set; }
        public string UnderlyingAssetSymbol { get; set; }
        public string UnderlyingAssetLatestPrice { get; set; }
        public string StrikePrice { get; set; }
        public int RecommendedQuantityToBuy { get; set; }
        public string ExpirationDate { get; set; }
        public string ContractAskPrice { get; set; }
        public string ContractBidPrice { get; set; }
        public string OpenInterest { get; set; }
        public string ImpliedVolatility { get; set; }
        public double TakeProfitLevel { get; set; }
        public double StopLossLevel { get; set; }
        public double TotalInvestmentAmount { get; set; }

    }
    public class RiskmanagerResponsePayload
    {
        public string AnalysisSummary { get; set; }
        
        public List<OptionsContractRecommendation> RecommendedOptionsContracts { get; set; }
    }
}