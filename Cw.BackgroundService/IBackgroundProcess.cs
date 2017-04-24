using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cw.BackgroundService
{
    /// <summary>
    /// Background Process Interface
    /// </summary>
    public interface IBackgroundProcess
    {
        /// <summary>
        /// Background Start
        /// </summary>
        void BackgroundStart();
    }
}
