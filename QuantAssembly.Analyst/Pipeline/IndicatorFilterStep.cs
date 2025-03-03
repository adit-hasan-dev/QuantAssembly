using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Models;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.DataProvider;

namespace QuantAssembly.Analyst
{
    [PipelineStep]
    [PipelineStepInput(nameof(AnalystContext.marketData))]
    [PipelineStepOutput(nameof(AnalystContext.indicatorData))]
    public class IndicatorFilterStep : IPipelineStep<AnalystContext>
    {
        public async Task Execute(AnalystContext context, ServiceProvider serviceProvider, BaseConfig config)
        {
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInfo($"[{nameof(IndicatorFilterStep)}] Retrieving indicator data for {context.marketData.Count} symbols");
            
            var indicatorFilterConfig = (config as Models.Config).indicatorFilterConfig;
            var indicatorDataProvider = serviceProvider.GetRequiredService<IIndicatorDataProvider>();

            var indicators = new List<IndicatorData>();
            foreach (var marketData in context.marketData)
            {
                var indicatorData = await indicatorDataProvider.GetIndicatorDataAsync(marketData.Symbol);
                indicators.Add(indicatorData);
            }
            
            indicators = indicators.Where(i => i.RSI < indicatorFilterConfig.RSIOversoldThreshold || i.RSI > indicatorFilterConfig.RSIOverboughtThreshold ).ToList();

            context.indicatorData = indicators;
            logger.LogInfo($"[{nameof(IndicatorFilterStep)}] Filtration complete. {context.indicatorData.Count} symbols remaining.");
        }
    }
}