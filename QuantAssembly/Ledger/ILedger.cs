namespace QuantAssembly.Ledger
{
    using QuantAssembly.Models;
    
    public interface ILedger
    {
        void AddOpenPosition(Position position);
        void ClosePosition(Position position);
        List<Position> GetOpenPositions();
        List<Position> GetClosedPositions();
        void OpenLedger(string filePath);
        void CloseLedger(string filePath);
    }
}

