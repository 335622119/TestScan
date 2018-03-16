using ScanApp.Entity;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ScanApp.Tools
{
    public class CrawlMaster
    {


        /// <summary>
        /// The data received event.
        /// </summary>
        public event DataReceivedEventHandler DataReceivedEvent;
        /// <summary>
        /// The add url event.
        /// </summary>
        public event AddUrlEventHandler AddUrlEvent;
        /// <summary>
        /// The threads.
        /// </summary>
        private Thread[] threads;
        /// <summary>
        /// The thread status.
        /// </summary>
        private bool[] threadStatus;

        public int ThreadCount { get; set; }

        public void Crawl()
        {
            threads = new Thread[ThreadCount];
            threadStatus = new bool[ThreadCount];
            for (int i = 0; i < ThreadCount; i++)
            {
                var threadStart = new ParameterizedThreadStart(this.CrawlProcess);

                this.threads[i] = new Thread(threadStart);
            }

            for (int i = 0; i < this.threads.Length; i++)
            {
                this.threads[i].Start(i);
                this.threadStatus[i] = false;
            }
        }
        /// <summary>
        /// 添加url
        /// </summary>
        /// <param name="url"></param>
        public void AddUrl(string url)
        {
            UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1, Type = true });

            Match mc = Regex.Match(url, @"((([A-Za-z]{3,9}:(?:\/\/)?)(?:[-;:&=\+\$,\w]+@)?[A-Za-z0-9.-]+(:[0-9]+)?|(?:ww‌​w.|[-;:&=\+\$,\w]+@)[A-Za-z0-9.-]+)((?:\/[\+~%\/.\w-_]*)?\??(?:[-\+=&;%@.\w_]*)#?‌​(?:[\w]*))?)", RegexOptions.IgnoreCase);

            for (int i = 0; i < Program.UrlSuffix.Count; i++)
            {
                string url1 = mc.ToString();
                if (url1.Substring(url1.Length - 1, 1) == "/")
                {
                    url1 = url1.Substring(0, url1.Length - 1);
                }
                url1 = url1 + Program.UrlSuffix[i];
                UrlQueue.Instance.EnQueue(new UrlInfo(url1) { Depth = 1, Type = false });
            }
        }

        #region Methods
        /// <summary>
        /// The crawl process.
        /// </summary>
        /// <param name="threadIndex">
        /// The thread index.
        /// </param>
        private void CrawlProcess(object threadIndex)
        {
            var currentThreadIndex = (int)threadIndex;
            while (true)
            {

                // 根据队列中的 Url 数量和空闲线程的数量，判断线程是睡眠还是退出
                if (UrlQueue.Instance.Count == 0)
                {
                    this.threadStatus[currentThreadIndex] = true;
                    if (!this.threadStatus.Any(t => t == false))
                    {
                        break;
                    }

                    Thread.Sleep(2000);
                    continue;
                }

                this.threadStatus[currentThreadIndex] = false;

                if (UrlQueue.Instance.Count == 0)
                {
                    continue;
                }


                UrlInfo urlInfo = UrlQueue.Instance.DeQueue();

                if (urlInfo == null)
                {
                    continue;
                }

                HttpHandle.HttpResult httpResult = HttpHandle.Get(urlInfo);
                if (httpResult != null)
                {
                    string html = httpResult.Body;
                    string code = httpResult.Code.ToString();

                    if (urlInfo.Type && httpResult.Code == 200)
                    {
                        this.ParseLinks(urlInfo, html);


                        Match mc = Regex.Match(urlInfo.UrlString, @"((([A-Za-z]{3,9}:(?:\/\/)?)(?:[-;:&=\+\$,\w]+@)?[A-Za-z0-9.-]+(:[0-9]+)?|(?:ww‌​w.|[-;:&=\+\$,\w]+@)[A-Za-z0-9.-]+)((?:\/[\+~%\/.\w-_]*)?\??(?:[-\+=&;%@.\w_]*)#?‌​(?:[\w]*))?)", RegexOptions.IgnoreCase);

                        for (int i = 0; i < Program.UrlSuffix.Count; i++)
                        {
                            string url1 = mc.ToString();
                            if (url1.Substring(url1.Length - 1, 1) == "/")
                            {
                                url1 = url1.Substring(0, url1.Length - 1);
                            }
                            url1 = url1 + Program.UrlSuffix[i];
                            //url1 = url1.Replace("//", "/");
                            var addUrlEventArgs1 = new AddUrlEventArgs { Title = url1, Depth = urlInfo.Depth + 1, Url = url1 };
                            if (this.AddUrlEvent != null && !this.AddUrlEvent(addUrlEventArgs1))
                            {
                                continue;
                            }
                            UrlQueue.Instance.EnQueue(new UrlInfo(url1) { Depth = 1, Type = false });
                        };
                    }
                    else
                    {
                        String regex = @"<title>.+</title>";

                        //返回网页标题  
                        String title = Regex.Match(html, regex).ToString();
                        html = Regex.Replace(title, @"[\""]+", "");
                        if (html.IndexOf("404") > -1 || html.IndexOf("Page Not Found") > -1 || html.IndexOf("未找到") > -1 || html.IndexOf("不存在") > -1 || html.IndexOf("错误") > -1 || html.IndexOf("网站防火墙") > -1 || html.IndexOf("请联系空间提供商") > -1)
                        {
                            code = "404";
                        }
                    }
                    if (this.DataReceivedEvent != null)
                    {
                        this.DataReceivedEvent(
                            new DataReceivedEventArgs
                            {
                                Url = urlInfo.UrlString,
                                Depth = urlInfo.Depth,
                                Html = html,
                                Code = code,
                                Type = urlInfo.Type
                            });
                    }
                }
            }
        }

        /// <summary>
        /// The parse content.
        /// </summary>
        /// <param name="stream">
        /// The stream.
        /// </param>
        /// <param name="characterSet">
        /// The character set.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private string ParseContent(Stream stream, string characterSet)
        {
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            byte[] buffer = memoryStream.ToArray();

            Encoding encode = Encoding.ASCII;
            string html = encode.GetString(buffer);

            string localCharacterSet = characterSet;

            Match match = Regex.Match(html, "<meta([^<]*)charset=([^<]*)\"", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                localCharacterSet = match.Groups[2].Value;

                var stringBuilder = new StringBuilder();
                foreach (char item in localCharacterSet)
                {
                    if (item == ' ')
                    {
                        break;
                    }

                    if (item != '\"')
                    {
                        stringBuilder.Append(item);
                    }
                }

                localCharacterSet = stringBuilder.ToString();
            }

            if (string.IsNullOrEmpty(localCharacterSet))
            {
                localCharacterSet = characterSet;
            }

            if (!string.IsNullOrEmpty(localCharacterSet))
            {
                encode = Encoding.GetEncoding(localCharacterSet);
            }

            memoryStream.Close();

            return encode.GetString(buffer);
        }
        /// <summary>
        /// The parse links.
        /// </summary>
        /// <param name="urlInfo">
        /// The url info.
        /// </param>
        /// <param name="html">
        /// The html.
        /// </param>
        private void ParseLinks(UrlInfo urlInfo, string html)
        {
            var urlDictionary = new Dictionary<string, string>();

            Match match = Regex.Match(html, @"((([A-Za-z]{3,9}:(?:\/\/)?)(?:[-;:&=\+\$,\w]+@)?[A-Za-z0-9.-]+(:[0-9]+)?|(?:ww‌​w.|[-;:&=\+\$,\w]+@)[A-Za-z0-9.-]+)((?:\/[\+~%\/.\w-_]*)?\??(?:[-\+=&;%@.\w_]*)#?‌​(?:[\w]*))?)");
            while (match.Success)
            {

                ///;
                // 以 href 作为 key
                string urlKey = match.Value;

                // 以 text 作为 value
                string urlValue = Regex.Replace(match.Value, "(?i)<.*?>", string.Empty);
                string Url = @"^http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?$";
                if (Regex.IsMatch(urlValue, Url))
                {
                    urlDictionary[urlKey] = urlValue;
                }
                match = match.NextMatch();
            }

            foreach (var item in urlDictionary)
            {
                string href = item.Key;
                string text = item.Value;

                if (!string.IsNullOrEmpty(href))
                {
                    string url = href.Replace("%3f", "?")
                            .Replace("%3d", "=")
                            .Replace("%2f", "/")
                            .Replace("&amp;", "&");
                    if (string.IsNullOrEmpty(url) || url.StartsWith("#")
                        || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                        || url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var baseUri = new Uri(urlInfo.UrlString);
                    Uri currentUri = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                         ? new Uri(url)
                                         : new Uri(baseUri, url);

                    url = currentUri.AbsoluteUri;

                    var addUrlEventArgs = new AddUrlEventArgs { Title = text, Depth = urlInfo.Depth + 1, Url = url };
                    if (this.AddUrlEvent != null && !this.AddUrlEvent(addUrlEventArgs))
                    {
                        continue;
                    }
                    UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = urlInfo.Depth + 1, Type = true });
                }
            }
        }

        #endregion
    }
}
