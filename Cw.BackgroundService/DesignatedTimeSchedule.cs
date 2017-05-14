using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cw.BackgroundService
{
    /// <summary>
    /// 指定時間
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="Cw.BackgroundService.Schedule{T}" />
    public class DesignatedTimeSchedule<T> : Schedule<T> where T : IBackgroundProcess
    {
        public enum ScheduleMode
        {
            
        }

        /// <summary>
        /// 每天{hours}:{minutes}執行
        /// </summary>
        /// <param name="hours">The hours.</param>
        /// <param name="minutes">The minutes.</param>
        public DesignatedTimeSchedule(int hours, int minutes)
        {
           
        }

        /// <summary>
        /// 每周{dayOfWeek} {hours}:{minutes}執行
        /// </summary>
        /// <param name="dayOfWeek">The day of week.</param>
        /// <param name="hours">The hours.</param>
        /// <param name="minutes">The minutes.</param>
        public DesignatedTimeSchedule(DayOfWeek dayOfWeek, int hours, int minutes)
        {

        }

        /// <summary>
        /// 每月{day}號{hours}:{minutes}執行
        /// </summary>
        /// <param name="day">The day.</param>
        /// <param name="hours">The hours.</param>
        /// <param name="minutes">The minutes.</param>
        public DesignatedTimeSchedule(int day,int hours, int minutes)
        {

        }
        


        protected override bool RunCondition()
        {
            return true;
        }
    }
}
