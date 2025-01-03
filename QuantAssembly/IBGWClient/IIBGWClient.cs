using IBApi;
using QuantAssembly.Models;
using QuantAssembly.Models.Constants;

namespace QuantAssembly.Impl.IBGW
{
    public interface IIBGWClient
    {
        Task<TransactionResult> PlaceOrder(Position position, OrderType orderType, OrderAction action);
        Task<MarketData> RequestMarketDataAsync(string ticker, int requestId, string instrumentType = "STK", string currency = "USD");
        Task<AccountData> RequestAccountSummaryAsync(string accountId);

        Task<ContractDetails> GetSymbolContractDetailsAsync(string symbol, InstrumentType instrumentType = InstrumentType.Stock, string currency = "USD");
    }

}