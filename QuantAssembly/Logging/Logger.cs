using QuantAssembly.Config;

namespace QuantAssembly.Logging
{
    public class Logger : ILogger
    {
        private readonly string _logFile;
        private static readonly object _logLock = new object();

        private bool _isDebugEnabled = false;
        private bool isDevEnv = false;

        public Logger(IConfig config, bool isDevEnv = false)
        {
            var dateSuffix = DateTime.Now.ToString("yyyyMMdd");
            _logFile = $"{config.LogFilePath}_{dateSuffix}.log";
            _isDebugEnabled = config.EnableDebugLog;
            this.isDevEnv = isDevEnv;
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

        public void LogWarn(string message)
        {
            _log($"{DateTime.Now}: Warning: {message}\n");
        }

        private void _log(string message)
        {
            lock (_logLock)
            {
                Console.WriteLine(message);
                if (!isDevEnv && !string.IsNullOrEmpty(_logFile))
                {
                    File.AppendAllText(_logFile, message);
                }
            }
        }
    }

}
