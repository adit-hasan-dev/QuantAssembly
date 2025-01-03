using QuantAssembly.Config;
using QuantAssembly.Impl.IBGW;
using QuantAssembly.Logging;
using QuantAssembly.Models;

namespace QuantAssembly.DataProvider
{
    public class IBGWAccountDataProvider : IAccountDataProvider
    {
        private readonly IBGWClient iBGWClient;
        private ILogger logger;

        public IBGWAccountDataProvider(IBGWClient client, IConfig config ,ILogger logger)
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