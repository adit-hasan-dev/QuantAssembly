public class StrategyMetrics
{
    public string StrategyName { get; set; }
    public double TotalProfitOrLoss { get; set; }
    public double WinRate { get; set; }
    public double AverageProfitPerTrade { get; set; }
    public TimeSpan AverageHoldingPeriod { get; set; }
    public double SharpeRatio { get; set; }
    public double AverageNumberOfPositionsOpened { get; set; } // per week
    public double AverageNumberOfPositionsClosed { get; set; } // per week
    public double MaxDrawDown { get; set; }
}
