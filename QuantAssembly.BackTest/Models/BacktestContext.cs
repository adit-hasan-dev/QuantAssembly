using QuantAssembly.Common.Constants;
using QuantAssembly.Core.Models;

namespace QuantAssembly.BackTesting.Models
{
    public class BacktestContext : QuantContext
    {
        public TimePeriod timePeriod { get; init; }
        public StepSize stepSize { get; init; }
    }
}