using System.Text;

namespace QuantAssembly.BackTesting.Models
{
    public class BacktestStrategyReport
    {
        public string StrategyName { get; set; }
        public int StopLossConditionsHit { get; set; }
        public int ExitConditionsHit { get; set; }
        public int EntryConditionsHit { get; set; }
        public int TakeProfitConditionsHit { get; set; }


        public override string ToString()
        {
            return $"Strategy Name: {StrategyName}\n" +
                   $"Stop Loss Conditions Hit: {StopLossConditionsHit}\n" +
                   $"Exit Conditions Hit: {ExitConditionsHit}\n" +
                   $"Entry Conditions Hit: {EntryConditionsHit}\n" +
                   $"Take Profit Conditions Hit: {TakeProfitConditionsHit}";
        }
    }

    public class BackTestSummary
    {
        public IList<BacktestStrategyReport> backtestStrategyReports = new List<BacktestStrategyReport>();
        public double InitialPortfolioValue { get; set; }
        public double FinalPortfolioValue { get; set; }

        public override string ToString()
        {
            StringBuilder stringBuilder= new StringBuilder();
            stringBuilder.AppendLine("BacktestSummary:");
            stringBuilder.AppendLine($"Initial portfolio value: ${InitialPortfolioValue}");
            stringBuilder.AppendLine($"Final portfolio value: ${FinalPortfolioValue}");

            foreach (var backtestStrategyReport in backtestStrategyReports)
            {
                stringBuilder.AppendLine(backtestStrategyReport.ToString());
            }

            return stringBuilder.ToString();
        }
    }

}