namespace QuantAssembly.Common.Pipeline
{
    public abstract class PipelineContext
    {
        public DateTime StartTime { get; set; } = DateTime.Now;
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}