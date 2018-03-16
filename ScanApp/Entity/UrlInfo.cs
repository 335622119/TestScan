namespace ScanApp.Entity
{
    /// <summary>
    /// The url info.
    /// </summary>
    public class UrlInfo
    {
        #region Fields

        /// <summary>
        /// The url.
        /// </summary>
        private readonly string url;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlInfo"/> class.
        /// </summary>
        /// <param name="urlString">
        /// The url string.
        /// </param>
        public UrlInfo(string urlString)
        {
            this.url = urlString;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the depth.
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// Gets the url string.
        /// </summary>
        public string UrlString
        {
            get
            {
                return this.url;
            }
        }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public int StatusCode { get; set; }
        /// <summary>
        /// 获取的类型（1、GET 2、HEAD）
        /// </summary>
        public bool Type { get; set; }

        #endregion
    }
}
