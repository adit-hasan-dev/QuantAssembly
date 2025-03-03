using System.Security.Cryptography.X509Certificates;

namespace QuantAssembly.Common.Models
{
    public class OptionsContractData
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
        public double AskPrice { get; set; }
        public double BidPrice { get; set; }
        public string OptionType { get; set; } // "call" or "put"
        public double StrikePrice { get; set; }
        public DateOnly ExpirationDate { get; set; }
        // No convenient way to get options volume data for free 
        // in Alpaca. Need to look harder
        // public double Volume { get; set; }
        public double OpenInterest { get; set; }
        public double ImpliedVolatility { get; set; }
        public double Delta { get; set; }
        public double Gamma { get; set; }
        public double Theta { get; set; }
        public double Vega { get; set; }
        public double Rho { get; set; }
    }

}