using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 郭楠查看器
{
    class Logger
    {
        //用lazy实现单实例
        private static readonly Lazy<Logger> _instance = new Lazy<Logger>(() => new Logger());
        public static Logger Instance => _instance.Value;

        //首次加载实例时，设定日志路径
        private string defaultLogFile;
        private Logger()
        {
            string logFolder = "log";
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);
            defaultLogFile = Path.Combine(logFolder, DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss.fff") + ".log");
        }

        //用锁定防止并行占进程
        private static readonly object writerLock = new object();
        public void logWrite(string message)
        {
            lock (writerLock)
            {
                using StreamWriter sw = new StreamWriter(defaultLogFile, true);
                sw.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
            }
        }
    }
}
