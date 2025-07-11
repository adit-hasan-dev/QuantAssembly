namespace QuantAssembly.Common.Ledger
{
    using Newtonsoft.Json;
    using QuantAssembly.Common.Logging;
    using QuantAssembly.Common.Models;
    using System.Collections.Generic;
    using System.IO;

    public class Ledger : ILedger, IDisposable
    {
        private List<Position> positions = new List<Position>();
        private string filePath;
        private ILogger logger;
        private bool disposed = false;

        public Ledger(string ledgerFilePath, ILogger logger)
        {
            this.filePath = ledgerFilePath;
            this.logger = logger;

            if (!File.Exists(filePath))
            {
                logger.LogWarn($"[Ledger] Ledger file path: {filePath} does not exist. Creating new file.");
                File.Create(filePath).Dispose();
            }

            string fileContent = File.ReadAllText(filePath);
            LedgerData ledgerData;

            if (string.IsNullOrWhiteSpace(fileContent))
            {
                this.logger.LogInfo($"[Ledger] Ledger file is empty. Initializing with an empty ledger.");
                ledgerData = new LedgerData { Positions = new List<Position>() };
            }
            else
            {
                ledgerData = JsonConvert.DeserializeObject<LedgerData>(fileContent);
            }

            positions = ledgerData?.Positions ?? new List<Position>();
            this.logger.LogInfo($"[Ledger] Initialized ledger with file path: {filePath}");
        }


        public void AddOpenPosition(Position position)
        {
            this.logger.LogInfo($"[Ledger::AddOpenPosition] Opening position {position.Symbol} id: {position.PositionGuid}");
            positions.Add(position);
            SaveLedger();
        }

        public void ClosePosition(Position position)
        {
            this.logger.LogInfo($"[Ledger::ClosePosition] Closing position {position.Symbol} id: {position.PositionGuid}");
            position.State = PositionState.Closed;
            SaveLedger();
        }

        public List<Position> GetOpenPositions()
        {
            return positions.Where(p => p.State == PositionState.Open).ToList();
        }

        public List<Position> GetClosedPositions()
        {
            return positions.Where(p => p.State == PositionState.Closed).ToList();
        }

        public void CloseLedger()
        {
            SaveLedger();
            this.logger.LogInfo($"[Ledger] Closed ledger with file path: {filePath}");
        }

        private void SaveLedger()
        {
            var ledgerData = new LedgerData
            {
                Positions = positions
            };
            File.WriteAllText(filePath, JsonConvert.SerializeObject(ledgerData));
            this.logger.LogInfo($"[Ledger] Saved ledger with file path: {filePath}");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Close the ledger before disposing
                CloseLedger();
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Ledger()
        {
            Dispose(false);
        }

        private class LedgerData
        {
            public List<Position> Positions { get; set; }
        }
    }

}