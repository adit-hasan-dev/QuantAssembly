using QuantAssembly.Dashboard.Models;

public class AnalyticsService
{
    public IQueryable<StrategyMetrics> GetStrategyMetrics(List<Position> positions)
    {
        double riskFreeRate = 0.01; // Example: 1% risk-free rate

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

                var closedPositions = strategyPositions.Where(p => p.State == "Closed").ToList();
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
                var averageNumberOfPositionsOpened = weeks > 0 ? Math.Round(strategyPositions.Count(p => p.State == "Open") / weeks, 2) : 0;
                var averageNumberOfPositionsClosed = weeks > 0 ? Math.Round(strategyPositions.Count(p => p.State == "Closed") / weeks, 2) : 0;

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
}