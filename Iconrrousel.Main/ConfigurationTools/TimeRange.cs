using System;

namespace Iconrrousel.Main.Configuration
{
    public class TimeRange
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public TimeRange()
        {
            StartTime = new TimeSpan(9, 0, 0);
            EndTime = new TimeSpan(17, 0, 0);
        }

        public TimeRange(TimeSpan startTime, TimeSpan endTime)
        {
            StartTime = startTime;
            EndTime = endTime;
        }

        public bool IsInRange(TimeSpan currentTime)
        {
            if (EndTime > StartTime)
            {
                return currentTime >= StartTime && currentTime <= EndTime;
            }
            else
            {
                return currentTime >= StartTime || currentTime <= EndTime;
            }
        }

        public override string ToString()
        {
            return $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
        }
    }

}
