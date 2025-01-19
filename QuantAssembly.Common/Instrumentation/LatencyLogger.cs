using System.Diagnostics;
using QuantAssembly.Common.Logging;

namespace QuantAssembly.Common.Instrumentation
{
    public static class LatencyLogger
    {
        public static void DoWithLatencyLogger(Action action, string tag, ILogger logger)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                action();
                logger.LogDebug($"{tag} took {stopwatch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        public static async Task DoWithLatencyLoggerAsync(Func<Task> func, string tag, ILogger logger, string logLevel = "info")
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await func();
                if (logLevel.Equals("debug", StringComparison.InvariantCultureIgnoreCase))
                {
                    logger.LogDebug($"{tag} took {stopwatch.ElapsedMilliseconds} ms");
                }
                else
                {
                    logger.LogInfo($"{tag} took {stopwatch.ElapsedMilliseconds} ms");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }
    }
}