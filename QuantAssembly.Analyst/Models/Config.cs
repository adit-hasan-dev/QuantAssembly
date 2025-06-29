using QuantAssembly.Common.Config;

namespace QuantAssembly.Analyst.Models
{
    public class MarketDataFilterConfig
    {
        public double? MinPrice { get; set; } = null;
        public double? MaxPrice { get; set; } = null;
        public double? MinVolume { get; set; } = null;
        public double? MaxSpreadPercentage { get; set; } = null;
        public double? MaxChangePercentage { get; set; } = null;
    }

    public class CompanyDataFilterConfig
    {
        public double? MinimumMarketCap { get; set; }
        public double? MaximumMarketCap { get; set; }
        public List<string> Sectors { get; set; } = new();
        public double? MinimumPERatio { get; set; }
        public double? MaximumPERatio { get; set; }

        public double? MinimumDividendYield { get; set; }
        public double? MaximumDividendYield { get; set; }

        public double? MinimumEPS { get; set; }
        public double? MaximumEPS { get; set; }

        public double? MinimumPriceToSalesRatio { get; set; }
        public double? MaximumPriceToSalesRatio { get; set; }

        public double? MinimumPriceToBookRatio { get; set; }
        public double? MaximumPriceToBookRatio { get; set; }
    }


    public class IndicatorFilterConfig
    {
        public double? RSIOversoldThreshold { get; set; }
        public double? RSIOverboughtThreshold { get; set; }
    }

    public class OptionsContractFilterConfig
    {
        public double? MinimumOpenInterest { get; set; }
        public double? MinimumVolume { get; set; }
        public double? MaxBidAskSpread { get; set; }
        public double? MinImpliedVolatility { get; set; }
        public double? MaxImpliedVolatility { get; set; }
        public double? MinDelta { get; set; }
        public double? MaxDelta { get; set; }
        public double? MinGamma { get; set; }
        public double? MaxGamma { get; set; }
        public double? MinTheta { get; set; }
        public double? MaxTheta { get; set; }
        public double? MinVega { get; set; }
        public double? MaxVega { get; set; }
        public double? MinRho { get; set; }
        public double? MaxRho { get; set; }
    }

    public class EmailPublishConfig
    {
        public string SourceEmailAddress { get; set; }
        public string DestinationEmailAddress { get; set; }
        public string AppPassword { get; set; }
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
    }

    public class Config : BaseConfig
    {
        public MarketDataFilterConfig marketDataFilterConfig { get; set; }
        public CompanyDataFilterConfig companyDataFilterConfig { get; set; }
        public IndicatorFilterConfig indicatorFilterConfig { get; set; }
        public OptionsContractFilterConfig optionsContractFilterConfig { get; set; }
        public string OutputFilePath { get; set; }
        public EmailPublishConfig emailPublishConfig { get; set; }
    }
}