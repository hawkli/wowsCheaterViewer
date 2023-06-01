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

        private static readonly Lazy<Logger> _instance = new Lazy<Logger>(() => new Logger());
        public static Logger Instance => _instance.Value;
        private Logger()
        {
            defaultLogFile = "log.log";
        }
        private static readonly object writerLock = new object();
        private string defaultLogFile;

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
