using QuantAssembly.Common.Models;

namespace QuantAssembly.Common.Ledger
{
    public interface ILedger
    {
        void AddOpenPosition(Position position);
        void ClosePosition(Position position);
        List<Position> GetOpenPositions();
        List<Position> GetClosedPositions();
    }
}

