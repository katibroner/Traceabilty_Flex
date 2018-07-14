using System;
using System.IO;
using System.Reflection;

namespace Traceabilty_Flex
{
    class LogWriter
    {
        private string m_exePath = string.Empty;
        public LogWriter(string logMessage, string key)
        {
            LogWrite(logMessage,key);
        }
        public void LogWrite(string logMessage,string key)
        {
            // m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            m_exePath = "c:\\tmp";

            string logfile="";
            if (key == "error")
                logfile = "PartsListErrors.txt";
            else if (key == "QMS")
                logfile = "logQMS.txt";
            else logfile = "DebugLog.txt";

            try
            {
                using (StreamWriter w = File.AppendText(m_exePath + "\\" + logfile))
                {

                    Log(logMessage, w);
                }
            }
            catch (Exception)
            {

            }
        }

        public void Log(string logMessage, TextWriter txtWriter)
        {
            try
            {
                txtWriter.Write("\r\nLog Entry : ");
                txtWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                    DateTime.Now.ToLongDateString());
                txtWriter.WriteLine("  :");
                txtWriter.WriteLine("  :{0}", logMessage);
                txtWriter.WriteLine("-------------------------------");
            }
            catch (Exception)
            {
            }
        }
    }
}
