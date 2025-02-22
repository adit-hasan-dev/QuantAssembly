using System.Linq.Expressions;

namespace QuantAssembly.Common.Pipeline
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PipelineStepAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class PipelineStepInputAttribute : Attribute
    {
        public string MemberName { get; set; }

        public PipelineStepInputAttribute(string memberName)
        {
            MemberName = memberName;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class PipelineStepOutputAttribute : Attribute
    {
        public string MemberName;

        public PipelineStepOutputAttribute(string memberName)
        {
            MemberName = memberName;
        }
    }
}