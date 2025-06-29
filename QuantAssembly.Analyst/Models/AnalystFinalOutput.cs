namespace QuantAssembly.Analyst.Models
{
    public class AnalystFinalOutput
    {
        public string AnalysisSummary { get; set; }
        public double TotalInvestmentAmount { get; set; }
        public double MaximumReturn { get; set; }
        public double MaximumRisk { get; set; }
        public List<OptionsContractRecommendation> RecommendedOptionsContracts { get; set; }
    }
}