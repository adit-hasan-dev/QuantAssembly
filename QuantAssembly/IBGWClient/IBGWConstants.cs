using QuantAssembly.Models.Constants;

namespace QuantAssembly.Impl.IBGW
{
    public class IBGWErrorCodes
    {
        public static readonly List<int> NotificationErrorCodes = new List<int>() { 2104, 2106 };
    }

    public class IBGWTickType
    {
        public const int DelayedBidPrice = 66;
        public const int DelayedAskPrice = 67;
        public const int DelayedLastPrice = 68;
    }

    public enum IBGWOrderStatus
    {
        PendingSubmit,
        PendingCancel,
        PreSubmitted,
        Submitted,
        ApiCancelled,
        Cancelled,
        Filled,
        Inactive
    }

    public class IBGWTypeMap
    {
        public static Dictionary<OrderType, string> OrderTypeToIBGWOrderTypes = new Dictionary<OrderType, string>{
            { OrderType.Market, "MKT"},
            { OrderType.StopLimit, "STP"},
            { OrderType.Limit, "LMT" }
        };
    }

}