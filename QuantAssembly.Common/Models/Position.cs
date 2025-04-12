using QuantAssembly.Common.Constants;

namespace QuantAssembly.Common.Models
{
    public enum PositionState
    {
        Open,
        Closed,
    }

    public enum ClosePositionReason
    {
        Unknown,
        TakeProfitLevelHit,
        StopLossLevelHit,
        ExitConditionHit
    }

    public class Position
    {
        public double OpenPrice;
        public double ClosePrice;
        public double CurrentPrice;
        public Guid PositionGuid { get; set; } = Guid.NewGuid();
        public string PlatformOrderId { get; set; }
        public string Symbol { get; set; }
        public InstrumentType InstrumentType { get; set; } = InstrumentType.Stock;
        public PositionState State { get; set; }
        public ClosePositionReason CloseReason { get; set; } = ClosePositionReason.Unknown;
        public DateTime OpenTime { get; set; }
        public DateTime CloseTime { get; set; }
        public string Currency { get; set; } = "USD";
        public int Quantity { get; set; }
        public string StrategyName { get; set; }
        public string StrategyDefinition { get; set; }

        public double ProfitOrLoss
        {
            get
            {
                if (State == PositionState.Closed && ClosePrice > 0)
                {
                    return (ClosePrice - OpenPrice) * Quantity;
                }
                else
                {
                    return (CurrentPrice - OpenPrice) * Quantity;
                }
            }
        }

        public double ProfitOrLossPercentage
        {
            get
            {
                if (OpenPrice == 0)
                    return 0;
                return (ProfitOrLoss / OpenPrice) * 100;
            }
        }

        public override string ToString()
        {
            return $"Position Details:\n" +
                   $"Position Guid: {PositionGuid}\n" +
                   $"Platform OrderId: {PlatformOrderId}\n" +
                   $"Symbol: {Symbol}\n" +
                   $"Instrument Type: {InstrumentType}\n" +
                   $"Position State: {State}\n" +
                   $"Open Time: {OpenTime}\n" +
                   $"Close Time: {CloseTime}\n" +
                   $"Currency: {Currency}\n" +
                   $"Open Price: {OpenPrice}\n" +
                   $"Close Price: {ClosePrice}\n" +
                   $"Current Price: {CurrentPrice}\n" +
                   $"Quantity: {Quantity}\n" +
                   $"Profit or Loss: {ProfitOrLoss}\n" +
                   $"Strategy Name: {StrategyName}\n" +
                   $"Strategy Definition: {StrategyDefinition}";
        }
    }
}