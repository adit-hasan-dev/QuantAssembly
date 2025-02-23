using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Pipeline;

namespace QuantAssembly.Analyst
{
    public class InitStep : IPipelineStep<AnalystContext>
    {
        public async Task Execute(AnalystContext context, ServiceProvider serviceProvider)
        {
            return;
        }
    }
}