namespace TradingBot.Executor
{
    using Models;
    public interface IExecutor
    {
        public (TransactionResult, Position) OpenPosition(EntrySignal signal);
        public TransactionResult ClosePosition(ExitSignal signal); 

        public TransactionResult GetTransactionStatus();
    }
}