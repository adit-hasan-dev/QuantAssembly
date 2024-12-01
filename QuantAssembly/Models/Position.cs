namespace QuantAssembly.Models
{
    public enum PositionState
    {
        Open,
        Closed,
    }

    public class Position 
    { 
        public Position(Guid? positionGuid = null) 
        { 
            this.positionGuid = positionGuid ?? Guid.NewGuid(); 
        } 
        
        public string Ticker { get; set; } 
        public PositionState positionState { get; set; } 
        public Guid positionGuid { get; set; } 
        public DateTime OpenTime { get; set; } 
        public double OpenPrice { get; set; } 
        public int Quantity { get; set; } 
        public DateTime CloseTime { get; set; } 
        public double ClosePrice { get; set; } 
        public double ProfitOrLoss { get; set; }
    }
}