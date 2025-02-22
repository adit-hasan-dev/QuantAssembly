namespace QuantAssembly.Common.Pipeline
{
    public abstract class PipelineContext
    {
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}