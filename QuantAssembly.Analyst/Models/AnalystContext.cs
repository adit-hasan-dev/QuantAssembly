using QuantAssembly.Common.Pipeline;

namespace QuantAssembly.Analyst
{
    public class AnalystContext : PipelineContext
    {
        public List<Company> companies { get; set; } = new List<Company>();
    }
}