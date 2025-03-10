using Alpaca.Markets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
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

        public async Task<ChatMessageContent> InvokeLLM(InvokeLLMRequest request)
        {
            logger.LogInfo($"[{nameof(AzureOpenAIService)}] Invoking LLM.");
            Kernel kernel = BuildKernel();
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
            string systemMessage = await promptTemplateFactory
                .Create(new PromptTemplateConfig(request.SystemMessage))
                .RenderAsync(kernel, kernelArguments);

            string userPrompt = await promptTemplateFactory
                .Create(new PromptTemplateConfig(request.UserMessage))
                .RenderAsync(kernel, kernelArguments);
            
            
            var chatHistory = new ChatHistory(systemMessage);
            chatHistory.AddUserMessage(userPrompt);

            // Enable planning
            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new() 
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                MaxTokens = 8192
            };
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory: chatHistory,
                executionSettings: openAIPromptExecutionSettings,
                kernel: kernel
                );
            logger.LogInfo($"[{nameof(AzureOpenAIService)}] Succesfully called LLM.");
            return result;
        }

        private Kernel BuildKernel()
        {
            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: config!.DeploymentName,
                apiKey: config.ApiKey,
                endpoint: config.Endpoint
            )
            .Services.AddSingleton(this.marketNewsDataProvider);
            builder.Plugins.AddFromType<MarketNewsPlugin>("market_news");
            return builder.Build();
        }
    }
}