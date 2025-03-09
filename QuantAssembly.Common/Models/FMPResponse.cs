using System.Text.Json.Serialization;

namespace QuantAssembly.Common.Models
{
    public class FMPKeyMetricsResponse
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("fiscalYear")]
        public string FiscalYear { get; set; }

        [JsonPropertyName("marketCap")]
        public long MarketCap { get; set; }

        [JsonPropertyName("enterpriseValue")]
        public long EnterpriseValue { get; set; }

        [JsonPropertyName("evToEBITDA")]
        public double EvToEBITDA { get; set; }

        [JsonPropertyName("returnOnEquity")]
        public double ReturnOnEquity { get; set; }

        [JsonPropertyName("freeCashFlowToFirm")]
        public double FreeCashFlowToFirm { get; set; }

        [JsonPropertyName("netCurrentAssetValue")]
        public double NetCurrentAssetValue { get; set; }
    }
}