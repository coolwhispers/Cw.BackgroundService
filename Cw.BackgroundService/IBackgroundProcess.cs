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

        /// <summary>
        /// Background Stop.
        /// </summary>
        void BackgroundStop();
    }
}