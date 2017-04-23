using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cw.BackgroundService
{
    public interface IBackgroundService
    {
        /// <summary>
        /// Background Start
        /// </summary>
        void BackgroundStart();
    }
}
