using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace Cw.BackgroundService
{
    /// <summary>
    /// Background Process
    /// </summary>
    public class Process
    {
        /// <summary>
        /// Create a new ProcessList
        /// </summary>
        /// <returns>ProcessList</returns>
        public static ProcessList NewList()
        {
            return ProcessList.Create();
        }

        private static ConcurrentDictionary<Guid, ProcessItem> _services = new ConcurrentDictionary<Guid, ProcessItem>();

        /// <summary>
        /// Adds the specified service.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <returns></returns>
        public static Guid Add(IBackgroundProcess service)
        {
            Guid id;
            var newService = new ProcessItem(service);
            do
            {
                id = Guid.NewGuid();
            }
            while (!_services.TryAdd(id, newService));

            newService.Start();

            return id;
        }

        /// <summary>
        /// Adds the specified service.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <returns></returns>
        public async static Task<Guid> AddAsync(IBackgroundProcess service)
        {
            return await Task.Run(() => Add(service));
        }

        /// <summary>
        /// Stops the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public static void Stop(Guid id)
        {
            if (_services.ContainsKey(id))
            {
                ProcessItem service;

                _services.TryRemove(id, out service);

                service.Stop();
            }
        }

        /// <summary>
        /// Stops the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public async static Task StopAsync(Guid id)
        {
            await Task.Run(() => Stop(id));
        }

        /// <summary>
        /// Stops all.
        /// </summary>
        public static void StopAll()
        {
            var list = new List<Task>();
            foreach (var id in _services.Keys)
            {
                list.Add(_services[id].StopAsync());
            }

            foreach (var task in list)
            {
                task.Wait();
            }
        }

        /// <summary>
        /// Stops all.
        /// </summary>
        /// <returns></returns>
        public async static Task StopAllAsync()
        {
            await Task.Run(() => StopAll());
        }

        /// <summary>
        /// Determines whether the specified identifier is stopped.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>
        ///   <c>true</c> if the specified identifier is stopped; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsStopped(Guid id)
        {
            if (!_services.ContainsKey(id))
            {
                return true;
            }

            return _services[id].IsServiceStopped;
        }

        /// <summary>
        /// Windows Service Console
        /// </summary>
        /// <param name="servicesToRun">The services to run.</param>
        public static void RunInteractive(ServiceBase[] servicesToRun)
        {
            var onStartMethod = typeof(ServiceBase).GetMethod("OnStart", BindingFlags.Instance | BindingFlags.NonPublic);
            var onStopMethod = typeof(ServiceBase).GetMethod("OnStop", BindingFlags.Instance | BindingFlags.NonPublic);
            
            foreach (ServiceBase service in servicesToRun)
            {
                Console.Write("Starting {0}...", service.ServiceName);
                onStartMethod.Invoke(service, new object[] { new string[] { } });
                Console.WriteLine("Started");
            }

            Console.WriteLine("Press any key to stop the services");
            Console.ReadKey();
            
            foreach (ServiceBase service in servicesToRun)
            {
                Console.Write("Stopping {0}...", service.ServiceName);
                onStopMethod.Invoke(service, null);
                Console.WriteLine("Stopped");
            }

        }
    }
}