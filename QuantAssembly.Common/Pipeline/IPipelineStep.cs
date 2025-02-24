using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;

namespace QuantAssembly.Common.Pipeline
{
    public interface IPipelineStep<TContext> where TContext : new()
    {
        Task Execute(TContext context, ServiceProvider serviceProvider, BaseConfig config);
    }
}