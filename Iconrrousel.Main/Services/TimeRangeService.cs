using Iconrrousel.Main.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace Iconrrousel.Main.Services
{
    public class TimeRangeService
    {
        private DispatcherTimer _timer;
        private List<TimeRange> _timeRanges;

        private void Log(string message, [CallerMemberName] string caller = null, Exception ex = null)
        {
            LoggingConfig.EnsureConfigured();
            var baseMsg = $"[{DateTime.Now:O}] TimeRangeService.{caller}: {message}";
            if (ex != null)
            {
                baseMsg += $" | Exception: {ex}";
            }
            Trace.WriteLine(baseMsg);
        }

        public TimeRangeService()
        {
            Log("Initializing TimeRangeService");
            _timeRanges = new List<TimeRange>();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMinutes(1);
            _timer.Tick += Timer_Tick;
        }

        public void UpdateTimeRanges(List<TimeRange> timeRanges)
        {
            Log($"Updating time ranges, count: {timeRanges?.Count ?? 0}");
            _timeRanges = timeRanges ?? new List<TimeRange>();

            if (_timeRanges.Any())
            {
                if (!_timer.IsEnabled)
                {
                    _timer.Start();
                    Log("Timer started");
                }
            }
            else
            {
                if (_timer.IsEnabled)
                {
                    _timer.Stop();
                    Log("Timer stopped");
                }
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            CheckTimeRanges();
        }

        private void CheckTimeRanges()
        {
            try
            {
                var currentTime = DateTime.Now.TimeOfDay;
                var activeRange = _timeRanges.FirstOrDefault(r => r.IsInRange(currentTime));

                if (activeRange != null)
                {
                    Log($"Active time range: {activeRange}");
                }
            }
            catch (Exception ex)
            {
                Log("Error checking time ranges", ex: ex);
            }
        }

        public void Stop()
        {
            Log("Stopping TimeRangeService");
            if (_timer != null && _timer.IsEnabled)
            {
                _timer.Stop();
            }
        }
    }
}
