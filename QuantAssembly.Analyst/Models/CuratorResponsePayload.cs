using QuantAssembly.Common;

namespace QuantAssembly.Analyst.Models
{
    public class CuratedSymbol
    {
        public string Symbol { get; set; }
        public TrendDirection TrendDirection { get; set; }
        public string Analysis { get; set; }
        public string Catalysts { get; set; }
        public List<string> FunctionsCalled { get; set; }
    }

    public class CuratorResponsePayload
    {
        public List<CuratedSymbol> curatedSymbols { get; set; }
    }
}