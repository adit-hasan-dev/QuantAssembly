using IBApi;

namespace QuantAssembly.Impl.IBGW
{
    public abstract class BaseEventHandler<T>
    {
        protected EWrapperImpl eWrapper;
        protected EClientSocket clientSocket;
        protected TaskCompletionSource<T> taskCompletionSource;
        protected abstract void Detach();
        public abstract void ErrorReceivedHandler(int id, int errorCode, string errorMsg, string advancedOrderRejectJson);
    }
}