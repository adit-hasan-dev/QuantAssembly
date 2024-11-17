using Newtonsoft.Json.Linq;
using System.IO;

namespace TradingBot.Config
{
    public class Config : IConfig
    {
        private readonly JObject _config;

        public Config()
        {
            _config = JObject.Parse(File.ReadAllText("appsettings.json"));
        }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.

        public string AccountId => _config["AccountId"].ToString();

        public bool EnableDebugLog => _config["EnableDebugLog"].ToString().Contains("true");
        public double MaxPortfolioEngagement => (double)_config["MaxPortfolioEngagement"];
        public double MaxSingleTradeAllocation => (double)_config["MaxSingleTradeAllocation"];
        public double GlobalStopLoss => (double)_config["GlobalStopLoss"];
        public string[] Tickers => _config["Tickers"].ToObject<string[]>();

#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8604 // Dereference of a possibly null reference.
    }
}
