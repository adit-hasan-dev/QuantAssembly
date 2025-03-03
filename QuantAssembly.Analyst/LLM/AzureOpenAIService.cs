using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI.Chat;
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
        IChatCompletionService chatCompletionService;
        Kernel kernel;
        ILogger logger;
        public AzureOpenAIService(IServiceProvider serviceProvider)
        {
            this.kernel =  serviceProvider.GetRequiredService<Kernel>();
            this.logger = serviceProvider.GetRequiredService<ILogger>();
            this.chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        }

        public async Task<ChatMessageContent> InvokeLLM(InvokeLLMRequest request)
        {
            logger.LogInfo($"[{nameof(AzureOpenAIService)}] Invoking LLM.");
            
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

            var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory);
            logger.LogInfo($"[{nameof(AzureOpenAIService)}] Succesfully called LLM.");
            return result;
        }
    }
}