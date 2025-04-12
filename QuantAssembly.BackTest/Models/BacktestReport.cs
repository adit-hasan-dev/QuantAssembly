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
        public int PositionsStillOpen { get; set; }
        public double AverageProfitOnTakeProfit { get; set; }
        public double AverageLossOnStopLoss { get; set; }
        public double AverageHoldingPeriodInDays { get; set; }
        public double WinRate { get; set; }
        public double LossRate { get; set; }
        public double MaxDrawdown { get; set; }
        public double SharpeRatio { get; set; }
        public double ProfitFactor { get; set; }
        public double AverageReturnPerTrade { get; set; }
        public double LargestSingleTradeProfit { get; set; }
        public double LargestSingleTradeLoss { get; set; }

        public override string ToString()
        {
            return $"Strategy Name: {StrategyName}\n" +
                   $"Stop Loss Conditions Hit: {StopLossConditionsHit}\n" +
                   $"Exit Conditions Hit: {ExitConditionsHit}\n" +
                   $"Entry Conditions Hit: {EntryConditionsHit}\n" +
                   $"Take Profit Conditions Hit: {TakeProfitConditionsHit}\n" +
                   $"Positions Still Open: {PositionsStillOpen}\n" +
                   $"Average Profit on Take Profit: {AverageProfitOnTakeProfit:F2}\n" +
                   $"Average Loss on Stop Loss: {AverageLossOnStopLoss:F2}\n" +
                   $"Average Holding Period (Days): {AverageHoldingPeriodInDays:F2}\n" +
                   $"Win Rate: {WinRate:P2}\n" +
                   $"Loss Rate: {LossRate:P2}\n" +
                   $"Max Drawdown: {MaxDrawdown:F2}\n" +
                   $"Sharpe Ratio: {SharpeRatio:F2}\n" +
                   $"Profit Factor: {ProfitFactor:F2}\n" +
                   $"Average Return Per Trade: {AverageReturnPerTrade:F2}\n" +
                   $"Largest Single Trade Profit: {LargestSingleTradeProfit:F2}\n" +
                   $"Largest Single Trade Loss: {LargestSingleTradeLoss:F2}";
        }
    }

    public class BackTestSummary
    {
        public IList<BacktestStrategyReport> backtestStrategyReports = new List<BacktestStrategyReport>();
        public double InitialPortfolioValue { get; set; }
        public double FinalPortfolioValue { get; set; }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("BacktestSummary:");
            stringBuilder.AppendLine($"Initial portfolio value: ${InitialPortfolioValue}");
            stringBuilder.AppendLine($"Final portfolio value: ${FinalPortfolioValue}");

            foreach (var backtestStrategyReport in backtestStrategyReports)
            {
                stringBuilder.AppendLine(backtestStrategyReport.ToString());
                stringBuilder.AppendLine("");
            }

            return stringBuilder.ToString();
        }
    }

}