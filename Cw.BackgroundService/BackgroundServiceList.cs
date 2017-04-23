using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cw.BackgroundService
{
    public class BackgroundServiceList
    {
        internal static BackgroundServiceList Create()
        {
            return new BackgroundServiceList();
        }

        private BackgroundServiceList()
        {

        }

        private List<Guid> _serivceIds = new List<Guid>();

        public void Add(IBackgroundService service)
        {
            BackgroundService.Add(service);
        }

        public async Task AddAsync(IBackgroundService service)
        {
            await Task.Run(() => Add(service));
        }

        public void Stop()
        {
            var tasks = new List<Task>();

            foreach (var id in _serivceIds)
            {
                tasks.Add(BackgroundService.StopAsync(id));
            }

            foreach (var task in tasks)
            {
                task.Wait();
            }
        }

    }
}
