using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;

namespace QuantAssembly.Analyst
{
    [PipelineStep]
    [PipelineStepInput(nameof(AnalystContext.composedOutput))]
    public class PresentationStep : IPipelineStep<AnalystContext>
    {
        public async Task Execute(AnalystContext context, ServiceProvider serviceProvider, BaseConfig config)
        {
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInfo($"[{nameof(PresentationStep)}] Rendering final output");

            var analystConfig = config as Models.Config;
            string fileName = $"{analystConfig!.OutputFilePath}/OptionsReport_{DateTime.UtcNow:yyyy-MM-ddTHH}.md";
            File.WriteAllText(fileName, context.composedOutput);
            logger.LogInfo($"[{nameof(PresentationStep)}] Successfully saved final output to {fileName}");
        }
    }
}