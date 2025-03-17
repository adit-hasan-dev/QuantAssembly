namespace QuantAssembly.Analyst.Models
{
    public class InvokeLLMRequest
    {
        public string Prompt { get; set; }
        public string Context { get; set; }
        public Dictionary<string, string> Variables { get; set; } = new();
        public Dictionary<string, Type> Plugins { get; set; } = new();
    }
}