using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using QuantAssembly.Analyst.DataProvider;
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
            var marketNewsDataProvider = serviceProvider.GetRequiredService<IMarketNewsDataProvider>();

            // Curator LLM call
            string systemPrompt = File.ReadAllText("LLM/Curator.System.Prompt.md");
            var userContext = JsonConvert.SerializeObject(context.candidates);
            string curatorInputJsonSchema = Common.Utility.ReadJsonAsMinifiedString("LLM/Curator.Input.Schema.json");
            string curatorInputJsonSample = Common.Utility.ReadJsonAsMinifiedString("LLM/Curator.Input.Sample.json");
            string market_news_plugin_schema = Common.Utility.ReadJsonAsMinifiedString("LLM/MarketNewsPlugin.Input.Schema.json");
            string market_news_plugin_sample = Common.Utility.ReadJsonAsMinifiedString("LLM/MarketNewsPlugin.Input.Sample.json");

            var llmRequest = new InvokeLLMRequest
            {
                Prompt = systemPrompt,
                Context = userContext,
                Plugins = new Dictionary<string, object>
                {
                    { "market_news", new MarketNewsPlugin(marketNewsDataProvider) },
                },
                Variables = new Dictionary<string, string>
                {
                    { "curator_input_schema", curatorInputJsonSchema },
                    { "curator_input_sample", curatorInputJsonSample },
                    { "market_news_plugin_schema", market_news_plugin_schema },
                    { "market_news_plugin_sample", market_news_plugin_sample }
                },
                MaxTokens = 8192
            };
            logger.LogInfo($"[{nameof(LLMStep)}] Invoking LLM to curate stock symbols with {context.candidates.Count()} candidate symbols");
            CuratorResponsePayload curatorResponse = await llmService.InvokeLLM<CuratorResponsePayload>(llmRequest);
            logger.LogInfo($"[{nameof(LLMStep)}] Successfully called Curator agent and received {curatorResponse.curatedSymbols.Count()} curated symbols");

            
            // TradeManager LLM call
            systemPrompt = File.ReadAllText("LLM/TradeManager.System.Prompt.md");
            string tradeManagerInputJsonSchema = Common.Utility.ReadJsonAsMinifiedString("LLM/TradeManager.Input.Schema.json");
            string tradeManagerInputJsonSample = Common.Utility.ReadJsonAsMinifiedString("LLM/TradeManager.Input.Sample.json");
            // filter options contracts by the symbols from curated companies
            TradeManagerRequestPayload tradeManagerRequest = new ()
            {
                candidates = curatorResponse.curatedSymbols.Select(curatedSymbol =>
                {
                    var candidate = context.candidates.Where(candidate => candidate.company.Symbol == curatedSymbol.Symbol).FirstOrDefault();
                    return new TradeManagerCandidates()
                    {
                        Symbol = curatedSymbol.Symbol,
                        TrendDirection = curatedSymbol.TrendDirection,
                        Analysis = curatedSymbol.Analysis,
                        Catalysts = curatedSymbol.Catalysts,
                        LatestPrice = candidate.marketData.LatestPrice,
                        AskPrice = candidate.marketData.AskPrice,
                        BidPrice = candidate.marketData.BidPrice,
                        IndicatorData = candidate.indicatorData,
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
                    { "totalCapital", "10000" },
                    { "riskTolerance", "0.2" },
                    { "trademanager_input_schema", tradeManagerInputJsonSchema },
                    { "trademanager_input_sample", tradeManagerInputJsonSample }
                },
                MaxTokens = 10000
            };

            // Generate report
            logger.LogInfo($"[{nameof(LLMStep)}] Invoking LLM to generate final recommendation report with {tradeManagerRequest.candidates.Count()} curated symbols");
            var response = await llmService.InvokeLLM<RiskmanagerResponsePayload>(llmRequest);
            logger.LogInfo($"[{nameof(LLMStep)}] Successfully called TradeManager agent and received final report");

            context.composedOutput = response.riskManagerResponse;
        }
    }
}