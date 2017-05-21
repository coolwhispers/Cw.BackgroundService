using System.Collections.Generic;

namespace Cw.BackgroundService.Console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var li = new List<ISchedule>();
            li.Add(new Schedule<TestBackgroundProcess>(5));

            System.Console.WriteLine("Start.");
            foreach (var schedule in li)
            {
                schedule.Start();
            }

            System.Console.ReadKey();

            foreach (var schedule in li)
            {
                System.Console.WriteLine("Wait Stop.");
                schedule.Stop();
            }

            System.Console.WriteLine("End.");

            System.Console.ReadKey();
        }

        private class TestBackgroundProcess : IBackgroundProcess
        {
            public void BackgroundStart()
            {
                System.Console.WriteLine("Tested");
            }
        }
    }
}