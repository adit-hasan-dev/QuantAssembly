namespace QuantAssembly.Strategy
{
    public enum StrategyProperty
    {
        BidPrice,
        AskPrice,
        LatestPrice,
        MACD,
        SignalLine,
        RSI,
        SMA,
        Volume
    }

    public enum StrategyOperator
    {
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        EqualTo
    }

    public enum LogicalOperator
    {
        AND,
        OR
    }

    public class StrategyCondition
    {
        public StrategyProperty Property { get; set; }
        public StrategyOperator Operator { get; set; }
        public long Value { get; set; }
    }

    public class ConditionGroup
    {
        public LogicalOperator LogicalOperator { get; set; }
        public List<StrategyCondition> Conditions { get; set; } = new List<StrategyCondition>();
    }

    public class Strategy
    {
        public ConditionGroup EntryConditions { get; set; }
        public ConditionGroup ExitConditions { get; set; }
        public ConditionGroup StopLossConditions { get; set; }
        public ConditionGroup TakeProfitConditions { get; set; }
    }

}