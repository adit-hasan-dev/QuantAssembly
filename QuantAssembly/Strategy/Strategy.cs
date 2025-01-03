using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        SMA_50,
        SMA_200,
        EMA_50,
        Upper_Band,
        Lower_Band,
        ATR,
        HistoricalHigh,
        HistoricalLow,
        ProfitPercentage,
        LossPercentage
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

    public enum StrategyState
    {
        Active,     // Both buy and sell signals are processed
        SellOnly,   // Only sell signals are processed, ignore buy signals
        Halted      // No signals are processed
    }


    public interface IStrategyCondition
    {
    }

    public class PropertyToValueComparator : IStrategyCondition
    {
        public StrategyOperator Operator { get; set; }
        public StrategyProperty Property { get; set; }
        public long Value { get; set; }
    }

    public class PropertyToPropertyComparator : IStrategyCondition
    {
        public StrategyOperator Operator { get; set; }
        public StrategyProperty LeftHandOperand { get; set; }
        public StrategyProperty RightHandOperand { get; set; }
    }

    public class ConditionGroup
    {
        public LogicalOperator LogicalOperator { get; set; }
        [JsonConverter(typeof(StrategyConditionConverter))]
        public List<IStrategyCondition> Conditions { get; set; } = new List<IStrategyCondition>();
    }

    public class StrategyConditionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // This converter is used for types implementing IStrategyCondition
            return typeof(IStrategyCondition).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Load the JSON array
            JArray jsonArray = JArray.Load(reader);
            var conditions = new List<IStrategyCondition>();

            foreach (var item in jsonArray)
            {
                var jsonObject = item as JObject;
                if (jsonObject != null)
                {
                    IStrategyCondition condition;
                    if (jsonObject["Value"] != null)
                    {
                        // It has a "Value" property, so it must be a PropertyToValueComparator
                        condition = new PropertyToValueComparator();
                    }
                    else if (jsonObject["LeftHandOperand"] != null && jsonObject["RightHandOperand"] != null)
                    {
                        // It has "LeftHandOperand" and "RightHandOperand" properties, so it must be a PropertyToPropertyComparator
                        condition = new PropertyToPropertyComparator();
                    }
                    else
                    {
                        throw new JsonSerializationException("Unknown IStrategyCondition type.");
                    }

                    serializer.Populate(jsonObject.CreateReader(), condition);
                    conditions.Add(condition);
                }
            }

            return conditions;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var conditions = value as List<IStrategyCondition>;
            if (conditions != null)
            {
                writer.WriteStartArray();
                foreach (var condition in conditions)
                {
                    JToken jsonBody = JToken.FromObject(condition, serializer);
                    jsonBody.WriteTo(writer);
                }
                writer.WriteEndArray();
            }
            else
            {
                throw new JsonSerializationException("Expected a list of IStrategyCondition objects.");
            }
        }
    }



    public class Strategy
    {
        public string Name { get; set;}
        public StrategyState State { get; set;} = StrategyState.Halted;
        public ConditionGroup EntryConditions { get; set; }
        public ConditionGroup ExitConditions { get; set; }
        public ConditionGroup StopLossConditions { get; set; }
        public ConditionGroup TakeProfitConditions { get; set; }
    }
}