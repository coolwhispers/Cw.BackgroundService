using System;

namespace Cw.BackgroundService
{
    internal enum ExceptionCode
    {
        
    }

    /// <summary>
    /// 背景服務例外錯誤
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class BackgroundProcessException : Exception
    {
        internal static BackgroundProcessException Create(ExceptionCode errorCode)
        {
            var errorMessage = errorCode.ToString();

            return new BackgroundProcessException(errorMessage);
        }

        internal BackgroundProcessException(string message) : this(message, "https://github.com/coolwhispers/Cw.BackgroundService")
        {
        }

        internal BackgroundProcessException(string message, string helpLink) : base(message)
        {
            HelpLink = helpLink;
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