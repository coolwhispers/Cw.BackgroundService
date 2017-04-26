using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cw.BackgroundService
{
    /// <summary>
    /// Process List
    /// </summary>
    /// <seealso cref="System.Collections.IEnumerable" />
    public class ProcessList : IEnumerable
    {
        internal static ProcessList Create()
        {
            return new ProcessList();
        }

        private ProcessList()
        {
        }

        private List<Guid> _serivceIds = new List<Guid>();

        /// <summary>
        /// Adds the specified service.
        /// </summary>
        /// <param name="service">The service.</param>
        public void Add(IBackgroundProcess service)
        {
            _serivceIds.Add(Process.Add(service));
        }

        /// <summary>
        /// Adds the specified service.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <returns></returns>
        public async Task AddAsync(IBackgroundProcess service)
        {
            await Task.Run(() => Add(service));
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            var tasks = new List<Task>();

            foreach (var id in _serivceIds)
            {
                tasks.Add(Process.StopAsync(id));
            }

            foreach (var task in tasks)
            {
                task.Wait();
            }
        }

        /// <summary>
        /// 傳回會逐一查看集合的列舉程式。
        /// </summary>
        /// <returns>
        ///   <see cref="T:System.Collections.IEnumerator" /> 物件，用於逐一查看集合。
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            return _serivceIds.GetEnumerator();
        }
    }
}