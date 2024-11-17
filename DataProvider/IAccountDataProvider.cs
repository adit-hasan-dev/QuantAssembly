using TradingBot.Models;

namespace TradingBot.DataProvider
{
    public interface IAccountDataProvider
    {
        AccountData GetAccountData();
    }
}
