namespace QuantAssembly.Common.Pipeline
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PipelineStepAttribute : Attribute
    {
        public Type InputType;
        public string InputMemberName;
        public Type OutputType;
        public string OutputMemberName;
    }
}