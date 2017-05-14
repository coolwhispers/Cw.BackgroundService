using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cw.BackgroundService
{
    /// <summary>
    /// 間隔時間執行
    /// </summary>
    public class IntervalSchedule<T> : Schedule<T> where T : IBackgroundProcess
    {
        private int _seconds;

        private int _sleepSeconds = 60;

        /// <summary>
        /// 執行結束後每一小時執行一次
        /// </summary>
        public IntervalSchedule() : this(3600)
        {
        }

        /// <summary>
        /// 執行結束後每{seconds}秒執行一次
        /// </summary>
        /// <param name="seconds">The seconds.</param>
        public IntervalSchedule(int seconds)
        {
            _seconds = seconds;
        }

        /// <summary>
        /// 執行條件式
        /// </summary>
        /// <returns></returns>
        protected override bool RunCondition()
        {
            var timeSpan = LastProcessTime - DateTime.UtcNow;

            if (timeSpan.TotalSeconds > _seconds && timeSpan.TotalSeconds > _sleepSeconds)
            {
                Thread.Sleep(_sleepSeconds);
            }

            return (LastProcessTime - DateTime.UtcNow).TotalSeconds > _seconds;
        }
    }
}
