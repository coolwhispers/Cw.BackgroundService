using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cw.BackgroundService
{
    /// <summary>
    /// 排程服務
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Schedule<T> where T : IBackgroundProcess
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

        private DateTime _lastProcessTime;

        /// <summary>
        /// 最後執行時間
        /// </summary>
        public DateTime LastProcessTime => _lastProcessTime;

        /// <summary>
        /// 執行
        /// </summary>
        public void Process()
        {
            _isStop = false;
            _isComplete = false;
            _isRuning = false;

            do
            {
                while (RunCondition())
                {
                    var instance = Activator.CreateInstance<T>();

                    _isRuning = true;

                    instance.BackgroundStart();

                    if (instance is IDisposable)
                    {
                        ((IDisposable)instance).Dispose();
                    }

                    _isRuning = false;

                    _lastProcessTime = DateTime.UtcNow;

                    GC.Collect();
                }
            }
            while (_isStop);

            _isComplete = true;
        }

        /// <summary>
        /// 開始執行
        /// </summary>
        private void Start()
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

            while (!_isComplete)
            {
                if (!_isRuning)
                {
                    _isRuning = false;
                    _thread.Abort();
                    _isComplete = true;
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

        /// <summary>
        /// 執行條件式
        /// </summary>
        /// <returns></returns>
        protected abstract bool RunCondition();

        /// <summary>
        /// 儲存執行條件
        /// </summary>
        protected void SaveConfig(string str)
        {
            var typeNmae = this.GetType().Name;

            System.IO.File.WriteAllText(string.Format("{0}.sche", typeNmae), str);
        }

        /// <summary>
        /// 取得執行條件
        /// </summary>
        /// <returns></returns>
        protected string GetConfig()
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