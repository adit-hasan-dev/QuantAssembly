using QuantAssembly.Analyst.DataProvider;
using QuantAssembly.Common;

namespace QuantAssembly.Analyst
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using (var httpClient = new HttpClient())
            {
                var client = new PolygonClient(httpClient, "YP3LRG25IGoNzm7o79wcgtsHAVlNGxUz");
                var dataProvider = new PolygonMarketNewsDataProvider(client, null);
                var response = await dataProvider.GetMarketNewsAsync("AAPL", DateTime.UtcNow.AddMonths(-1), 3);
                Console.WriteLine(response[0].Author);
            }
            // var analyst = new Analyst();
            // await analyst.Run();
        }
    }
}