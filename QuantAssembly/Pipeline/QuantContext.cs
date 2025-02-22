using QuantAssembly.Common.Models;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.Models;
using QuantAssembly.Strategy;

namespace QuantAssembly
{
    public class QuantContext : PipelineContext
    {
        public AccountData accountData { get; set; } = new AccountData();
        public List<Position> openPositions { get; set; } = new List<Position>();
        public List<Signal> signals { get; set; } = new List<Signal>();
        public IStrategyProcessor? strategyProcessor { get; set; }
        public HashSet<string> symbolsToEvaluate { get; set; } = new HashSet<string>();
        public List<TransactionResult> transactions { get; set; } = new List<TransactionResult>();
        public List<Position> positionsToOpen { get; set; } = new List<Position>();
    }
}