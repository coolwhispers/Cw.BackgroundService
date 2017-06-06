using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cw.BackgroundService
{
    /// <summary>
    /// 排程服務
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Schedule
    {
        /// <summary>
        /// 使用傳入的 BackgroundProcess (這個物件不會執行IDisposable)
        /// </summary>
        /// <typeparam name="T">IBackgroundProcess</typeparam>
        /// <param name="backgroundProcess">The background process.</param>
        /// <param name="scheduleCondition">The schedule condition.</param>
        /// <returns>ScheduleObject</returns>
        public static Schedule Create<T>(T backgroundProcess, Func<DateTime, bool> scheduleCondition)
            where T : class, IBackgroundProcess
        {
            return new ScheduleObject<T>(backgroundProcess, scheduleCondition);
        }

        /// <summary>
        /// 自訂執行條件式
        /// </summary>
        /// <typeparam name="T">IBackgroundProcess</typeparam>
        /// <param name="scheduleCondition">執行條件式(in DateTime LastProcessTime, return bool NeedExecute)</param>
        /// <param name="args">執行參數</param>
        /// <returns>ScheduleArguments</returns>
        public static Schedule Create<T>(Func<DateTime, bool> scheduleCondition, params object[] args)
            where T : class, IBackgroundProcess
        {
            return new ScheduleArguments<T>(scheduleCondition, args);
        }

        /// <summary>
        /// The default stop wait
        /// </summary>
        protected int DefaultStopWait;

        private delegate bool Condition(DateTime lastProcessTime);

        private Condition _scheduleCondition;

        /// <summary>
        /// Initializes a new instance of the <see cref="Schedule"/> class.
        /// </summary>
        /// <param name="scheduleCondition">The schedule condition.</param>
        protected Schedule(Func<DateTime, bool> scheduleCondition)
        {
            DefaultStopWait = 3000;
            _scheduleCondition = new Condition(scheduleCondition);
            LoadLastProcessTime();
            _isRun = false;
            _isComplete = false;
            _isExecute = false;
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

        /// <summary>
        /// 執行物件
        /// </summary>
        protected abstract void Execute();

        /// <summary>
        /// 執行
        /// </summary>
        private void Process()
        {
            do
            {
                LastAliveTime = DateTime.Now;

                if (_scheduleCondition.Invoke(LastProcessTime))
                {
                    try
                    {
                        _isExecute = true;
                        Execute();
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

                GC.Collect();
            }
            while (_isRun);

            _isComplete = true;
        }

        private object _startLock = new object();

        /// <summary>
        /// 開始執行
        /// </summary>
        public void Start()
        {
            lock (_startLock)
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
                    //要求強制停止或沒在執行則直接停止
                    _thread.Abort();
                    _isExecute = false;
                    _isComplete = true;
                }
                else
                {
                    //執行中 等待停止
                    Thread.Sleep(DefaultStopWait);
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
    /// 使用傳入的 BackgroundProcess (這個物件不會執行IDisposable)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="Cw.BackgroundService.Schedule" />
    public sealed class ScheduleObject<T> : Schedule
        where T : class, IBackgroundProcess
    {
        private T _importProcess;

        /// <summary>
        /// 使用傳入的 BackgroundProcess (這個物件不會執行IDisposable)
        /// </summary>
        /// <param name="scheduleCondition">The schedule condition.</param>
        /// <param name="backgroundProcess">The background process.</param>
        public ScheduleObject(T backgroundProcess, Func<DateTime, bool> scheduleCondition) : base(scheduleCondition)
        {
            _importProcess = backgroundProcess;
        }

        /// <summary>
        /// 執行物件
        /// </summary>
        protected override void Execute()
        {
            _importProcess.BackgroundStart();
        }
    }

    /// <summary>
    /// 自訂執行條件式
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="Cw.BackgroundService.Schedule" />
    public sealed class ScheduleArguments<T> : Schedule
        where T : class, IBackgroundProcess
    {
        private object[] _args;

        /// <summary>
        /// 自訂執行條件式
        /// </summary>
        /// <param name="scheduleCondition">執行條件式(in DateTime LastProcessTime, return bool NeedExecute)</param>
        /// <param name="args">執行參數</param>
        public ScheduleArguments(Func<DateTime, bool> scheduleCondition, params object[] args) : base(scheduleCondition)
        {
            _args = args;
        }

        /// <summary>
        /// 執行物件
        /// </summary>
        protected override void Execute()
        {
            var instance = Activator.CreateInstance(typeof(T), _args) as T;

            instance.BackgroundStart();

            if (instance is IDisposable)
            {
                ((IDisposable)instance).Dispose();
            }
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

        private static bool SleepReturn(DateTime nextTime, DateTime now)
        {
            if (now.CompareTo(nextTime) < 0)
            {
                var sleepTimeSpan = nextTime - now;
                if (sleepTimeSpan.TotalMinutes > 1)
                {
                    Thread.Sleep(15000);
                }
                else
                {
                    Thread.Sleep(sleepTimeSpan);
                }
            }

            return DateTime.Now.CompareTo(nextTime) <= 0;
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
                 var nextTime = lastProcessTime.AddSeconds(seconds);

                 return SleepReturn(nextTime, DateTime.Now);
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
            return new Func<DateTime, bool>(lastProcessTime =>
            {
                var now = DateTime.Now;
                var nextTime = now.Date.AddHours(hours).AddMinutes(minutes);

                if (now.CompareTo(nextTime) < 0 && lastProcessTime.Date == nextTime.Date)
                {
                    nextTime = nextTime.AddMonths(1);
                }

                return SleepReturn(nextTime, now);
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

                return SleepReturn(nextTime, now);
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
                var nextTime = new DateTime(now.Year, now.Month, day, hours, minutes, 0);

                if (now.CompareTo(nextTime) < 0 && lastProcessTime.Date == nextTime.Date)
                {
                    nextTime = nextTime.AddMonths(1);
                }

                return SleepReturn(nextTime, now);
            });
        }
    }
}