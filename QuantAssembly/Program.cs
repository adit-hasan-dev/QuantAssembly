using QuantAssembly.DataProvider;
using QuantAssembly.Impl.IBGW;
using QuantAssembly.Orchestratration;

namespace QuantAssembly
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var quant = new Quant();
            quant.Run();
        }
    }
}
