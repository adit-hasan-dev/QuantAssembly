namespace QuantAssembly.Ledger
{
    using Newtonsoft.Json;
    using QuantAssembly.Config;
    using QuantAssembly.Logging;
    using QuantAssembly.Models;
    using System.Collections.Generic;
    using System.IO;

    public class Ledger : ILedger, IDisposable
    {
        private List<Position> openPositions = new List<Position>();
        private List<Position> closedPositions = new List<Position>();

        private string filePath;
        private ILogger logger;
        private bool disposed = false;

        public Ledger(IConfig config, ILogger logger)
        {
            this.filePath = config.LedgerFilePath;
            this.logger = logger;

            if (!File.Exists(filePath))
            {
                string errorMessage = $"[Ledger] Ledger file path: {filePath} does not exist";
                this.logger.LogError(errorMessage);
                throw new FileNotFoundException(errorMessage);
            }

            var ledgerData = JsonConvert.DeserializeObject<LedgerData>(File.ReadAllText(filePath));
            openPositions = ledgerData.OpenPositions ?? new List<Position>();
            closedPositions = ledgerData.ClosedPositions ?? new List<Position>();
            this.logger.LogInfo($"[Ledger] Initialized ledger with file path: {filePath}");
        }

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

        public void CloseLedger()
        {
            var ledgerData = new
            {
                OpenPositions = openPositions,
                ClosedPositions = closedPositions
            };
            File.WriteAllText(filePath, JsonConvert.SerializeObject(ledgerData));
            this.logger.LogInfo($"[Ledger] Closed ledger with file path: {filePath}");
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
            public List<Position> OpenPositions { get; set; }
            public List<Position> ClosedPositions { get; set; }
        }
    }
}