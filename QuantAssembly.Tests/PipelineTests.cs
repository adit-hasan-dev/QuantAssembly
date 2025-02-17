using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;

namespace QuantAssembly.Tests
{
    public class ToyContext : PipelineContext
    {
        public int InputValue { get; set; } = 5;
        public int IntermediateValue { get; set; }
        public int OutputValue { get; set; }
    }

    [PipelineStep(
        InputType = typeof(Guid),
        InputMemberName = nameof(ToyContext.Id),
        OutputType = typeof(int),
        OutputMemberName = nameof(ToyContext.IntermediateValue))]
    public class ToyStep1 : IPipelineStep<ToyContext>
    {
        public void Execute(ToyContext context, ServiceProvider serviceProvider)
        {
            context.IntermediateValue = context.InputValue * 2;
        }

        public void ValidatePrerequisites()
        {
            // No prerequisites to validate for this toy step
        }
    }

    [PipelineStep(
        InputType = typeof(int),
        InputMemberName = nameof(ToyContext.IntermediateValue),
        OutputType =typeof(int),
        OutputMemberName =nameof(ToyContext.OutputValue))]
    public class ToyStep2 : IPipelineStep<ToyContext>
    {
        public void Execute(ToyContext context, ServiceProvider serviceProvider)
        {
            context.OutputValue = context.IntermediateValue + 10;
        }

        public void ValidatePrerequisites()
        {
            // No prerequisites to validate for this toy step
        }
    }

    [TestClass]
    public class PipelineTests
    {
        [TestMethod]
        public void Test_ToyPipeline()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILogger, ConsoleLogger>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var pipeline = new PipelineBuilder<ToyContext>(serviceProvider)
                .AddStep<ToyStep1>()
                .AddStep<ToyStep2>()
                .Build();

            pipeline.Execute();
            var context = pipeline.GetContext();
            Assert.AreEqual(10, context.IntermediateValue);
            Assert.AreEqual(20, context.OutputValue);
        }
    }

    public class ConsoleLogger : ILogger
    {
        public void LogInfo(string message) => Console.WriteLine($"INFO: {message}");
        public void LogError(Exception ex) => Console.WriteLine($"ERROR: {ex.Message}");

        public void LogDebug(string message)
        {
            LogInfo(message);
        }

        public void LogError(string message)
        {
            throw new NotImplementedException();
        }

        public void LogWarn(string message)
        {
            throw new NotImplementedException();
        }
    }
}
