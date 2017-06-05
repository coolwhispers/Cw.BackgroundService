using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cw.BackgroundService
{
    /// <summary>
    /// 排程服務
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Schedule<T> where T : IBackgroundProcess
    {
        private delegate bool Condition(DateTime lastProcessTime);

        private Condition _scheduleCondition;

        /// <summary>
        /// 自訂執行條件式
        /// </summary>
        /// <param name="scheduleCondition">執行條件式(in DateTime LastProcessTime, out bool IfExecute)</param>
        public Schedule(Func<DateTime, bool> scheduleCondition)
        {
            _isRun = false;
            _isComplete = false;
            _isExecute = false;

            _scheduleCondition = new Condition(scheduleCondition);

            LoadLastProcessTime();
        }

        private void LoadLastProcessTime()
        {
            LastProcessTime = DateTime.MinValue;
        }

        private void SaveLastProcessTime()
        {
            LastProcessTime = DateTime.Now;
        }

        private bool _isRun;

        private bool _isComplete;

        private bool _isExecute;

        private Thread _thread;

        /// <summary>
        /// 最後執行時間
        /// </summary>
        public DateTime LastProcessTime { get; private set; }

        /// <summary>
        /// 排程最後存活時間
        /// </summary>
        public DateTime LastAliveTime { get; private set; }

        private void Execute()
        {
            try
            {
                _isExecute = true;

                var instance = Activator.CreateInstance<T>();

                instance.BackgroundStart();

                if (instance is IDisposable)
                {
                    ((IDisposable)instance).Dispose();
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                SaveLastProcessTime();

                _isExecute = false;
            }
        }

        /// <summary>
        /// 執行
        /// </summary>
        internal void Process()
        {
            do
            {
                LastAliveTime = DateTime.Now;

                if (_scheduleCondition.Invoke(LastProcessTime))
                {
                    Execute();
                }

                GC.Collect();
            }
            while (_isRun);

            _isComplete = true;
        }

        private object startLock = new object();

        /// <summary>
        /// 開始執行
        /// </summary>
        public void Start()
        {
            lock (startLock)
            {
                if (_isRun == true)
                {
                    return;
                }

                _isRun = true;
            }

            _thread = new Thread(new ThreadStart(Process));

            _thread.Start();
        }

        /// <summary>
        /// 要求停止
        /// </summary>
        /// <param name="abort">強制停止</param>
        public void Stop(bool abort = false)
        {
            _isRun = false;

            while (!_isComplete)
            {
                if (abort || !_isExecute)
                {
                    _thread.Abort();
                    _isExecute = false;
                    _isComplete = true;
                }
                else
                {
                    Thread.Sleep(3000);
                }
            }
        }

        /// <summary>
        /// 要求停止
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            await Task.Run(() => Stop());
        }
    }

    /// <summary>
    /// 執行條件
    /// </summary>
    public sealed class ScheduleCondition
    {
        private ScheduleCondition()
        {
        }

        private static void Sleep(DateTime nextTime, DateTime now)
        {
            if (now.CompareTo(nextTime) >= 0)
            {
                return;
            }

            var sleepTimeSpan = nextTime - now;
            if (sleepTimeSpan.TotalMinutes > 1)
            {
                Thread.Sleep(15000);
                return;
            }

            Thread.Sleep(nextTime - now);
        }

        private static bool IsExpired(DateTime nextTime)
        {
            return DateTime.Now.CompareTo(nextTime) <= 0;
        }

        private static bool ReturnValue(DateTime nextTime, DateTime now)
        {
            Sleep(nextTime, now);

            return IsExpired(nextTime);
        }

        /// <summary>
        /// 固定間格執行
        /// </summary>
        /// <param name="seconds">間格秒數</param>
        /// <returns></returns>
        public static Func<DateTime, bool> Interval(int seconds)
        {
            return new Func<DateTime, bool>(lastProcessTime =>
             {
                 var now = DateTime.Now;
                 var nextTime = lastProcessTime.AddSeconds(seconds);

                 return ReturnValue(nextTime, now);
             });
        }

        /// <summary>
        /// 每天執行
        /// </summary>
        /// <param name="hours">小時</param>
        /// <param name="minutes">分鐘</param>
        /// <returns></returns>
        public static Func<DateTime, bool> Daily(int hours, int minutes)
        {
            return new Func<DateTime, bool>(x =>
            {
                var now = DateTime.Now;
                var nextTime = now.Date.AddHours(hours).AddMinutes(minutes);
                var compare = now.CompareTo(nextTime);

                return ReturnValue(nextTime, now);
            });
        }

        /// <summary>
        /// 每週執行
        /// </summary>
        /// <param name="dayOfWeek">星期</param>
        /// <param name="hours">小時</param>
        /// <param name="minutes">分鐘</param>
        /// <returns></returns>
        public static Func<DateTime, bool> Weekly(DayOfWeek dayOfWeek, int hours, int minutes)
        {
            return new Func<DateTime, bool>(lastProcessTime =>
            {
                var now = DateTime.Now;
                var nextTime = now.Date.AddHours(hours).AddMinutes(minutes);

                if (now.CompareTo(nextTime) < 0)
                {
                    while (nextTime.DayOfWeek != dayOfWeek || nextTime.Date == lastProcessTime.Date)
                    {
                        nextTime = nextTime.AddDays(1);
                    }
                }

                return ReturnValue(nextTime, now);
            });
        }

        /// <summary>
        /// 每月執行
        /// </summary>
        /// <param name="day">日期</param>
        /// <param name="hours">小時</param>
        /// <param name="minutes">分鐘</param>
        /// <returns></returns>
        public static Func<DateTime, bool> Monthly(int day, int hours, int minutes)
        {
            return new Func<DateTime, bool>(lastProcessTime =>
            {
                var now = DateTime.Now;
                var lastProcessDay = lastProcessTime.Date;
                var nextTime = new DateTime(now.Year, now.Month, day, hours, minutes, 0);

                if (now.CompareTo(nextTime) < 0 && lastProcessDay.Date == nextTime.Date)
                {
                    nextTime = nextTime.AddMonths(1);
                }

                return ReturnValue(nextTime, now);
            });
        }
    }
}