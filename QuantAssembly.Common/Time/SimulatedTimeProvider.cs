namespace QuantAssembly.Common
{
    using System;
    using System.Threading.Tasks;
    using static QuantAssembly.Common.DateHelper;

    public class SimulatedTimeProvider : ITimeProvider
    {
        private TimeMachine timeMachine;
        public SimulatedTimeProvider(TimeMachine timeMachine)
        {
            this.timeMachine = timeMachine;
        }

        public Task<DateTime> GetCurrentTime()
        {
            return Task.FromResult(timeMachine.GetCurrentTime());
        }

        public Task<bool> IsWithinTradingHoursAsync()
        {
            var status = GetLocalMarketStatus(timeMachine.GetCurrentTime());
            return Task.FromResult(status == MarketStatus.Open);
        }
    }
}