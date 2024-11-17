namespace TradingBot.Logging
{
    public interface ILogger
    {
        void LogInfo(string message);
        void LogDebug(string message);
        void LogError(string message);
        void LogError(Exception exception);
        void LogTransaction(string message);

        void SetDebugToggle(bool toggle);
    }
}