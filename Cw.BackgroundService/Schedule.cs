using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cw.BackgroundService
{
    /// <summary>
    /// 排程物件介面
    /// </summary>
    public interface ISchedule
    {
        /// <summary>
        /// 開始執行
        /// </summary>
        void Start();
        /// <summary>
        /// 要求停止
        /// </summary>
        void Stop();

        /// <summary>
        /// 強制停止
        /// </summary>
        void Abort();
        /// <summary>
        /// 要求停止
        /// </summary>
        Task StopAsync();
    }

    /// <summary>
    /// 排程服務
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Schedule<T> : ISchedule where T : IBackgroundProcess
    {
        /// <summary>
        /// 排程模式
        /// </summary>
        private enum ScheduleMode
        {
            /// <summary>
            /// 自訂
            /// </summary>
            Custom,

            /// <summary>
            /// 間隔幾秒
            /// </summary>
            Interval,

            /// <summary>
            /// 每天
            /// </summary>
            Daily,

            /// <summary>
            /// 每週
            /// </summary>
            Weekly,

            /// <summary>
            /// 每月
            /// </summary>
            Monthly,
        }

        private ScheduleMode _scheduleMode;

        private int _interval;

        /// <summary>
        /// 間隔時間執行
        /// </summary>
        /// <param name="interval">The interval.</param>
        public Schedule(int interval)
        {
            _scheduleMode = ScheduleMode.Interval;
            _interval = interval;
        }

        private int _hours;
        private int _minutes;

        /// <summary>
        /// 每天執行
        /// </summary>
        /// <param name="hours">The hours.</param>
        /// <param name="minutes">The minutes.</param>
        public Schedule(int hours, int minutes)
        {
            _scheduleMode = ScheduleMode.Daily;
            _hours = hours;
            _minutes = minutes;
        }

        private delegate bool CustomSchedule();

        private CustomSchedule _customSchedule;

        /// <summary>
        /// 自訂執行條件式
        /// </summary>
        /// <param name="func">The function.</param>
        public Schedule(Func<bool> func)
        {
            _scheduleMode = ScheduleMode.Custom;
            _customSchedule = new CustomSchedule(func);
        }

        private int _day;

        /// <summary>
        /// 每月
        /// </summary>
        /// <param name="day">The day.</param>
        /// <param name="hours">The hours.</param>
        /// <param name="minutes">The minutes.</param>
        public Schedule(int day, int hours, int minutes)
        {
            _scheduleMode = ScheduleMode.Monthly;
            _day = day;
            _hours = hours;
            _minutes = minutes;
        }

        private DayOfWeek _dayOfWeek;

        /// <summary>
        /// Initializes a new instance of the <see cref="Schedule{T}"/> class.
        /// </summary>
        /// <param name="dayOfWeek">The day of week.</param>
        /// <param name="hours">The hours.</param>
        /// <param name="minutes">The minutes.</param>
        public Schedule(DayOfWeek dayOfWeek, int hours, int minutes)
        {
            _scheduleMode = ScheduleMode.Weekly;
            _dayOfWeek = dayOfWeek;
            _hours = hours;
            _minutes = minutes;
        }

        private bool _isStop;

        private bool _isComplete;

        private bool _isRuning;

        private Thread _thread;

        /// <summary>
        /// 最後執行時間
        /// </summary>
        public DateTime LastProcessTime { get; private set; }

        private T _instance;

        /// <summary>
        /// 執行
        /// </summary>
        internal void Process()
        {
            _isStop = false;
            _isComplete = false;
            _isRuning = false;

            do
            {
                if (RunCondition())
                {
                    _instance = Activator.CreateInstance<T>();

                    _isRuning = true;
                    try
                    {
                        _instance.BackgroundStart();

                        if (_instance is IDisposable)
                        {
                            ((IDisposable)_instance).Dispose();
                        }
                    }
                    catch (Exception)
                    {
                    }

                    _instance = default(T);

                    _isRuning = false;

                    LastProcessTime = DateTime.UtcNow;

                    GC.Collect();
                }
            }
            while (!_isStop);

            _isComplete = true;
        }

        /// <summary>
        /// 開始執行
        /// </summary>
        public void Start()
        {
            _thread = new Thread(new ThreadStart(Process));

            _thread.Start();
        }

        /// <summary>
        /// 要求停止
        /// </summary>
        public void Stop()
        {
            _isStop = true;

            if (_instance != null)
            {
                _instance.BackgroundStop();
                _instance = default(T);
            }

            while (!_isComplete)
            {                
            }
        }

        /// <summary>
        /// 強制停止
        /// </summary>
        public void Abort()
        {
            _isStop = true;

            if (!_isRuning)
            {
                _isRuning = false;
                _thread.Abort();
                _isComplete = true;
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

        /// <summary>
        /// 執行條件式
        /// </summary>
        /// <returns></returns>
        private bool RunCondition()
        {
            switch (_scheduleMode)
            {
                case ScheduleMode.Interval:
                    return IntervalSchedule();

                case ScheduleMode.Daily:
                    return DailySchedule();

                case ScheduleMode.Weekly:
                    return WeeklySchedule();

                case ScheduleMode.Monthly:
                    return MonthlySchedule();

                case ScheduleMode.Custom:
                    return _customSchedule.Invoke();
            }

            _isStop = true;
            return false;
        }

        private bool IntervalSchedule()
        {
            var seconds = (LastProcessTime.AddSeconds(_interval) - DateTime.UtcNow).TotalSeconds;

            var half = _interval / 2;

            if (seconds > half && seconds > 10)
            {
                Thread.Sleep(10000);
            }
            else if (seconds > 1)
            {
                Thread.Sleep(1000);
            }
            else if (seconds > 0)
            {
                Thread.Sleep(Convert.ToInt32(seconds * 1000));
            }

            return (LastProcessTime.AddSeconds(_interval) - DateTime.UtcNow).TotalSeconds < 0;
        }

        private bool DailySchedule()
        {
            var now = DateTime.UtcNow;
            var nextTime = now.Date.AddHours(_hours).AddMinutes(_minutes);
            var compare = now.CompareTo(nextTime);
            if (compare > 0)
            {
                Thread.Sleep(nextTime.AddDays(1) - now);
            }
            else if (compare < 0)
            {
                Thread.Sleep(now - nextTime);
            }

            return true;
        }

        private bool WeeklySchedule()
        {
            var now = DateTime.UtcNow;
            var nextTime = now.Date.AddHours(_hours).AddMinutes(_minutes);

            var compare = now.CompareTo(nextTime);
            while (compare < 0 && nextTime.DayOfWeek != _dayOfWeek)
            {
                nextTime = nextTime.AddDays(1);
            }

            Thread.Sleep(nextTime - now);

            return true;
        }

        private bool MonthlySchedule()
        {
            var now = DateTime.UtcNow;
            var nextTime = new DateTime(now.Year, now.Month, _day, _hours, _minutes, 0);

            var compare = now.CompareTo(nextTime);
            while (compare < 0 && nextTime.DayOfWeek != _dayOfWeek)
            {
                nextTime = nextTime.AddDays(1);
            }

            Thread.Sleep(nextTime - now);

            return true;
        }

        /// <summary>
        /// 儲存執行條件
        /// </summary>
        private void SaveConfig(string str)
        {
            var typeNmae = this.GetType().Name;

            System.IO.File.WriteAllText(string.Format("{0}.sche", typeNmae), str);
        }

        /// <summary>
        /// 取得執行條件
        /// </summary>
        /// <returns></returns>
        private string GetConfig()
        {
            var typeNmae = this.GetType().Name;

            try
            {
                return System.IO.File.ReadAllText(string.Format("{0}.sche", typeNmae));
            }
            catch (Exception)
            {
            }

            return null;
        }
    }
}