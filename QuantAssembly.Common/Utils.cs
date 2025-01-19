using QuantAssembly.Common.Constants;

namespace QuantAssembly.Common
{
    public class Utility
    {
        public static TimeSpan GetTimeSpanFromTimePeriod(TimePeriod timePeriod)
        {
            return timePeriod switch
            {
                TimePeriod.Day => TimeSpan.FromDays(1),
                TimePeriod.FiveDays => TimeSpan.FromDays(5),
                TimePeriod.OneMonth => TimeSpan.FromDays(30),
                TimePeriod.ThreeMonths => TimeSpan.FromDays(90),
                TimePeriod.SixMonths => TimeSpan.FromDays(180),
                TimePeriod.OneYear => TimeSpan.FromDays(365),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}