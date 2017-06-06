using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cw.BackgroundService
{
    /// <summary>
    /// 排程服務
    /// </summary>
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
            _scheduleCondition = new Condition(scheduleCondition);
            Init();
        }

        private void Init()
        {
            DefaultStopWait = 3000;
            _isRun = false;
            _isComplete = false;
            _isExecute = false;
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
        public bool IsAlive => _thread == null ? false : _thread.IsAlive;

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
            if (!IsAlive)
            {
                Init();
            }

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
        private static Func<DateTime, bool> Interval(int seconds)
        {
            return lastProcessTime => SleepReturn(lastProcessTime.AddSeconds(seconds), DateTime.Now);
        }

        /// <summary>
        /// 每N小時執行
        /// </summary>
        /// <param name="minutes">分鐘</param>
        /// <param name="hours">每N小時</param>
        /// <returns></returns>
        public static Func<DateTime, bool> DefiniteHours(int hours = 1, int minutes = 0)
        {
            if (hours < 1) { hours = 1; }
            if (minutes < 0) { minutes = 0; }

            return lastProcessTime =>
            {
                var now = DateTime.Now;
                var nextTime = now.Date.AddMinutes(minutes).AddHours(hours);

                while (now.CompareTo(nextTime) < 0 && lastProcessTime > nextTime)
                {
                    nextTime = nextTime.AddHours(hours);
                }

                return SleepReturn(nextTime, now);
            };
        }

        /// <summary>
        /// 每N天執行
        /// </summary>
        /// <param name="hours">時</param>
        /// <param name="minutes">分</param>
        /// <param name="day">日期</param>
        /// <param name="dayOfWeeks">The day of weeks.</param>
        /// <returns></returns>
        public static Func<DateTime, bool> DefiniteDays(int hours = 0, int minutes = 0, int day = 1, params DayOfWeek[] dayOfWeeks)
        {
            if (day < 1) { day = 1; }
            if (hours < 0) { hours = 0; }
            if (minutes < 0) { minutes = 0; }

            return lastProcessTime =>
            {
                var now = DateTime.Now;
                var nextTime = now.Date.AddMinutes(minutes).AddHours(hours).AddDays(day);

                while (InWeekly(nextTime, dayOfWeeks))
                {
                    nextTime.AddDays(day);
                }

                while (now.CompareTo(nextTime) < 0 && lastProcessTime > nextTime)
                {
                    nextTime = nextTime.AddDays(day);
                }

                return SleepReturn(nextTime, now);
            };
        }

        private static bool InWeekly(DateTime nextTime, DayOfWeek[] dayOfWeeks)
        {
            var list = dayOfWeeks == null ? new List<DayOfWeek>() : dayOfWeeks.ToList();

            return list.Count > 0 ? dayOfWeeks.Contains(nextTime.DayOfWeek) : true;
        }
    }
}