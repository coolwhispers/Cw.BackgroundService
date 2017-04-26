using System;

namespace Cw.BackgroundService
{
    public class BackgroundProcessException : Exception
    {
        private int _errorCode;

        internal BackgroundProcessException(int errorCode)
        {
            _errorCode = errorCode;
        }

        /// <summary>
        /// 取得或設定與這個例外狀況相關聯說明檔的連結。
        /// </summary>
        /// </exception>
        public override string HelpLink { get; set; }

        /// <summary>
        /// 取得描述目前例外狀況的訊息。
        /// </summary>
        public override string Message { get; }
    }
}