namespace QuantAssembly.Core.Models
{
    public enum TransactionState
    {
        Pending,
        Submitted,
        Completed,
        Failed,
        Cancelled
    }
    public class TransactionResult
    {
        public string OrderId { get; set; }
        public TransactionState TransactionState{ get; set; }
    }
}