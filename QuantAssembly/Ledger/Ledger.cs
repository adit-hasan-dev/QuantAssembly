namespace QuantAssembly.Ledger
{
    using Newtonsoft.Json;
    using QuantAssembly.Models;
    using System.Collections.Generic;
    using System.IO;

    public class Ledger : ILedger
    {
        private List<Position> openPositions = new List<Position>();
        private List<Position> closedPositions = new List<Position>();

        public void AddOpenPosition(Position position)
        {
            openPositions.Add(position);
        }

        public void ClosePosition(Position position)
        {
            position.positionState = PositionState.Closed;
            openPositions.Remove(position);
            closedPositions.Add(position);
        }

        public List<Position> GetOpenPositions()
        {
            return openPositions;
        }

        public List<Position> GetClosedPositions()
        {
            return closedPositions;
        }

        public void CloseLedger(string filePath)
        {
            var ledgerData = new
            {
                OpenPositions = openPositions,
                ClosedPositions = closedPositions
            };
            File.WriteAllText(filePath, JsonConvert.SerializeObject(ledgerData));
        }

        public void OpenLedger(string filePath)
        {
            if (File.Exists(filePath))
            {
                var ledgerData = JsonConvert.DeserializeObject<LedgerData>(File.ReadAllText(filePath));
                openPositions = ledgerData.OpenPositions ?? new List<Position>();
                closedPositions = ledgerData.ClosedPositions ?? new List<Position>();
            }
        }

        private class LedgerData
        {
            public List<Position> OpenPositions { get; set; }
            public List<Position> ClosedPositions { get; set; }
        }
    }
}