using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using QuantAssembly.Analyst.LLM;
using QuantAssembly.Analyst.Models;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;

namespace QuantAssembly.Analyst
{
    [PipelineStep]
    [PipelineStepInput(nameof(AnalystContext.candidates))]
    [PipelineStepOutput(nameof(AnalystContext.composedOutput))]
    public class LLMStep : IPipelineStep<AnalystContext>
    {
        public async Task Execute(AnalystContext context, ServiceProvider serviceProvider, BaseConfig config)
        {
            var logger = serviceProvider.GetRequiredService<ILogger>();
            var llmService = serviceProvider.GetRequiredService<ILLMService>();

            string systemPrompt = File.ReadAllText("LLM/SystemPrompt.md");
            string userPrompt = File.ReadAllText("LLM/UserPrompt.md");
            var userContext = JsonConvert.SerializeObject(context.candidates);

            var llmRequest = new InvokeLLMRequest
            {
                SystemMessage = systemPrompt,
                UserMessage = userPrompt,
                Context = userContext,
                Variables = new Dictionary<string, string>{
                    { "totalCapital", "10000" }
                }
            };

            logger.LogInfo($"[{nameof(LLMStep)}] Invoking LLM ...");
            var response = await llmService.InvokeLLM(llmRequest);
            logger.LogInfo($"[{nameof(LLMStep)}] Received response from LLM: {response.Items}");

            context.composedOutput = response.Content;
        }
    }
}