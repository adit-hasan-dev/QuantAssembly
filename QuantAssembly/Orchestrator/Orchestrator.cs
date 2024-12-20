namespace QuantAssembly.Orchestratration
{
    using Newtonsoft.Json;
    using QuantAssembly.Logging;
    using QuantAssembly.Models;
    using QuantAssembly.Strategy;
    public class Orchestrator
    {
        private Dictionary<string, Strategy> strategies = new Dictionary<string, Strategy>();
        private ILogger logger;
        public Orchestrator(ILogger logger)
        {
            this.logger = logger;
        }

        public void LoadStrategy(string stockTicker, string filePath)
        {
            logger.LogInfo($"Loading strategy for ticker: {stockTicker} from {filePath}");
            string fileContent = File.ReadAllText(filePath);
            var strategy = JsonConvert.DeserializeObject<Strategy>(fileContent);

            if (strategy != null && ValidateStrategy(strategy))
            {
                strategies[stockTicker] = strategy;
            }
            else
            {
                logger.LogError($"[Orchestrator::LoadStrategy] Failed to deserialize or validate strategy for {stockTicker} from {filePath}");
                throw new InvalidOperationException("Invalid strategy definition.");
            }
            logger.LogInfo($"Successfully loaded strategy for ticker: {stockTicker} from {filePath}");
        }


        public bool ShouldOpen(MarketData marketData, AccountData accountData, string stockTicker)
        {
            if (!strategies.TryGetValue(stockTicker, out var strategy))
            {
                throw new InvalidOperationException("Strategy not found.");
            }

            return EvaluateConditionGroup(strategy.EntryConditions, marketData) || EvaluateConditionGroup(strategy.TakeProfitConditions, marketData);
        }

        public bool ShouldClose(MarketData marketData, AccountData accountData, string stockTicker)
        {
            if (!strategies.TryGetValue(stockTicker, out var strategy))
            {
                throw new InvalidOperationException("Strategy not found.");
            }

            return EvaluateConditionGroup(strategy.StopLossConditions, marketData) || EvaluateConditionGroup(strategy.ExitConditions, marketData);
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

        private bool EvaluateConditionGroup(ConditionGroup group, MarketData marketData)
        {
            if (group == null || !group.Conditions.Any()) return false;

            bool result = EvaluateCondition(group.Conditions[0], marketData);

            for (int i = 1; i < group.Conditions.Count; i++)
            {
                var condition = group.Conditions[i];
                bool conditionResult = EvaluateCondition(condition, marketData);

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

        private bool EvaluateCondition(IStrategyCondition condition, MarketData marketData)
        {
            if (condition is PropertyToValueComparator valueComparator)
            {
                var propertyValue = GetPropertyValue(marketData, valueComparator.Property);
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
                var leftValue = GetPropertyValue(marketData, propertyComparator.LeftHandOperand);
                var rightValue = GetPropertyValue(marketData, propertyComparator.RightHandOperand);
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

        private double GetPropertyValue(MarketData marketData, StrategyProperty property)
        {
            return property switch
            {
                StrategyProperty.BidPrice => marketData.BidPrice,
                StrategyProperty.AskPrice => marketData.AskPrice,
                StrategyProperty.LatestPrice => marketData.LatestPrice,
                StrategyProperty.MACD => marketData.MACD,
                StrategyProperty.SignalLine => marketData.Signal,
                StrategyProperty.RSI => marketData.RSI,
                StrategyProperty.SMA => marketData.SMA,
                _ => throw new InvalidOperationException("Invalid property")
            };
        }
    }
}