namespace QuantAssembly.Common.Pipeline
{
    public class PipelineException : Exception
    {
        public PipelineException(string message) : base(message)
        {
        }
    }

    public class PipelineValidationException : PipelineException
    {
        public PipelineValidationException(string message) : base(message)
        {
            
        }
    }
}