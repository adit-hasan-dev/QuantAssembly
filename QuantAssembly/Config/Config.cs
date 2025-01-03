using Newtonsoft.Json.Linq;
using System.IO;

namespace QuantAssembly.Config
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
        public string LogFilePath => _config["LogFilePath"].ToString();
        public Dictionary<string, string> TickerStrategyMap => _config["TickerStrategyMap"].ToObject<Dictionary<string, string>>();
        public string LedgerFilePath => _config["LedgerFilePath"].ToString();
        public int PollingIntervalInMs => (int)_config["PollingIntervalInMs"];
        public RiskManagementConfig RiskManagement => _config["RiskManagement"].ToObject<RiskManagementConfig>();
        public Dictionary<string, JObject> CustomProperties => _config["CustomProperties"].ToObject<Dictionary<string, JObject>>();
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8604 // Dereference of a possibly null reference.
    }
}
