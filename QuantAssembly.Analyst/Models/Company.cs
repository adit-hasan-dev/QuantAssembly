namespace QuantAssembly.Analyst
{
    public class Company
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string Sector { get; set; }
        public double Price { get; set; }
        public double PriceToEarningsRatio { get; set; }
        public double DividendYield { get; set; }
        public double EarningsPerShare { get; set; }
        public double YearlyLow { get; set; }
        public double YearlyHigh { get; set; }
        public double MarketCap { get; set; }
        public double EBITDA { get; set; }
        public double PriceToSalesRatio { get; set; }
        public double PriceToBookRatio { get; set; }
        public string SECFilings { get; set; }
    }
}