using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.TradeManager;
using QuantAssembly.Models;
using QuantAssembly.Common.Constants;
using QuantAssembly.Common.Logging;
using QuantAssembly.DataProvider;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Models;

namespace QuantAssembly
{
    [PipelineStep]
    [PipelineStepInput(nameof(QuantContext.positionsToOpen))]
    [PipelineStepOutput(nameof(QuantContext.transactions))]
    public class OpenPositionsStep : IPipelineStep<QuantContext>
    {
        public async Task Execute(QuantContext context, ServiceProvider serviceProvider)
        {
            // We expect the RiskManager to have already computed the position size
        }
    }

}