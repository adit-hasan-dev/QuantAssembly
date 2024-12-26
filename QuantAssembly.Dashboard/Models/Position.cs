namespace QuantAssembly.Dashboard.Models
{
    public class Position
    {
        public Guid PositionGuid { get; set; }
        public string Symbol { get; set; }
        public string InstrumentType { get; set; }
        public string State { get; set; }
        public DateTime OpenTime { get; set; }
        public DateTime CloseTime { get; set; }
        public string Currency { get; set; }
        public double OpenPrice { get; set; }
        public double ClosePrice { get; set; }
        public double CurrentPrice { get; set; }
        public int Quantity { get; set; }
        public double ProfitOrLoss { get; set; }
        public string StrategyName { get; set; }
        public string StrategyDefinition { get; set; }
    }
}

