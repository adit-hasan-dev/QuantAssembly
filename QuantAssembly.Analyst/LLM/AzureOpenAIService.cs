using Alpaca.Markets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using OpenAI.Chat;
using QuantAssembly.Analyst.DataProvider;
using QuantAssembly.Analyst.Models;
using QuantAssembly.Common.Logging;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace QuantAssembly.Analyst.LLM
{
    public class AzureOpenAIServiceClientConfig
    {
        public string ApiKey { get; set; }
        public string Endpoint { get; set; }
        public string DeploymentName { get; set; }
    }
    public class AzureOpenAIService : ILLMService
    {
        ILogger logger;
        AzureOpenAIServiceClientConfig config;
        IMarketNewsDataProvider marketNewsDataProvider;
        public AzureOpenAIService(IServiceProvider serviceProvider, AzureOpenAIServiceClientConfig config)
        {
            this.logger = serviceProvider.GetRequiredService<ILogger>();
            this.marketNewsDataProvider = serviceProvider.GetRequiredService<IMarketNewsDataProvider>();
            this.config = config;
        }

        public async Task<TOutput> InvokeLLM<TOutput>(InvokeLLMRequest request)
        {
            logger.LogInfo($"[{nameof(AzureOpenAIService)}] Invoking LLM.");
            Kernel kernel = BuildKernel(request.Plugins); // Pass plugins dynamically
            logger.LogDebug($"[{nameof(AzureOpenAIService)}] Adding context:\n {request.Context}");

            var kernelArguments = new KernelArguments()
            {
                ["context"] = request.Context
            };

            foreach (var (name, value) in request.Variables)
            {
                logger.LogDebug($"[{nameof(AzureOpenAIService)}] Adding variable {name}: {value}");
                kernelArguments.Add(name, value);
            }

            var promptTemplateFactory = new KernelPromptTemplateFactory();
            string prompt = await promptTemplateFactory
                .Create(new PromptTemplateConfig(request.Prompt))
                .RenderAsync(kernel, kernelArguments);

            // Enable planning 
#pragma warning disable SKEXP0010
            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                MaxTokens = request.MaxTokens,
                ResponseFormat = typeof(TOutput)
            };
#pragma warning restore SKEXP0010

            // Invoke the combined prompt using InvokePromptAsync
            var promptResult = await kernel.InvokePromptAsync(prompt, new(openAIPromptExecutionSettings));

            logger.LogInfo($"[{nameof(AzureOpenAIService)}] Successfully invoked LLM.");
            var result = promptResult.GetValue<OpenAIChatMessageContent>()?.Content;
            return JsonConvert.DeserializeObject<TOutput>(result);
        }



        private Kernel BuildKernel(Dictionary<string, object> plugins)
        {
            var builder = Kernel.CreateBuilder();

            builder.AddAzureOpenAIChatCompletion(
                deploymentName: config!.DeploymentName,
                apiKey: config.ApiKey,
                endpoint: config.Endpoint
            )
            .Services.AddSingleton(this.marketNewsDataProvider);

            // Dynamically add plugins
            foreach (var (pluginName, pluginInstance) in plugins)
            {
                if (pluginInstance != null)
                {
                    builder.Plugins.AddFromObject(pluginInstance, pluginName);
                }
                else
                {
                    logger.LogWarn($"[{nameof(AzureOpenAIService)}] Failed to add plugin: {pluginName}");
                }
            }

            return builder.Build();
        }

    }
}