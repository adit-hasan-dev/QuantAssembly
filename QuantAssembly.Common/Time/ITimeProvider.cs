namespace QuantAssembly.Common
{
    public interface ITimeProvider
    {
        Task<DateTime> GetCurrentTime();
        Task<bool> IsWithinTradingHoursAsync();
    }
}