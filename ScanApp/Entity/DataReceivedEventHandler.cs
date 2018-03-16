namespace ScanApp.Entity
{
    using System;

    /// <summary>
    /// The data received event handler.
    /// </summary>
    /// <param name="args">
    /// The args.
    /// </param>
    public delegate void DataReceivedEventHandler(DataReceivedEventArgs args);

    /// <summary>
    /// The data received event args.
    /// </summary>
    public class DataReceivedEventArgs : EventArgs
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the depth.
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// Gets or sets the html.
        /// </summary>
        public string Html { get; set; }

        /// <summary>
        /// Gets or sets the url.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the url.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 获取的类型（1、GET 2、HEAD）
        /// </summary>
        public bool Type { get; set; }

        #endregion
    }
}
