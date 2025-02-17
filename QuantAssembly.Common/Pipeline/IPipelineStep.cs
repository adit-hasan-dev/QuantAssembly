using Microsoft.Extensions.DependencyInjection;

namespace QuantAssembly.Common.Pipeline
{
    public interface IPipelineStep<TContext> where TContext : new()
    {
        void Execute(TContext context, ServiceProvider serviceProvider);
        void ValidatePrerequisites();
    }
}