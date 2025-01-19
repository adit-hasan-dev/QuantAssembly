using QuantAssembly.BackTesting.Utility;
using QuantAssembly.Common.Constants;

namespace QuantAssembly.BackTesting
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var timePeriod = TimePeriod.OneYear;
            var stepSize = StepSize.ThirtyMinutes;
            var backTestEngine = new BackTestEngine(timePeriod, stepSize);
            await backTestEngine.Run();
        }
    }
}