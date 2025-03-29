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
        public int? minimumCompanyAge { get; set; }
        public int? maximumCompanyAge { get; set; }
        public double? minimumMarketCap { get; set; }
        public List<string> sectors { get; set; } = new List<string>();
        public List<string> subIndustries { get; set; } = new List<string>();
    }

    public class IndicatorFilterConfig
    {
        public double? RSIOversoldThreshold { get; set; } // Default: Oversold threshold
        public double? RSIOverboughtThreshold { get; set; } // Default: Overbought threshold
    }

    public class OptionsContractFilterConfig
    {
        public double? MinimumOpenInterest { get; set; }
        public double? MinimumVolume { get; set; }
        public double? MaxBidAskSpread { get; set; }
    }

    public class EmailPublishConfig
    {
        public string EmailAddress { get; set; }
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