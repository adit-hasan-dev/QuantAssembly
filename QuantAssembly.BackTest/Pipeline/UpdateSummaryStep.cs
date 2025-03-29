using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.BackTesting.Models;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Pipeline;

namespace QuantAssembly.BackTesting
{
    // Updates state after a full iteration
    // including stepping forward in time
    [PipelineStep]
    [PipelineStepOutput(nameof(BacktestContext.backtestSummary))]
    public class UpdateSummaryStep : IPipelineStep<BacktestContext>
    {
        public async Task Execute(BacktestContext context, ServiceProvider serviceProvider, BaseConfig baseConfig)
        {
            
        }
    }

}