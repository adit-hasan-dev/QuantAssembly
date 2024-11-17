namespace TradingBot.Models
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

        public PositionState positionState { get; set; }
        public Guid positionGuid { get; set; }
    }
}