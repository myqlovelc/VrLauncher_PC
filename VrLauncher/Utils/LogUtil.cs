using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace VrService
{
    public  class LogUtil
    {
        private FileStream _fs;
        private static StreamWriter _sw;

        public LogUtil(string fileName)
        {
            _fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            _sw = new StreamWriter(_fs);
        }

        public static void  WriteLog(string log)
        {
            _sw.WriteLine(string.Format("{0}: {1}",DateTime.Now.ToString(),log));
            _sw.Flush();
        }

        public void Clear()
        {
            _sw.Flush();
            _sw.Close();
            _fs.Close();
        }
    }
}
