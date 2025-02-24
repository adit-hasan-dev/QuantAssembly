using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;

namespace QuantAssembly.Analyst
{
    [PipelineStep]
    [PipelineStepInput(nameof(AnalystContext.indicatorData))]
    [PipelineStepOutput(nameof(AnalystContext.candidates))]
    public class PreAIStep : IPipelineStep<AnalystContext>
    {
        public async Task Execute(AnalystContext context, ServiceProvider serviceProvider, BaseConfig config)
        {
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInfo($"[{nameof(PreAIStep)}] Preparing data for {context.indicatorData.Count} symbols");
            var candidates = (from company in context.companies
                      join marketData in context.marketData on company.Symbol equals marketData.Symbol
                      join indicators in context.indicatorData on company.Symbol equals indicators.Symbol
                      select new CandidateData
                      {
                          company = company,
                          marketData = marketData,
                          indicatorData = indicators
                      }).ToList();

            context.candidates = candidates;
        }
    }
}