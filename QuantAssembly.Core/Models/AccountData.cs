namespace QuantAssembly.Core.Models
{
    public class AccountData
    {
        public string AccountID { get; set; }

        /// <summary>
        /// Total USD value of portfolio (equity + liquidity)
        /// </summary>
        public double TotalPortfolioValue { get; set; }

        /// <summary>
        /// This is the amount of cash in the account
        /// </summary>
        public double Liquidity { get; set; }

        /// <summary>
        /// This is the amount of funds currently
        /// invested in financial instruments like stocks, options, futures
        /// </summary>
        public double Equity { get; set; }

        public override string ToString()
        {
            return $"Account ID: {AccountID}\n" +
                   $"Total Portfolio Value: {TotalPortfolioValue:C}\n" +
                   $"Liquidity: {Liquidity:C}\n" +
                   $"Equity: {Equity:C}\n";
        }
    }
}