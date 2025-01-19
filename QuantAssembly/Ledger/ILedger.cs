namespace QuantAssembly.Ledger
{
    using QuantAssembly.Common.Models;
    
    public interface ILedger
    {
        void AddOpenPosition(Position position);
        void ClosePosition(Position position);
        List<Position> GetOpenPositions();
        List<Position> GetClosedPositions();
    }
}

