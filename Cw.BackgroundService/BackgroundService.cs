using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cw.BackgroundService
{
    public class BackgroundService
    {
        public static BackgroundServiceList CreateList()
        {
            return BackgroundServiceList.Create();
        }

        private static ConcurrentDictionary<Guid, BackgroundService> _services = new ConcurrentDictionary<Guid, BackgroundService>();

        public static Guid Add(IBackgroundService service)
        {
            Guid id;
            var newService = new BackgroundService(service);
            do
            {
                id = Guid.NewGuid();
            }
            while (_services.TryAdd(id, newService));

            return id;
        }

        public async static Task<Guid> AddAsync(IBackgroundService service)
        {
            return await Task.Run(() => Add(service));
        }

        public static void Stop(Guid id)
        {
            if (_services.ContainsKey(id))
            {
                _services.TryRemove(id, out BackgroundService serivce);

                serivce.StopService();
            }
        }

        public async static Task StopAsync(Guid id)
        {
            await Task.Run(() => Stop(id));
        }

        public async static void StopAll()
        {
            var list = new List<Task>();
            foreach (var id in _services.Keys)
            {
                list.Add(_services[id].StopServiceAsync());
            }

            foreach (var task in list)
            {
                await task;
            }
        }

        public static bool IsStopped(Guid id)
        {
            if (!_services.ContainsKey(id))
            {
                return true;
            }

            var service = _services[id];

            return service.ServiceStopped;
        }

        private IBackgroundService _serivce;
        private Thread _thread;
        private BackgroundService(IBackgroundService serivce)
        {
            _serivce = serivce;
            _thread = new Thread(serivce.BackgroundStart);
            _thread.Start();
            ServiceStopped = false;
        }

        public bool ServiceStopped { get; private set; }

        public void StopService()
        {
            var checkStop = true;
            do
            {
                switch (_thread.ThreadState)
                {
                    case ThreadState.Stopped:
                        checkStop = false;
                        break;
                }

            } while (checkStop);

            ServiceStopped = true;
        }

        public async Task StopServiceAsync()
        {
            await Task.Run(() => StopService());
        }
    }
}
