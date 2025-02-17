using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Constants;

namespace QuantAssembly.Common.Pipeline
{
    public class PipelineBuilder<TContext> where TContext : PipelineContext, new()
    {
        private List<Type> stepTypes = new List<Type>();
        private Dictionary<string, Type> outputTypes = new Dictionary<string, Type>();
        private ServiceProvider serviceProvider;

        public PipelineBuilder(ServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public PipelineBuilder<TContext> AddStep<TStep>() where TStep : IPipelineStep<TContext>, new()
        {
            var attribute = Attribute.GetCustomAttribute(typeof(TStep), typeof(PipelineStepAttribute)) as PipelineStepAttribute;
            if (attribute == null)
            {
                throw new PipelineValidationException($"{nameof(TStep)} is not a valid pipeline step since it is missing the {nameof(PipelineStepAttribute)} attribute");
            }
            
            // Check for input
            if (stepTypes.Count == 0)
            {
                stepTypes.Add(typeof(TStep));
            }
            else
            {
                // check if the input of TStep is provided by one of the previous steps
                if (attribute.InputType != null && !string.IsNullOrEmpty(attribute.InputMemberName) &&
                outputTypes.TryGetValue(attribute.InputMemberName, out var type) &&
                attribute.InputType == type)
                {
                    stepTypes.Add(typeof(TStep));
                }
                else
                {
                    throw new PipelineValidationException($"{nameof(TStep)} requires input member {attribute.InputMemberName} of type {attribute.InputType} which is not supplied by any previous step");
                }
            }

            // Add output 
            outputTypes.Add(attribute.OutputMemberName, attribute.OutputType);
            return this;
        }

        public Pipeline<TContext> Build()
        {
            ValidateContextType();
            Pipeline<TContext> pipeline = new Pipeline<TContext>(serviceProvider);
            foreach (Type stepType in stepTypes)
            {
                pipeline.steps.Add(Activator.CreateInstance(stepType) as IPipelineStep<TContext>);
            }

            return pipeline;
        }

        private void ValidateContextType()
        {
            // Validate that the TContext object has all the output types as members
            var contextMembers = typeof(TContext)
                .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Where(member => member.MemberType == MemberTypes.Property || member.MemberType == MemberTypes.Field)
                .ToDictionary(member => member.Name, member => member);

            foreach (var memberName in outputTypes.Keys)
            {
                if (!contextMembers.ContainsKey(memberName))
                {
                    throw new PipelineValidationException($"Output member: {memberName} supplied by a step is not a member of {typeof(TContext)}");
                }
            }
        }
    }
}