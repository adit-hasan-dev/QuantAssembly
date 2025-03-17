using Microsoft.SemanticKernel;
using QuantAssembly.Analyst.Models;

namespace QuantAssembly.Analyst.LLM
{
    public interface ILLMService
    {
        Task<TOutput> InvokeLLM<TOutput>(InvokeLLMRequest request);
    }
}