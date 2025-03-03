namespace QuantAssembly.Analyst.Models
{
    public class InvokeLLMRequest
    {
        public string SystemMessage { get; set; }
        public string Context { get; set; }
        public string UserMessage { get; set; }
        public Dictionary<string, string> Variables { get; set; }
     }
}