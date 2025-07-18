using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Ledger;
using QuantAssembly.Common.Logging;
using QuantAssembly.Core.Models;

namespace QuantAssembly.Core
{
    public abstract class TradingEngine
    {
        protected readonly Config config;
        protected ILogger logger;
        protected ILedger ledger;
        protected ServiceProvider serviceProvider;


        public TradingEngine()
        {
            this.config = ConfigurationLoader.LoadConfiguration<Config>();
            var services = new ServiceCollection();
            InitializeCoreDependencies(services);
            InitializeDependencies(services);
            this.serviceProvider = services.BuildServiceProvider();
        }

        public void InitializeCoreDependencies(ServiceCollection services)
        {
            this.logger = new Logger(this.config, isDevEnv: true);
            this.ledger = new Ledger(this.config.LedgerFilePath, this.logger);
            services
            .AddSingleton<ILogger, Logger>(provider =>
            {
                return (Logger)this.logger;
            })
            .AddSingleton<ILedger, Ledger>(provider =>
            {
                return new Ledger(this.config.LedgerFilePath, this.logger);
            });
        }

        public abstract Task Run();
        protected abstract void InitializeDependencies(ServiceCollection services);

    }
}