using System.Linq.Expressions;
using System.Reflection;

namespace QuantAssembly.Common.Pipeline
{
    public class PipelineUtil
    {
        internal static MemberInfo GetMemberInfo(Expression<Func<object>> expression)
        {
            if (expression is LambdaExpression lambda)
            {
                if (lambda.Body is MemberExpression member)
                {
                    return member.Member;
                }
                if (lambda.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
                {
                    return unaryMember.Member;
                }
            }
            throw new ArgumentException($"Invalid expression passed as parameter for {nameof(PipeStepInputAttribute)}");
        }

        internal static Type GetMemberType(MemberInfo member)
        {
            return member.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)member).FieldType,
                MemberTypes.Property => ((PropertyInfo)member).PropertyType,
                _ => throw new InvalidOperationException("Unsupported member type")
            };
        }
    }
}