namespace QuantAssembly.Logging
{
    public class Logger : ILogger
    {
        private readonly string _logFile;
        private readonly string _transactionLogFile;
        private static readonly object _logLock = new object();
        private static readonly object _transactionLogLock = new object();

        private bool _isDebugEnabled = false;

        public Logger(string logFile, string transactionLogFile)
        {
            _logFile = logFile;
            _transactionLogFile = transactionLogFile;
        }

        public void SetDebugToggle(bool isDebugEnabled)
        {
            _isDebugEnabled = isDebugEnabled;
        }

        public void LogInfo(string message)
        {
            _log($"{DateTime.Now}: INFO: {message}\n");
        }

        public void LogDebug(string message)
        {
            if (_isDebugEnabled)
            {
                _log($"{DateTime.Now}: Debug: {message}\n");
            }
        }

        public void LogError(string message)
        {
            _log($"{DateTime.Now}: ERROR: {message}\n");
        }

        public void LogError(Exception exception)
        {
            _log($"{DateTime.Now}: Exception: {exception.Message}, StackTrace: {exception.StackTrace}");
        }

        public void LogTransaction(string message)
        {
            lock (_transactionLogLock)
            {
                File.AppendAllText(_transactionLogFile, $"{DateTime.Now}: TRANSACTION: {message}\n");
            }
        }

        private void _log(string message)
        {
            lock (_logLock)
            {
                Console.WriteLine(message);
                if (!string.IsNullOrEmpty(_logFile))
                {
                    File.AppendAllText(_logFile, message);
                }
            }
        }
    }

}
