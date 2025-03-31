using QuantAssembly.Common.Constants;

namespace QuantAssembly.BackTesting
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var backTestEngine = new BackTestEngine();
            await backTestEngine.Run();
        }
    }
}