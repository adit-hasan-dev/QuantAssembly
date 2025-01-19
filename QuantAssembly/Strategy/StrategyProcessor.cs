namespace QuantAssembly.Strategy
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using QuantAssembly.Common.Logging;
    using QuantAssembly.Common.Models;
    using QuantAssembly.Models;
    public class StrategyProcessor : IStrategyProcessor
    {
        private Dictionary<string, Strategy> strategies = new Dictionary<string, Strategy>();
        private ILogger logger;
        public StrategyProcessor(ILogger logger)
        {
            this.logger = logger;
        }

        public void LoadStrategyFromFile(string stockTicker, string filePath)
        {
            logger.LogInfo($"[StrategyProcessor::LoadStrategy] Loading strategy for ticker: {stockTicker} from {filePath}");
            string fileContent = File.ReadAllText(filePath);
            this.LoadStrategyFromContent(stockTicker, fileContent);
        }

        public void LoadStrategyFromContent(string stockTicker, string strategyJson)
        {
            if (strategies.TryGetValue(stockTicker, out Strategy existingStrategy))
            {
                throw new InvalidOperationException($"[StrategyProcessor::LoadStrategy] Strategy for ticker {stockTicker} already exists, strategy: {existingStrategy.Name}. Duplicates are not allowed.");
            }
            
            logger.LogInfo($"[StrategyProcessor::LoadStrategy] Loading strategy for ticker: {stockTicker}");
            
            var strategy = JsonConvert.DeserializeObject<Strategy>(strategyJson);

            if (strategy != null && ValidateStrategy(strategy))
            {
                strategies[stockTicker] = strategy;
            }
            else
            {
                logger.LogError($"[StrategyProcessor::LoadStrategy] Failed to deserialize or validate strategy for {stockTicker}");
                throw new InvalidOperationException("Invalid strategy definition.");
            }

            logger.LogInfo($"Successfully loaded strategy for ticker: {stockTicker}");
        }

        public void SetStrategyStateForSymbol(string symbol, StrategyState state)
        {
            if (strategies.TryGetValue(symbol, out Strategy existingStrategy))
            {
                existingStrategy.State = state;
            }
            else
            {
                throw new KeyNotFoundException($"[StrategyProcessor::SetStrategyStateForSymbol] No strategy for symbol {symbol} was loaded");
            }

        }


        public bool ShouldOpen(MarketData marketData, AccountData accountData, HistoricalMarketData histData, string stockTicker)
        {
            if (!strategies.TryGetValue(stockTicker, out var strategy))
            {
                throw new KeyNotFoundException("Strategy not found.");
            }

            // When determine whether to enter a position, we only care about the entry conditions
            return EvaluateConditionGroup(strategy.EntryConditions, marketData, histData);
        }

        public bool ShouldClose(MarketData marketData, AccountData accountData, HistoricalMarketData histData, Position position)
        {
            if (!strategies.TryGetValue(position.Symbol, out var strategy))
            {
                throw new KeyNotFoundException("Strategy not found.");
            }

            // reasons to exit a position are:
            // 1. The take profit level was reached
            // 2. The exit condition was met
            // 3. The stop loss condition was met
            return  EvaluateConditionGroup(strategy.StopLossConditions, marketData, histData, position) || 
                    EvaluateConditionGroup(strategy.ExitConditions, marketData, histData, position) ||
                    EvaluateConditionGroup(strategy.TakeProfitConditions, marketData, histData, position);
        }

        private bool ValidateStrategy(Strategy strategy)
        {
            bool IsValidCondition(IStrategyCondition condition)
            {
                if (condition is PropertyToValueComparator valueComparator)
                {
                    return Enum.IsDefined(typeof(StrategyProperty), valueComparator.Property) &&
                           Enum.IsDefined(typeof(StrategyOperator), valueComparator.Operator);
                }
                else if (condition is PropertyToPropertyComparator propertyComparator)
                {
                    return Enum.IsDefined(typeof(StrategyProperty), propertyComparator.LeftHandOperand) &&
                           Enum.IsDefined(typeof(StrategyProperty), propertyComparator.RightHandOperand) &&
                           Enum.IsDefined(typeof(StrategyOperator), propertyComparator.Operator);
                }
                return false;
            }

            bool IsValidConditionGroup(ConditionGroup group)
            {
                return Enum.IsDefined(typeof(LogicalOperator), group.LogicalOperator) &&
                       group.Conditions.All(IsValidCondition);
            }

            return IsValidConditionGroup(strategy.EntryConditions) &&
                   IsValidConditionGroup(strategy.ExitConditions) &&
                   IsValidConditionGroup(strategy.StopLossConditions) &&
                   IsValidConditionGroup(strategy.TakeProfitConditions);
        }

        private bool EvaluateConditionGroup(ConditionGroup group, MarketData marketData, HistoricalMarketData histData, Position position = null)
        {
            if (group == null || !group.Conditions.Any()) return false;

            bool result = EvaluateCondition(group.Conditions[0], marketData, histData, position);

            for (int i = 1; i < group.Conditions.Count; i++)
            {
                var condition = group.Conditions[i];
                bool conditionResult = EvaluateCondition(condition, marketData, histData, position);

                if (group.LogicalOperator == LogicalOperator.AND)
                {
                    result = result && conditionResult;
                }
                else if (group.LogicalOperator == LogicalOperator.OR)
                {
                    result = result || conditionResult;
                }
            }

            return result;
        }

        private bool EvaluateCondition(IStrategyCondition condition, MarketData marketData, HistoricalMarketData histData, Position position = null)
        {
            if (condition is PropertyToValueComparator valueComparator)
            {
                var propertyValue = GetPropertyValue(marketData, histData, valueComparator.Property, position);
                return valueComparator.Operator switch
                {
                    StrategyOperator.GreaterThan => propertyValue > valueComparator.Value,
                    StrategyOperator.GreaterThanOrEqual => propertyValue >= valueComparator.Value,
                    StrategyOperator.LessThan => propertyValue < valueComparator.Value,
                    StrategyOperator.LessThanOrEqual => propertyValue <= valueComparator.Value,
                    StrategyOperator.EqualTo => propertyValue == valueComparator.Value,
                    _ => throw new InvalidOperationException("Invalid operator")
                };
            }
            else if (condition is PropertyToPropertyComparator propertyComparator)
            {
                var leftValue = GetPropertyValue(marketData, histData, propertyComparator.LeftHandOperand, position);
                var rightValue = GetPropertyValue(marketData, histData, propertyComparator.RightHandOperand, position);
                return propertyComparator.Operator switch
                {
                    StrategyOperator.GreaterThan => leftValue > rightValue,
                    StrategyOperator.GreaterThanOrEqual => leftValue >= rightValue,
                    StrategyOperator.LessThan => leftValue < rightValue,
                    StrategyOperator.LessThanOrEqual => leftValue <= rightValue,
                    StrategyOperator.EqualTo => leftValue == rightValue,
                    _ => throw new InvalidOperationException("Invalid operator")
                };
            }
            throw new InvalidOperationException("Unknown condition type");
        }

        private double GetPropertyValue(MarketData marketData, HistoricalMarketData histData, StrategyProperty property, Position position = null)
        {
            return property switch
            {
                StrategyProperty.BidPrice => marketData.BidPrice,
                StrategyProperty.AskPrice => marketData.AskPrice,
                StrategyProperty.LatestPrice => marketData.LatestPrice,
                StrategyProperty.MACD => histData.MACD,
                StrategyProperty.SignalLine => histData.Signal,
                StrategyProperty.RSI => histData.RSI,
                StrategyProperty.SMA_50 => histData.SMA_50,
                StrategyProperty.SMA_200 => histData.SMA_200,
                StrategyProperty.EMA_50 => histData.EMA_50,
                StrategyProperty.Upper_Band => histData.Upper_Band,
                StrategyProperty.Lower_Band => histData.Lower_Band,
                StrategyProperty.ATR => histData.ATR,
                StrategyProperty.HistoricalHigh => histData.HistoricalHigh,
                StrategyProperty.HistoricalLow => histData.HistoricalLow,
                StrategyProperty.ProfitPercentage => position.ProfitOrLoss > 0 ? (position.ProfitOrLoss/(position.OpenPrice*position.Quantity)) : 0,
                StrategyProperty.LossPercentage => position.ProfitOrLoss < 0 ? Math.Abs(position.ProfitOrLoss/(position.OpenPrice*position.Quantity)) : 0,
                _ => throw new InvalidOperationException("Invalid property")
            };
        }

        public IList<string> GetLoadedInstruments()
        {
            return strategies.Keys.ToList();
        }

        public Strategy GetStrategy(string symbol)
        {
            if (strategies.TryGetValue(symbol, out var strategy))
            {
                return strategy;
            }
            else
            {
                throw new KeyNotFoundException($"No strategy for symbol {symbol} was loaded");
            }
        }
    }
}