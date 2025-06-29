using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Models;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.DataProvider;

namespace QuantAssembly.Analyst
{
    [PipelineStep]
    [PipelineStepInput(nameof(AnalystContext.indicatorData))]
    [PipelineStepOutput(nameof(AnalystContext.optionsContractData))]
    public class OptionsFilterStep : IPipelineStep<AnalystContext>
    {
        public async Task Execute(AnalystContext context, ServiceProvider serviceProvider, BaseConfig config)
        {
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInfo($"[{nameof(OptionsFilterStep)}] Retrieving options chain data for {context.indicatorData.Count} symbols");

            var optionsDataProvider = serviceProvider.GetRequiredService<IOptionsChainDataProvider>();
            List<OptionsContractData> optionsContracts = new List<OptionsContractData>();

            foreach (var stock in context.indicatorData)
            {
                var contract = await optionsDataProvider.GetOptionsChainDataAsync(stock.Symbol, 5);
                optionsContracts.AddRange(contract);
            }

            logger.LogInfo($"[{nameof(OptionsFilterStep)}] Retrieved {optionsContracts.Count} contracts across {context.indicatorData.Count} symbols");
            logger.LogInfo($"[{nameof(OptionsFilterStep)}] Filtering options contracts...");

            var optionsFilterConfig = (config as Models.Config).optionsContractFilterConfig;
            var filteredData = optionsContracts.AsQueryable();

            // Apply all filters safely
            if (optionsFilterConfig.MinimumOpenInterest.HasValue)
                filteredData = filteredData.Where(c => c.OpenInterest >= optionsFilterConfig.MinimumOpenInterest.Value);

            if (optionsFilterConfig.MaxBidAskSpread.HasValue)
                filteredData = filteredData.Where(c =>
                    c.AskPrice > 0 &&
                    ((c.AskPrice - c.BidPrice) / c.AskPrice) <= optionsFilterConfig.MaxBidAskSpread.Value);

            if (optionsFilterConfig.MinImpliedVolatility.HasValue)
                filteredData = filteredData.Where(c => c.ImpliedVolatility >= optionsFilterConfig.MinImpliedVolatility.Value);

            if (optionsFilterConfig.MaxImpliedVolatility.HasValue)
                filteredData = filteredData.Where(c => c.ImpliedVolatility <= optionsFilterConfig.MaxImpliedVolatility.Value);

            if (optionsFilterConfig.MinDelta.HasValue)
                filteredData = filteredData.Where(c => c.Delta >= optionsFilterConfig.MinDelta.Value);

            if (optionsFilterConfig.MaxDelta.HasValue)
                filteredData = filteredData.Where(c => c.Delta <= optionsFilterConfig.MaxDelta.Value);

            if (optionsFilterConfig.MinGamma.HasValue)
                filteredData = filteredData.Where(c => c.Gamma >= optionsFilterConfig.MinGamma.Value);

            if (optionsFilterConfig.MaxGamma.HasValue)
                filteredData = filteredData.Where(c => c.Gamma <= optionsFilterConfig.MaxGamma.Value);

            if (optionsFilterConfig.MinTheta.HasValue)
                filteredData = filteredData.Where(c => c.Theta >= optionsFilterConfig.MinTheta.Value);

            if (optionsFilterConfig.MaxTheta.HasValue)
                filteredData = filteredData.Where(c => c.Theta <= optionsFilterConfig.MaxTheta.Value);

            if (optionsFilterConfig.MinVega.HasValue)
                filteredData = filteredData.Where(c => c.Vega >= optionsFilterConfig.MinVega.Value);

            if (optionsFilterConfig.MaxVega.HasValue)
                filteredData = filteredData.Where(c => c.Vega <= optionsFilterConfig.MaxVega.Value);

            if (optionsFilterConfig.MinRho.HasValue)
                filteredData = filteredData.Where(c => c.Rho >= optionsFilterConfig.MinRho.Value);

            if (optionsFilterConfig.MaxRho.HasValue)
                filteredData = filteredData.Where(c => c.Rho <= optionsFilterConfig.MaxRho.Value);

            // Placeholder for volume if/when added
            if (optionsFilterConfig.MinimumVolume.HasValue)
            {
                logger.LogWarn($"[OptionsFilterStep] Volume filtering is configured but volume data is not available. Skipping.");
                // filteredData = filteredData.Where(c => c.Volume >= optionsFilterConfig.MinimumVolume.Value);
            }

            logger.LogInfo($"[{nameof(OptionsFilterStep)}] Filtered contracts: {filteredData.Count()}");
            context.optionsContractData = filteredData.ToList();
        }

    }
}