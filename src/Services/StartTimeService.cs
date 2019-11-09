using System;

namespace MarbleBot.Services
{
    public class StartTimeService
    {
        public DateTime StartTime { get; }

        public StartTimeService(DateTime startTime)
        {
            StartTime = startTime;
        }
    }
}
