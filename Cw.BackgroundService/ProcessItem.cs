using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cw.BackgroundService
{
    /// <summary>
    /// Process Item
    /// </summary>
    public class ProcessItem
    {
        private IBackgroundProcess _serivce;
        private Task _task;
        internal ProcessItem(IBackgroundProcess serivce)
        {
            IsServiceStopped = true;
            _serivce = serivce;
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            if (IsServiceStopped)
            {
                _task = new Task(_serivce.BackgroundStart);
                _task.Start();
                IsServiceStopped = false;
            }
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync()
        {
            await Task.Run(() => Start());
        }

        /// <summary>
        /// Gets a value indicating whether this instance is service stopped.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is service stopped; otherwise, <c>false</c>.
        /// </value>
        public bool IsServiceStopped { get; private set; }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            while (!_task.IsCompleted)
            {

            }

            IsServiceStopped = true;
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            await Task.Run(() => Stop());
        }
        
    }
}
