using QuantAssembly.Common.Constants;

namespace QuantAssembly.Common
{
    public class TimeMachine
    {
        private DateTime currentTime;
        public readonly TimePeriod timePeriod;
        public readonly StepSize stepSize;
        public readonly DateTime startTime; 
        public readonly DateTime endTime;

        public TimeMachine(TimePeriod timePeriod, StepSize stepSize, DateTime startTime)
        {
            this.timePeriod = timePeriod;
            this.stepSize = stepSize;
            this.startTime = startTime;
            this.currentTime = this.startTime;
            this.endTime = startTime.Add(Common.Utility.GetTimeSpanFromTimePeriod(timePeriod));
        }

        public void StepForward()
        {
            currentTime = currentTime.Add(GetTimeSpanFromStepSize(stepSize));
        }

        public DateTime GetCurrentTime()
        {
            return currentTime;
        }

        public TimeMachine Clone()
        {
            return new TimeMachine(this.timePeriod, this.stepSize, this.startTime);
        }

        private TimeSpan GetTimeSpanFromStepSize(StepSize size)
        {
            return size switch
            {
                StepSize.OneMinute => TimeSpan.FromMinutes(1),
                StepSize.ThirtyMinutes => TimeSpan.FromMinutes(30),
                StepSize.OneHour => TimeSpan.FromHours(1),
                StepSize.OneDay => TimeSpan.FromDays(1),
                StepSize.OneWeek => TimeSpan.FromDays(7),
                StepSize.OneMonth => TimeSpan.FromDays(30),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

}