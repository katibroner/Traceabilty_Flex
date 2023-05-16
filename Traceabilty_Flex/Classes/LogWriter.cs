using System;
using System.IO;
using System.Net;

namespace Traceabilty_Flex
{
    internal class LogWriter
    {
        private string _mExePath = string.Empty;
        public LogWriter(string logMessage, string key)
        {
            LogWrite(logMessage, key);
        }

        private void LogWrite(string logMessage, string key)
        {
            _mExePath = "c:\\tmp\\Traceability";

            string logfile;
            switch (key)
            {
                case "error":
                    logfile = "PartsListErrors.txt";
                    break;
                case "RecipeEvents":
                    logfile = "RecipeEvents.txt";
                    break;
                default:
                    logfile = "DebugLog.txt";
                    break;
            }
            try
            {
                using (StreamWriter w = File.AppendText(_mExePath + "\\" + logfile))
                {
                    Log(logMessage, w);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void Log(string logMessage, TextWriter txtWriter)
        {
            try
            {
                txtWriter.Write("\r\nLog Entry : ");
                txtWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                txtWriter.WriteLine("  :");
                txtWriter.WriteLine("  :{0}", logMessage);
                txtWriter.WriteLine("-------------------------------");
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static void WriteLog(string logLine)
        {
            string path = "c:\\tmp\\Traceability\\Traceability_ErrorLog.txt";

            using (StreamWriter sw = new StreamWriter(path, true))
            {
                sw.WriteLine("{0:dd/MM/yyyy HH:mm} > {1}", DateTime.Now, logLine);
            }
        }

        public static void WriteLogTest(string logLine,string path)
        {
            //string path = "c:\\tmp\\Traceability\\Traceability_Test.txt";

            using (StreamWriter sw = new StreamWriter(path, true))
            {
                sw.WriteLine("{0:dd/MM/yyyy HH:mm} > {1}", DateTime.Now, logLine);
            }
        }

        public static void SendMail(string mailTo, string mailCC, string mailSubject, string mailBody)
        {
            mailTo = "sariel.goldvarg@flex.com";
            mailCC = "yuri.migal@flex.com, igor.sydorenko@flex.com, miroslav.sidler@flex.com";
            try
            {
                string MailSvc = "http://mignt100/Service/SendMail.ashx?";
                WebClient client = new WebClient();
                client.DownloadString(string.Format("{0}TO={1}&CC={2}&Subject={3}&Message={4}"
                    , MailSvc
                    , mailTo
                    , mailCC
                    , Uri.EscapeUriString(mailSubject)
                    , Uri.EscapeUriString(mailBody)));
                client.Dispose();
            }
            catch (Exception exp)
            {
                WriteLog(string.Format("Error sending mail : {0}\n{1}\n", exp.Message, mailBody));
            }
        }
    }
}
