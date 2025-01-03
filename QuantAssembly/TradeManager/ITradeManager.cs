namespace QuantAssembly.TradeManager
{
    using Models;
    using QuantAssembly.Models.Constants;

    public interface ITradeManager
    {
        public Task<TransactionResult> OpenPositionAsync(Position position, OrderType orderType);
        public Task<TransactionResult> ClosePositionAsync(Position position, OrderType orderType); 

        public Task<TransactionResult> GetTransactionStatusAsync();
    }
}