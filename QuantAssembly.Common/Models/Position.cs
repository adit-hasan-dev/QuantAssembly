using QuantAssembly.Common.Constants;

namespace QuantAssembly.Common.Models
{
    public enum PositionState
    {
        Open,
        Closed,
    }

    public class Position
    {
        private double openPrice;
        private double closePrice;
        private double currentPrice;
        private double profitOrLoss;

        public Position(Guid? positionGuid = null)
        {
            this.PositionGuid = positionGuid ?? Guid.NewGuid();
        }

        public Guid PositionGuid { get; set; }
        public string PlatformOrderId { get; set; }
        public string Symbol { get; set; }
        public InstrumentType InstrumentType { get; set; } = InstrumentType.Stock;
        public PositionState State { get; set; }
        public DateTime OpenTime { get; set; }
        public DateTime CloseTime { get; set; }
        public string Currency { get; set; } = "USD";
        public int Quantity { get; set; }
        public string StrategyName { get; set; }
        public string StrategyDefinition { get; set; }

        public double OpenPrice
        {
            get => openPrice;
            set
            {
                openPrice = value;
                UpdateProfitOrLoss();
            }
        }

        public double ClosePrice
        {
            get => closePrice;
            set
            {
                closePrice = value;
                UpdateProfitOrLoss();
            }
        }

        public double CurrentPrice
        {
            get => currentPrice;
            set
            {
                currentPrice = value;
                UpdateProfitOrLoss();
            }
        }

        public double ProfitOrLoss
        {
            get => profitOrLoss;
            private set => profitOrLoss = value;
        }

        private void UpdateProfitOrLoss()
        {
            if (State == PositionState.Closed && ClosePrice > 0)
            {
                ProfitOrLoss = (ClosePrice - OpenPrice) * Quantity;
            }
            else
            {
                ProfitOrLoss = (CurrentPrice - OpenPrice) * Quantity;
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