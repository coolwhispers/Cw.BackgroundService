using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cw.BackgroundService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cw.BackgroundService.Tests
{
    [TestClass()]
    public class ScheduleTests
    {
        [TestMethod()]
        public void StartTest()
        {
            var schedule = new Schedule<TestBackgroundProcess>(5);

            schedule.Start();

            schedule.Stop();



            Assert.Fail();
        }

        private class TestBackgroundProcess : IBackgroundProcess
        {
            public bool IsRun = false;
            public void BackgroundStart()
            {
                IsRun = true;
            }
        }
    }
}