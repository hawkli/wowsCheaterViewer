using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wowsCheaterViewer
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
            //如果日志路径不存在，则新建
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);
            //删除2天前日志
            foreach(string logFile in Directory.GetFiles(logFolder))
                if (Convert.ToDateTime(Path.GetFileName(logFile).Split(' ').First()) < DateTime.Now.AddDays(-2).Date)
                    File.Delete(logFile);
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
