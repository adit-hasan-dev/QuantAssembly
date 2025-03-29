using QuantAssembly.BackTesting.Utility;
using QuantAssembly.Common.Constants;
using QuantAssembly.Common.Models;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.Models;
using QuantAssembly.Strategy;

namespace QuantAssembly.BackTesting.Models
{
    // TODO: Derive from QuantContext???
    public class BacktestContext : PipelineContext
    {
        public AccountData accountData { get; set; } = new AccountData();
        public IStrategyProcessor? strategyProcessor { get; set; }
        public BackTestSummary backtestSummary { get; set; }

        public TimePeriod timePeriod { get; init; }
        public StepSize stepSize { get; init; }
        public List<Position> openPositions { get; set; } = new List<Position>();
        public List<Signal> signals { get; set; } = new List<Signal>();
        public HashSet<string> symbolsToEvaluate { get; set; } = new HashSet<string>();
        public List<TransactionResult> transactions { get; set; } = new List<TransactionResult>();
        public List<Position> positionsToOpen { get; set; } = new List<Position>();

    }
}