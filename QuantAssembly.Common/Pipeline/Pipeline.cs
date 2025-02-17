using System;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Common.Logging;

namespace QuantAssembly.Common.Pipeline
{
    public class Pipeline<TContext> where TContext : PipelineContext, new()
    {
        private List<IPipelineStep<TContext>> steps = new List<IPipelineStep<TContext>>();
        private ServiceProvider serviceProvider;
        private ILogger logger;
        private TContext context = new();

        public Pipeline(ServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.logger = serviceProvider.GetRequiredService<ILogger>();
        }
        public Pipeline<TContext> Build()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Building pipeline: ");
            builder.AppendJoin(" -> ", steps.Select(step => step.GetType().Name));
            logger.LogInfo($"[Pipeline::Build] {builder}");
            Validate();
            logger.LogInfo($"[Pipeline::Build] Successfully built pipeline");
            return this;
        }

        public void Execute()
        {
            RecreateContext();
            foreach (var step in steps)
            {
                logger.LogDebug($"[Pipeline::Execute] Executing step: {step.GetType().Name}");
                step.ValidatePrerequisites();
                step.Execute(context, serviceProvider);
                logger.LogDebug($"[Pipeline::Execute] Successfully executed step: {step.GetType().Name}");
            }

        }

        public Pipeline<TContext> AddStep<TStep>() where TStep : IPipelineStep<TContext>, new()
        {
            TStep step = new TStep();
            steps.Add(step);
            return this;
        }

        public TContext GetContext()
        {
            return context;
        }

        private void RecreateContext()
        {
            context = new TContext();
        }

        private void Validate()
        {
            // Validate if the input types of every step are satisfied by the output types of a previous step
            HashSet<string> suppliedMembers = new HashSet<string> { nameof(context.Id), nameof(context.StartTime) };
            List<Type> suppliedTypes = new List<Type> { context.Id.GetType(), context.StartTime.GetType() };

            foreach (IPipelineStep<TContext> step in steps)
            {
                Type stepType = step.GetType();
                var attribute = Attribute.GetCustomAttribute(stepType, typeof(PipelineStepAttribute)) as PipelineStepAttribute;
                if (attribute != null)
                {
                    if (!suppliedMembers.Contains(attribute.InputMemberName))
                    {
                        throw new PipelineValidationException($"Step: {stepType} requires input member: {attribute.InputMemberName} which is not supplied by any previous step");
                    }

                    if (!string.IsNullOrEmpty(attribute.OutputMemberName))
                    {
                        suppliedMembers.Add(attribute.OutputMemberName);
                        suppliedTypes.Add(attribute.OutputType);
                    }
                }
                else
                {
                    throw new PipelineValidationException($"Step: {stepType} does not have the required PipelineStepAttribute values");
                }
            }

            // Validate that the TContext object has all the output types as members
            var contextMembers = typeof(TContext)
                .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Where(member => member.MemberType == MemberTypes.Property || member.MemberType == MemberTypes.Field)
                .ToDictionary(member => member.Name, member => member);

            foreach (var memberName in suppliedMembers)
            {
                if (!contextMembers.ContainsKey(memberName))
                {
                    throw new PipelineValidationException($"Output member: {memberName} supplied by a step is not a member of {typeof(TContext)}");
                }
            }
        }
    }
}