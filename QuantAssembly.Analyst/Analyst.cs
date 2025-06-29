using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using QuantAssembly.Analyst.DataProvider;
using QuantAssembly.Analyst.LLM;
using QuantAssembly.Common;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Impl.AlpacaMarkets;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;
using QuantAssembly.Core.DataProvider;
using QuantAssembly.DataProvider;

namespace QuantAssembly.Analyst
{
    public class Analyst
    {
        private ILogger logger;
        private readonly Models.Config config;
        private ServiceProvider serviceProvider;

        public Analyst()
        {
            this.config = ConfigurationLoader.LoadConfiguration<Models.Config>();
        }

        public async Task Run()
        {
            InitializeAnalyst();
            this.logger = serviceProvider.GetRequiredService<ILogger>();

            this.logger.LogInfo($"[{nameof(Analyst)}] Building pipeline...");
            var pipeline = new PipelineBuilder<AnalystContext>(this.serviceProvider, this.config)
                .AddStep<InitStep>()
                .AddStep<StockDataFilterStep>()
                .AddStep<IndicatorFilterStep>()
                .AddStep<OptionsFilterStep>()
                .AddStep<PreAIStep>()
                .AddStep<LLMStep>()
                .AddStep<PublishStep>()
                .Build();

            this.logger.LogInfo($"[{nameof(Analyst)}] Pipeline successfully built. Executing pipeline.");
            await pipeline.Execute();
        }

        private void InitializeAnalyst()
        {
            var services = new ServiceCollection();
            AzureOpenAIServiceClientConfig openAIServiceConfig;
            if (config.CustomProperties.TryGetValue(nameof(AzureOpenAIServiceClientConfig), out var openAIServiceConfigJson))
            {
                openAIServiceConfig = JsonConvert.DeserializeObject<AzureOpenAIServiceClientConfig>(openAIServiceConfigJson.ToString());
            }
            else
            {
                throw new Exception("AlpacaMarketsClientConfig not found in config");
            }

            this.serviceProvider = services
                .AddSingleton<ILogger, Logger>(provider => {
                    return new Logger(config, isDevEnv: true);
                })
                .AddSingleton<AlpacaMarketsClient>(provider =>
                {
                    if (config.CustomProperties.TryGetValue(nameof(AlpacaMarketsClientConfig), out var alpacaConfigJson))
                    {
                        var alpacaConfig = JsonConvert.DeserializeObject<AlpacaMarketsClientConfig>(alpacaConfigJson.ToString());
                        return new AlpacaMarketsClient(alpacaConfig);
                    }
                    else
                    {
                        throw new Exception("AlpacaMarketsClientConfig not found in config");
                    }
                })
                .AddSingleton<IIndicatorDataProvider, StockIndicatorsDataProvider>(provider =>
                 {
                    var alpacaClient = provider.GetRequiredService<AlpacaMarketsClient>();
                    var logger = provider.GetRequiredService<ILogger>();
                    return new StockIndicatorsDataProvider(alpacaClient, logger);
                })
                .AddSingleton<IOptionsChainDataProvider, AlpacaOptionsChainDataProvider>(provider => {
                    var alpacaClient = provider.GetRequiredService<AlpacaMarketsClient>();
                    var logger = provider.GetRequiredService<ILogger>();
                    return new AlpacaOptionsChainDataProvider(alpacaClient, logger);
                })
                .AddSingleton<PolygonClient>(provider => {
                    if (config.CustomProperties.TryGetValue(nameof(PolygonClientConfig), out var clientConfig))
                    {
                        var polygonConfig = JsonConvert.DeserializeObject<PolygonClientConfig>(clientConfig.ToString());
                        return new PolygonClient(new HttpClient(), polygonConfig.apiKey, polygonConfig.maxCallsPerMin);
                    }
                    else
                    {
                        throw new Exception("PolygonClientApiKey not found in config");
                    }
                })
                .AddSingleton<IMarketNewsDataProvider, PolygonMarketNewsDataProvider>(provider => {
                    var polygonClient = provider.GetRequiredService<PolygonClient>();
                    var logger = provider.GetRequiredService<ILogger>();
                    
                    return new PolygonMarketNewsDataProvider(polygonClient, logger);
                })
                .AddSingleton<ILLMService, AzureOpenAIService>(provider => {
                    return new AzureOpenAIService(provider, openAIServiceConfig);
                })
                .BuildServiceProvider();
        }
    }
}