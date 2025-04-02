using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Ledger;
using QuantAssembly.Common.Logging;

namespace QuantAssembly.Core
{
    public abstract class TradingEngine<TConfig> where TConfig : BaseConfig
    {
        protected readonly TConfig config;
        protected ILogger logger;
        protected ILedger ledger;
        protected ServiceProvider serviceProvider;


        public TradingEngine()
        {
            this.config = ConfigurationLoader.LoadConfiguration<TConfig>();
            var services = new ServiceCollection();
            InitializeCoreDependencies(services);
            InitializeDependencies(services);
            this.serviceProvider = services.BuildServiceProvider();
            logger = serviceProvider.GetRequiredService<ILogger>();
            ledger = serviceProvider.GetRequiredService<ILedger>();
        }

        public void InitializeCoreDependencies(ServiceCollection services)
        {
            services
            .AddSingleton<ILogger, Logger>(provider =>
            {
                return new Logger(this.config, isDevEnv: false);
            })
            .AddSingleton<ILedger, Ledger>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger>();
                return new Ledger(this.config.LedgerFilePath, logger);
            });
        }

        public abstract Task Run();
        protected abstract void InitializeDependencies(ServiceCollection services);

    }
}