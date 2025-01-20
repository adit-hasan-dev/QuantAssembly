using QuantAssembly.Common.Models;
using QuantAssembly.Dashboard.Models;

public class AnalyticsService
{
    public IQueryable<StrategyMetrics> GetStrategyMetrics(List<Position> positions)
    {
        double riskFreeRate = 0.02;

        var metrics = positions
            .GroupBy(p => p.StrategyName)
            .Select(g =>
            {
                var strategyPositions = g.ToList();
                var totalProfitOrLoss = Math.Round(strategyPositions.Sum(p => p.ProfitOrLoss), 2);
                var winningTrades = strategyPositions.Count(p => p.ProfitOrLoss > 0);
                var totalTrades = strategyPositions.Count;
                var winRate = totalTrades > 0 ? Math.Round((double)winningTrades / totalTrades * 100, 2) : 0;
                var averageProfitPerTrade = totalTrades > 0 ? Math.Round(strategyPositions.Average(p => p.ProfitOrLoss), 2) : 0;

                var closedPositions = strategyPositions.Where(p => p.State == PositionState.Closed).ToList();
                var averageHoldingPeriod = closedPositions.Count > 0
                    ? TimeSpan.FromTicks((long)closedPositions.Average(p => (p.CloseTime - p.OpenTime).Ticks))
                    : TimeSpan.Zero;

                // Calculate Sharpe Ratio
                var avgReturn = totalTrades > 0 ? strategyPositions.Average(p => p.ProfitOrLoss) : 0;
                var stdDevReturn = totalTrades > 0
                    ? Math.Sqrt(strategyPositions.Average(p => Math.Pow(p.ProfitOrLoss - avgReturn, 2)))
                    : 0;
                var sharpeRatio = stdDevReturn > 0 ? Math.Round((avgReturn - riskFreeRate) / stdDevReturn, 2) : 0;

                var firstOpenTime = strategyPositions.Min(p => p.OpenTime);
                var weeks = (DateTime.Now - firstOpenTime).TotalDays / 7;
                var averageNumberOfPositionsOpened = weeks > 0 ? Math.Round(strategyPositions.Count(p => p.State == PositionState.Open) / weeks, 2) : 0;
                var averageNumberOfPositionsClosed = weeks > 0 ? Math.Round(strategyPositions.Count(p => p.State == PositionState.Closed) / weeks, 2) : 0;

                // Calculate Max Drawdown
                var cumulativeProfit = 0.0;
                var maxDrawdown = 0.0;
                var peak = 0.0;
                foreach (var position in strategyPositions.OrderBy(p => p.OpenTime))
                {
                    cumulativeProfit += position.ProfitOrLoss;
                    if (cumulativeProfit > peak)
                    {
                        peak = cumulativeProfit;
                    }
                    var drawdown = peak - cumulativeProfit;
                    if (drawdown > maxDrawdown)
                    {
                        maxDrawdown = drawdown;
                    }
                }
                maxDrawdown = Math.Round(maxDrawdown, 2);

                return new StrategyMetrics
                {
                    StrategyName = g.Key,
                    TotalProfitOrLoss = totalProfitOrLoss,
                    WinRate = winRate,
                    AverageProfitPerTrade = averageProfitPerTrade,
                    AverageHoldingPeriod = averageHoldingPeriod,
                    SharpeRatio = sharpeRatio,
                    AverageNumberOfPositionsOpened = averageNumberOfPositionsOpened,
                    AverageNumberOfPositionsClosed = averageNumberOfPositionsClosed,
                    MaxDrawDown = maxDrawdown
                };
            })
            .AsQueryable();

        return metrics;
    }

    public class StrategyCumulativeProfitLossData
    {
        public string StrategyName { get; set; }
        public List<int> NumberOfTransactions { get; set; }
        public List<double> CumulativeProfits { get; set; }
    }

    public List<StrategyCumulativeProfitLossData> GetCumulativeProfitLossData(List<Position> positions)
    {
        var openDateFallback = DateTime.Parse("0001-01-01T00:00:00");
        var strategyGroups = positions.GroupBy(p => p.StrategyName);

        var maxTransactions = strategyGroups.Max(g => g.Count());

        var result = new List<StrategyCumulativeProfitLossData>();

        foreach (var group in strategyGroups)
        {
            var cumulativeProfits = new List<double>();

            // Process positions, using 'OpenTime' and fall back to 'openDateFallback' if 'CloseTime' is default
            var sortedGroup = group.OrderBy(p => p.CloseTime == openDateFallback ? p.OpenTime : p.CloseTime);

            foreach (var position in sortedGroup)
            {
                var profitOrLoss = position.State == PositionState.Closed
                    ? position.ProfitOrLoss  // Closed position
                    : position.CurrentPrice - position.OpenPrice; // Open position

                var cumulativeProfit = (cumulativeProfits.Count > 0 ? cumulativeProfits.Last() : 0) + profitOrLoss;
                cumulativeProfits.Add(cumulativeProfit);
            }

            // If necessary, add 0 values to ensure all strategies have the same number of data points
            for (int i = cumulativeProfits.Count; i < maxTransactions; i++)
            {
                cumulativeProfits.Add(cumulativeProfits.Last());
            }

            result.Add(new StrategyCumulativeProfitLossData
            {
                StrategyName = group.Key,
                NumberOfTransactions = Enumerable.Range(1, maxTransactions).ToList(),
                CumulativeProfits = cumulativeProfits
            });
        }

        return result;
    }

}