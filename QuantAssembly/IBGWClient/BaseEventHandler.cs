using IBApi;

namespace QuantAssembly.Impl.IBGW
{
    public abstract class BaseEventHandler<T>
    {
        protected EWrapperImpl eWrapper;
        protected EClientSocket clientSocket;
        protected TaskCompletionSource<T> taskCompletionSource;
    }
}