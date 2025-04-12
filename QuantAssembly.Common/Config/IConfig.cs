using Newtonsoft.Json.Linq;

namespace QuantAssembly.Common.Config
{
    public abstract class BaseConfig 
    { 
        public bool EnableDebugLog { get; init; } 
        public string LogFilePath { get; init; } 
        public string LedgerFilePath { get; init;} 

        public Dictionary<string, JToken> CustomProperties { get; init; } 

    }
}