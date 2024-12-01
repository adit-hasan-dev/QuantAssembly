namespace QuantAssembly.Impl.IBGW
{
    public class IBGWErrorCodes 
    {
        public static readonly List<int> NotificationErrorCodes = new List<int>() {2104, 2106};
    }

    public class IBGWTickType
    {
        public const int DelayedBidPrice = 66;
        public const int DelayedAskPrice = 67;
        public const int DelayedLastPrice = 68;
    }
}