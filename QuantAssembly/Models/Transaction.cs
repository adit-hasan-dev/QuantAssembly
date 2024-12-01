namespace QuantAssembly.Models
{
    public enum TransactionState
    {
        Sent,
        Filled,
        Closed
    }
    public class TransactionResult
    {
        public string OrderId { get; set; }
        public TransactionState transactionState{ get; set; }
    }
}