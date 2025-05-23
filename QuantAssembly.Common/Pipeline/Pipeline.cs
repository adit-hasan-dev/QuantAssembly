using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;

namespace QuantAssembly.Common.Pipeline
{
    public class Pipeline<TContext> where TContext : PipelineContext, new()
    {
        internal List<IPipelineStep<TContext>> steps = new List<IPipelineStep<TContext>>();
        private ServiceProvider serviceProvider;
        private ILogger logger;
        private BaseConfig config;
        private TContext context = new();

        public Pipeline(ServiceProvider serviceProvider, BaseConfig config)
        {
            this.serviceProvider = serviceProvider;
            this.logger = serviceProvider.GetRequiredService<ILogger>();
            this.config = config;
        }

        public async Task Execute()
        {
            RecreateContext();
            foreach (var step in steps)
            {
                logger.LogDebug($"[Pipeline::Execute] Executing step: {step.GetType().Name}");
                await Instrumentation.LatencyLogger.DoWithLatencyLoggerAsync(async () =>
                {
                    await  step.Execute(context, serviceProvider, config);
                },
                step.GetType().Name,
                logger);
                logger.LogDebug($"[Pipeline::Execute] Successfully executed step: {step.GetType().Name}");
            }
        }

        public TContext GetContext()
        {
            return context;
        }

        private void RecreateContext()
        {
            context = new TContext();
        }
    }
}