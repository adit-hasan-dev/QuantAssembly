using Microsoft.Extensions.DependencyInjection;

namespace QuantAssembly.Common.Pipeline
{
    public interface IPipelineStep<TContext> where TContext : new()
    {
        Task Execute(TContext context, ServiceProvider serviceProvider);
    }
}