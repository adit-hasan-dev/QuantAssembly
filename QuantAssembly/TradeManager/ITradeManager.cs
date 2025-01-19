namespace QuantAssembly.TradeManager
{
    using Models;
    using QuantAssembly.Common.Models;
    using QuantAssembly.Common.Constants;

    public interface ITradeManager
    {
        public Task<TransactionResult> OpenPositionAsync(Position position, OrderType orderType);
        public Task<TransactionResult> ClosePositionAsync(Position position, OrderType orderType); 

        public Task<TransactionResult> GetTransactionStatusAsync();
    }
}