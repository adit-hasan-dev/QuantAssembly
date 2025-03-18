namespace QuantAssembly.Analyst.Models
{
    public class InvokeLLMRequest
    {
        public int MaxTokens { get; set; }
        public string Prompt { get; set; }
        public string Context { get; set; }
        public Dictionary<string, string> Variables { get; set; } = new();
        public Dictionary<string, object> Plugins { get; set; } = new();
    }
}