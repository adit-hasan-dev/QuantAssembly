using Newtonsoft.Json.Linq;

namespace QuantAssembly.Common.Config
{
    public abstract class BaseConfig 
    { 
        public bool EnableDebugLog { get; set; } 
        public string LogFilePath { get; set; } 
        public Dictionary<string, JToken> CustomProperties { get; set; } 

    }
}