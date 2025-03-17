using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using QuantAssembly.Analyst.LLM;
using QuantAssembly.Analyst.Models;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;

namespace QuantAssembly.Analyst
{
    [PipelineStep]
    [PipelineStepInput(nameof(AnalystContext.candidates))]
    [PipelineStepOutput(nameof(AnalystContext.composedOutput))]
    public class LLMStep : IPipelineStep<AnalystContext>
    {
        public async Task Execute(AnalystContext context, ServiceProvider serviceProvider, BaseConfig config)
        {
            var logger = serviceProvider.GetRequiredService<ILogger>();
            var llmService = serviceProvider.GetRequiredService<ILLMService>();

            // Curate candidates
            string systemPrompt = File.ReadAllText("LLM/Curator.System.Prompt.md");
            var userContext = JsonConvert.SerializeObject(context.candidates);

            var llmRequest = new InvokeLLMRequest
            {
                Prompt = systemPrompt,
                Context = userContext
            };
            logger.LogInfo($"[{nameof(LLMStep)}] Invoking LLM to curate stock symbols with {context.candidates.Count()} candidate symbols");
            CuratorResponsePayload curatorResponse = await llmService.InvokeLLM<CuratorResponsePayload>(llmRequest);
            logger.LogInfo($"[{nameof(LLMStep)}] Successfully called Curator agent and received {curatorResponse.curatedSymbols.Count()} curated symbols");

            systemPrompt = File.ReadAllText("LLM/TradeManager.System.Prompt.md");
            // filter options contracts by the symbols from curated companies
            TradeManagerRequestPayload tradeManagerRequest = new ()
            {
                candidates = curatorResponse.curatedSymbols.Select(curatedSymbol =>
                {
                    return new TradeManagerCandidates()
                    {
                        Symbol = curatedSymbol.Symbol,
                        TrendDirection = curatedSymbol.TrendDirection,
                        Analysis = curatedSymbol.Analysis,
                        Catalysts = curatedSymbol.Catalysts,
                        OptionsContracts = context.optionsContractData.Where(contract => {
                            return contract.Symbol.Contains(curatedSymbol.Symbol, StringComparison.InvariantCultureIgnoreCase);
                        }).ToList()
                    };
                }).Where(candidate => candidate.OptionsContracts.Any()).ToList()
            };

            llmRequest = new InvokeLLMRequest
            {
                Prompt = systemPrompt,
                Context = JsonConvert.SerializeObject(tradeManagerRequest),
                Variables = new Dictionary<string, string>{
                    { "totalCapital", "10000" }
                }
            };

            // Generate report
            logger.LogInfo($"[{nameof(LLMStep)}] Invoking LLM to generate final recommendation report with {tradeManagerRequest.candidates.Count()} curated symbols");
            var response = await llmService.InvokeLLM<RiskmanagerResponsePayload>(llmRequest);
            logger.LogInfo($"[{nameof(LLMStep)}] Successfully called TradeManager agent and received final report");

            context.composedOutput = response.riskManagerResponse;
        }
    }
}