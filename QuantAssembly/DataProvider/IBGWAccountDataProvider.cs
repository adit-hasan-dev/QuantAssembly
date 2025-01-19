using QuantAssembly.Common.Config;
using QuantAssembly.Impl.IBGW;
using QuantAssembly.Common.Logging;
using QuantAssembly.Models;

namespace QuantAssembly.DataProvider
{
    public class IBGWAccountDataProvider : IAccountDataProvider
    {
        private readonly IIBGWClient iBGWClient;
        private ILogger logger;

        public IBGWAccountDataProvider(IIBGWClient client, IConfig config ,ILogger logger)
        {
            this.logger = logger;
            this.iBGWClient = client;
        }

        public async Task<AccountData> GetAccountDataAsync(string accountId)
        {
            logger.LogInfo($"[IBGWAccountDataProvider::GetAccountDataAsync] Getting Account Data");
            return await iBGWClient.RequestAccountSummaryAsync(accountId);
        }
    }
}