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
        //设定日志路径
        private static string logFolder = "log";
        private static string defaultLogFile = Path.Combine(logFolder, DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss.fff") + ".log");
        //用锁定防止并行占进程
        private static readonly object writerLock = new object();
        public static void logWrite(string message)
        {
            lock (writerLock)
            {
                File.AppendAllText(defaultLogFile,$"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message} {Environment.NewLine}");
            }
        }
    }
}
