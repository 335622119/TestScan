using ScanApp.Entity;
using ScanApp.Tools;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ScanApp
{
    class Program
    {
        /// <summary>
        /// 爬虫排除列表
        /// </summary>
        public static List<string> UrlDebar = new List<string>();
        /// <summary>
        /// 爬虫后缀列表
        /// </summary>
        public static List<string> UrlSuffix = new List<string>();
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
        private static BloomFilter<string> filter;

        /// <summary>
        /// 获取当前路径
        /// </summary>
        private static string path = System.IO.Directory.GetCurrentDirectory();

        /// <summary>
        /// 爬取操作台
        /// </summary>
        public static CrawlMaster master = new CrawlMaster();
        [STAThread]
        static void Main(string[] args)
        {
            //UrlInfo urlinfo = new UrlInfo("http://www.0ddt.com/web.rar");

            //HttpHandle.HttpResult httpResult = HttpHandle.Get(urlinfo);
            //StopProcess("ScanApp");//清理进程
            //StopProcess("ScanApp.vshost");//清理进程



            //创建Bloom 算法
            filter = new BloomFilter<string>(200000);
            //添加爬虫排除列表
            UrlDebar = ReadTxtContent(path + "\\Dictionary");
            //添加爬虫字典后缀
            UrlSuffix = ReadTxtContent(path + "\\Dictionary\\UrlSuffix");
            //获取起始主Url
            string MianUrl = ConfigurationManager.ConnectionStrings["MianUrl"].ToString();
            //获取配置文件的线程数
            int ThreadCount = Convert.ToInt32(ConfigurationManager.ConnectionStrings["ThreadCount"].ToString());
            //获取添加地址
            master.AddUrl(MianUrl);
            //设置线程数
            master.ThreadCount = ThreadCount;
            master.AddUrlEvent += MasterAddUrlEvent;
            master.DataReceivedEvent += MasterDataReceivedEvent;
            master.Crawl();
            
            Console.ReadKey(true);
        }  
        /// <summary>
        /// 监测Url是否重复
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static bool MasterAddUrlEvent(AddUrlEventArgs args)
        {
            foreach (string item in UrlDebar)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    if (args.Url.Contains(item))
                    {
                        return false; // 返回 false 代表：不添加到队列中
                    }
                }
            }
            if (!filter.Contains(args.Url))
            {
                filter.Add(args.Url);
                return true;
            }
            return false;
        }
        /// <summary>
        /// 检测完毕回调函数
        /// </summary>
        /// <param name="args"></param>
        private static void MasterDataReceivedEvent(ScanApp.Entity.DataReceivedEventArgs args)
        {
            string HtmlText = args.Html;

            batchPool(args.Url, args.Code, args.Type);
        }


        /// <summary>
        /// 界面展示
        /// </summary>
        /// <param name="Msg"></param>
        private static void batchPool(string url, string code, bool type)
        {
            int total = UrlQueue.Instance.Count;
            //int workerThreads = 0;
            //int maxWordThreads = 0;
            ////int   
            //int compleThreads = 0;
            //ThreadPool.GetAvailableThreads(out workerThreads, out compleThreads);
            //ThreadPool.GetMaxThreads(out maxWordThreads, out compleThreads);


            //Console.WriteLine("+------------------------------------------------------------------------+");
            //Console.WriteLine("剩余线程：" + Convert.ToString(maxWordThreads - workerThreads));
            Console.WriteLine();
            Console.WriteLine("正在检测：" + url);
            Console.WriteLine("剩余数量：" + Convert.ToString(total) + "  ----  检测结果：" + code);

            //if (workerThreads == maxWordThreads)
            //{
            //    Console.WriteLine("剩余线程：0");
            //    Console.WriteLine("剩余数量：0");
            //}
            //Console.WriteLine("+------------------------------------------------------------------------+");

            if (code == "200" && !type)
            {
                WriteLog(url + "----" + code);
            }
            //Console.WriteLine("Test");
            //ClearCurrentConsoleLine();
        }
        /// <summary>
        /// 将结果添加到数据库
        /// </summary>
        /// <param name="url"></param>
        public static void AddMysqlUrl(string url)
        {
            //DataTable dt = MySqlHelper.GetDataTable(string.Format("select * from resultUrl where url='{0}'", url));
            //if (dt.Rows.Count == 0)
            //{
            //    int read = MySqlHelper.ExecuteSql(string.Format("INSERT INTO resultUrl (url) VALUES ('{0}')", url));
            //}
        }

        /// <summary>
        /// 读取txt文件内容
        /// </summary>
        /// <param name="Path">文件地址</param>
        public static List<string> ReadTxtContent(string Path)
        {
            List<string> TextList = new List<string>();
                            //创建所有子目录
                if (!System.IO.Directory.Exists(Path))
                {
                    System.IO.Directory.CreateDirectory(Path);
                }

            DirectoryInfo folder = new DirectoryInfo(Path);

            foreach (FileInfo file in folder.GetFiles("*.txt"))
            {
                //创建文本文件
                if (!System.IO.File.Exists(file.FullName))
                {
                    System.IO.File.Create(file.FullName).Close();
                }
                else{
                    StreamReader sr = new StreamReader(file.FullName, Encoding.Default);
                    string content;
                    while ((content = sr.ReadLine()) != null)
                    {
                        TextList.Add(content.ToString());
                    }
                }
            }

            return TextList;
        }

        /// <summary>
        /// 文本写入线程锁
        /// 读写锁，当资源处于写入模式时，其他线程写入需要等待本次写入结束之后才能继续写入
        /// </summary>
        static ReaderWriterLockSlim LogWriteLock = new ReaderWriterLockSlim();

        public static void WriteLog(string strLog)
        {
            try
            {
                LogWriteLock.EnterWriteLock();

                string sFilePath = path;
                string sFileName = "results.txt";
                sFileName = sFilePath + "\\" + sFileName; //文件的绝对路径
                if (!Directory.Exists(sFilePath))//验证路径是否存在
                {
                    Directory.CreateDirectory(sFilePath);
                    //不存在则创建
                }
                FileStream fs;
                StreamWriter sw;
                if (File.Exists(sFileName))
                //验证文件是否存在，有则追加，无则创建
                {
                    fs = new FileStream(sFileName, FileMode.Append, FileAccess.Write);
                }
                else
                {
                    fs = new FileStream(sFileName, FileMode.Create, FileAccess.Write);
                }
                sw = new StreamWriter(fs);
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "   ---   " + strLog);
                sw.Close();
                fs.Close();
            }
            catch (Exception)
            {

            }
            finally
            {
                //退出写入模式，释放资源占用
                //注意：一次请求对应一次释放
                //      若释放次数大于请求次数将会触发异常[写入锁定未经保持即被释放]
                //      若请求处理完成后未释放将会触发异常[此模式不下允许以递归方式获取写入锁定]
                LogWriteLock.ExitWriteLock();
            }
            
        }
    }
}
