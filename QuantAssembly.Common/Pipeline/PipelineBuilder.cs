using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Config;

namespace QuantAssembly.Common.Pipeline
{
    public class PipelineBuilder<TContext> where TContext : PipelineContext, new()
    {
        private List<Type> stepTypes = new List<Type>();
        private HashSet<string> outputMembers = new HashSet<string>();
        private ServiceProvider serviceProvider;
        private BaseConfig config;

        public PipelineBuilder(ServiceProvider serviceProvider, BaseConfig config)
        {
            this.serviceProvider = serviceProvider;
            this.config = config;
        }

        public PipelineBuilder<TContext> AddStep<TStep>() where TStep : IPipelineStep<TContext>, new()
        {
            var stepType = typeof(TStep);
            var stepAttribute = Attribute.GetCustomAttribute(stepType, typeof(PipelineStepAttribute)) as PipelineStepAttribute;
            if (stepAttribute == null)
            {
                throw new PipelineValidationException($"{nameof(TStep)} is not a valid pipeline step since it is missing the {nameof(PipelineStepAttribute)} attribute");
            }

            var inputAttributes = stepType.GetCustomAttributes(typeof(PipelineStepInputAttribute), false) as PipelineStepInputAttribute[];
            var outputAttributes = stepType.GetCustomAttributes(typeof(PipelineStepOutputAttribute), false) as PipelineStepOutputAttribute[];

            var contextMembers = typeof(TContext)
                .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Where(member => member.MemberType == MemberTypes.Property || member.MemberType == MemberTypes.Field)
                .Select(member => member.Name)
                .ToHashSet();
            inputAttributes?.All(inputAttribute => contextMembers.Contains(inputAttribute.MemberName));
            outputAttributes?.All(outputAttribute => contextMembers.Contains(outputAttribute.MemberName));
            
            // Check for input
            if (stepTypes.Count == 0)
            {
                stepTypes.Add(stepType);
            }
            else
            {
                var inputAvailable = inputAttributes?.All(inputAttribute => outputMembers.Contains(inputAttribute.MemberName)) ?? false;
                if (!inputAvailable)
                {
                    throw new PipelineValidationException($"{nameof(TStep)} requires input members which are not supplied by any previous step");
                }

                stepTypes.Add(stepType);
            }

            // Add output
            outputMembers.UnionWith(outputAttributes?.Select(outputAttribute => outputAttribute.MemberName) ?? new HashSet<string>());

            return this;
        }

        public Pipeline<TContext> Build()
        {
            Pipeline<TContext> pipeline = new Pipeline<TContext>(serviceProvider, config);
            foreach (Type stepType in stepTypes)
            {
                pipeline.steps.Add(Activator.CreateInstance(stepType) as IPipelineStep<TContext>);
            }

            return pipeline;
        }
    }
}