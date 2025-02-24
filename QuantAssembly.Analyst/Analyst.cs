using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using QuantAssembly.Analyst.Models;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Impl.AlpacaMarkets;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;
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
                .AddStep<PreAIStep>()
                .Build();

            this.logger.LogInfo($"[{nameof(Analyst)}] Pipeline successfully built. Executing pipeline.");
            await pipeline.Execute();
        }

        private void InitializeAnalyst()
        {
            var services = new ServiceCollection();
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
                .BuildServiceProvider();
            
        }
    }
}