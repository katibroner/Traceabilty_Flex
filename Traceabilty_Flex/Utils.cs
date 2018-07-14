using System;
using System.IO;
using System.Net;
using System.Configuration;
using TraceabilityTestGui;
using System.Data;

namespace Traceabilty_Flex
{
    class Utils
    {
        #region Logs Utils
        public static void WriteLog(string logLine)
        {
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location + ".log";// Process.GetCurrentProcess().MainModule.FileName;

            using (StreamWriter sw = new StreamWriter(path, true))     // Append, Create if not exist
            {
                sw.WriteLine("{0:dd/MM/yyyy HH:mm} > {1}", DateTime.Now, logLine);
            }
        }

        public static void SendMail(string mailTo, string mailCC, string mailSubject, string mailBody)
        {
            mailTo = BuildMailList(mailTo);
            mailCC = BuildMailList(mailCC);
            try
            {
                string MailSvc = ConfigurationManager.AppSettings["MailSvc"];
                if (!MailSvc.EndsWith("?")) MailSvc += "?";
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
                WriteLog(string.Format("Error sending mail : {0}\n{1}\n", exp.Message, mailBody.Replace("<br/>", "<br/>\n")));
            }
        }

        public static void SendMail(string mailSubject, string mailBody)
        {
            string[] str = BuildMailList();
            string mailTo = str[0];
            string mailCC = str[1];

            try
            {
                string MailSvc = ConfigurationManager.AppSettings["MailSvc"];

                if (!MailSvc.EndsWith("?")) MailSvc += "?";
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
                WriteLog(string.Format("Error sending mail : {0}\n{1}\n", exp.Message, mailBody.Replace("<br/>", "<br/>\n")));
            }
        }

        private static string[] BuildMailList()
        {
            string[] str = new string[2];

            string MailTo = ConfigurationManager.AppSettings["MailTo"];
            string MailCC = ConfigurationManager.AppSettings["MailCC"];

            string[] _mailList = MailTo.Split(';'); string updatedList = "";
            for (int i = 0; i < _mailList.GetLength(0); i++)
            {
                if (_mailList[i].Trim().Length > 0 && _mailList[i].Trim() != "0")
                    updatedList += ((_mailList[i].Contains("@")) ? _mailList[i] : _mailList[i] + "@flex.com") + ";";
            }

            string[] _mailList2 = MailCC.Split(';'); string updatedList2 = "";
            for (int i = 0; i < _mailList2.GetLength(0); i++)
            {
                if (_mailList2[i].Trim().Length > 0 && _mailList2[i].Trim() != "0")
                    updatedList2 += ((_mailList2[i].Contains("@")) ? _mailList2[i] : _mailList2[i] + "@flex.com") + ";";
            }

            str[0] = updatedList;
            str[1] = updatedList2;

            return str;
        }

        private static string BuildMailList(string MailList)
        {
            if (string.IsNullOrEmpty(MailList)) return "";

            string[] _mailList = MailList.Split(';'); string updatedList = "";
            for (int i = 0; i < _mailList.GetLength(0); i++)
            {
                if (_mailList[i].Trim().Length > 0 && _mailList[i].Trim() != "0")
                    updatedList += ((_mailList[i].Contains("@")) ? _mailList[i] : _mailList[i] + "@flextronics.com") + ";";
            }
            return updatedList;
        }
        #endregion
        private static void LogSetupErr(string Line, string Station, string Board, string Barcode, string Part, string Message)
        {
         //   string result = "";
            SQLClass sq = new SQLClass("LtsMonitor");

            string sql = "INSERT INTO [LTSMon].[dbo].[LtsErrorLog] "
                       + "  ([Line],[Station],[Board],[Barcode],[Part],[Message]) "
                       + "VALUES "
                       + "  (" + ((Line == null) ? "null" : "'" + Line.Replace("'", "''") + "'")
                       + "  ," + ((Station == null) ? "null" : "'" + Station.Replace("'", "''") + "'")
                       + "  ," + ((Board == null) ? "null" : "'" + Board.Replace("'", "''") + "'")
                       + "  ," + ((Barcode == null) ? "null" : "'" + Barcode.Replace("'", "''") + "'")
                       + "  ," + ((Part == null) ? "''" : "'" + Part.Replace("'", "''") + "'")
                       + "  ," + ((Message == null) ? "null" : "'" + Message.Replace("'", "''") + "'")
                       + "  )";
            //throw new NotImplementedException();
            sq.UpdateDB("LtsMonitor", sql, out string result);
            if (result != null)
            {
                Utils.WriteLog("LogSetupErr:" + result);
                Utils.SendMail(sq.GetJoinedList("LtsMonitor", "select eMail from [Users] where [Admin] > 0", ';', out  result)     //GetSetting("MailCC")
                    , ""
                    , "Traceability Monitor Error"
                    , "LogSetupErr:" + result);
            }
        }
        private static void LogSetupErr(string Line, string Station, string Board, string Barcode, string Part, DateTime dtCreated, string Message)
        {
            //string result = "";
            SQLClass sq = new SQLClass("LtsMonitor");

            string sql = "INSERT INTO [LTSMon].[dbo].[LtsErrorLog] "
                       + "  ([Line],[Station],[Board],[Barcode],[Part],[dtCreated],[Message]) "
                       + "VALUES "
                       + "  (" + ((Line == null) ? "null" : "'" + Line.Replace("'", "''") + "'")
                       + "  ," + ((Station == null) ? "null" : "'" + Station.Replace("'", "''") + "'")
                       + "  ," + ((Board == null) ? "null" : "'" + Board.Replace("'", "''") + "'")
                       + "  ," + ((Barcode == null) ? "null" : "'" + Barcode.Replace("'", "''") + "'")
                       + "  ," + ((Part == null) ? "''" : "'" + Part.Replace("'", "''") + "'")
                       + "  ,'" + dtCreated.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'"
                       + "  ," + ((Message == null) ? "null" : "'" + Message.Replace("'", "''") + "'")
                       + "  )";
            //throw new NotImplementedException();
            sq.UpdateDB("LtsMonitor", sql, out string result);
            if (result != null)
            {
                Utils.WriteLog("LogSetupErr:" + result);
                Utils.SendMail(sq.GetJoinedList("LtsMonitor", "select eMail from [Users] where [Admin] > 0", ';', out result)     //GetSetting("MailCC")
                    , ""
                    , "Traceability Monitor Error"
                    , "LogSetupErr:" + result);
            }
        }
        #region dbUtils
        //public static int UpdateDB(string csName, string sql, out string Result)
        //{
        //    ///
        //    /// Update DB
        //    /// 
        //    Result = null;
        //    int CmdResult = 0;
        //    string connectionString = ConfigurationManager.ConnectionStrings[csName].ConnectionString;
        //    string provider = ConfigurationManager.ConnectionStrings[csName].ProviderName;

        //    System.Data.Common.DbProviderFactory factory = System.Data.Common.DbProviderFactories.GetFactory(provider);
        //    System.Data.Common.DbConnection con = factory.CreateConnection();
        //    con.ConnectionString = connectionString;
        //    System.Data.Common.DbCommand cmd = factory.CreateCommand();
        //    cmd.CommandText = sql;
        //    cmd.Connection = con;
        //    try
        //    {
        //        con.Open();
        //        CmdResult = cmd.ExecuteNonQuery();
        //    }
        //    catch (Exception exp)
        //    {
        //        Result = "UpdateDB Error updating " + csName + " : " + exp.Message + "<br />";
        //        //WriteLog("Utils.UpdateDB", "", csName, sql + "; Error: " + exp.Message, Result);
        //        //Result.ForeColor = System.Drawing.Color.Red;
        //    }
        //    finally
        //    {
        //        if (con.State == ConnectionState.Open) con.Close();
        //    }
        //    return CmdResult;
        //}

        //public static DataTable SelectBAAN(string csName, string sql, out string Result)
        //{
        //    ///
        //    /// Select from BaaN
        //    /// 
        //    DataTable CmdResult = new DataTable();
        //    string BaanConnectionString = ConfigurationManager.ConnectionStrings[csName].ConnectionString;
        //    IfxDataReader reader = null;
        //    IfxConnection BaanConnection = new IfxConnection(BaanConnectionString);
        //    IfxCommand BaanCommand = new IfxCommand();
        //    BaanCommand.Connection = BaanConnection;
        //    BaanCommand.CommandText = sql;

        //    try
        //    {
        //        BaanConnection.Open();
        //        reader = BaanCommand.ExecuteReader(CommandBehavior.CloseConnection);
        //        CmdResult.Load(reader);
        //        reader.Close();
        //    }
        //    catch (IfxException exp)
        //    {
        //        Result.Text += "SelectDB Error on " + csName + " : " + exp.Message + "<br />";
        //        Utils.SaveLog("Utils.SelectDB", "", csName, sql + "; Error: " + exp.Message, Result);
        //        Result.ForeColor = System.Drawing.Color.Red;
        //    }
        //    finally
        //    {
        //        if (reader != null && !reader.IsClosed) reader.Close();
        //        //if (BaanConnection.State == ConnectionState.Open) BaanConnection.Close();
        //        //BaanConnection.Dispose();
        //    }
        //    return CmdResult;
        //}
        //public static DataTable SelectDB(string csName, string sql, out string Result)
        //{
        //    ///
        //    /// Update DB
        //    /// 
        //    Result = null;
        //    DataTable CmdResult = new DataTable();
        //    string connectionString = ConfigurationManager.ConnectionStrings[csName].ConnectionString;
        //    string provider = ConfigurationManager.ConnectionStrings[csName].ProviderName;

        //    System.Data.Common.DbProviderFactory factory = System.Data.Common.DbProviderFactories.GetFactory(provider);
        //    System.Data.Common.DbConnection con = factory.CreateConnection();
        //    con.ConnectionString = connectionString;
        //    System.Data.Common.DbCommand cmd = factory.CreateCommand();
        //    cmd.CommandText = sql;
        //    cmd.Connection = con;
        //    System.Data.Common.DbDataReader reader;

        //    try
        //    {
        //        con.Open();
        //        //CmdResult = cmd.ExecuteNonQuery();
        //        reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
        //        CmdResult.Load(reader);
        //        //if (CmdResult.Rows.Count == 0)
        //        //    CmdResult.Rows.Add(CmdResult.NewRow());
        //    }
        //    catch (Exception exp)
        //    {
        //        Result = "SelectDB Error on " + csName + " : " + exp.Message + "<br />";
        //        //Utils.SaveLog("Utils.SelectDB", "", csName, sql + "; Error: " + exp.Message, Result);
        //        //Result.ForeColor = System.Drawing.Color.Red;
        //    }
        //    finally
        //    {
        //        if (con.State == ConnectionState.Open) con.Close();
        //    }
        //    return CmdResult;
        //}
        //public static DataTable SelectDB(string csName, string csSource, string sql, out string Result)
        //{
        //    ///
        //    /// Update DB
        //    /// 
        //    Result = null;
        //    DataTable CmdResult = new DataTable();
        //    string connectionString = string.Format(ConfigurationManager.ConnectionStrings[csName].ConnectionString, csSource);
        //    string provider = ConfigurationManager.ConnectionStrings[csName].ProviderName;

        //    System.Data.Common.DbProviderFactory factory = System.Data.Common.DbProviderFactories.GetFactory(provider);
        //    System.Data.Common.DbConnection con = factory.CreateConnection();
        //    con.ConnectionString = connectionString;
        //    System.Data.Common.DbCommand cmd = factory.CreateCommand();
        //    cmd.CommandText = sql;
        //    cmd.Connection = con;
        //    System.Data.Common.DbDataReader reader;

        //    try
        //    {
        //        con.Open();
        //        //CmdResult = cmd.ExecuteNonQuery();
        //        reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
        //        CmdResult.Load(reader);
        //        //if (CmdResult.Rows.Count == 0)
        //        //    CmdResult.Rows.Add(CmdResult.NewRow());
        //    }
        //    catch (Exception exp)
        //    {
        //        Result = "SelectDB Error on " + csName + " : " + exp.Message + "<br />";
        //        //Utils.SaveLog("Utils.SelectDB", "", csName, sql + "; Error: " + exp.Message, Result);
        //        //Result.ForeColor = System.Drawing.Color.Red;
        //    }
        //    finally
        //    {
        //        if (con.State == ConnectionState.Open) con.Close();
        //    }
        //    return CmdResult;
        //}
        //public static object SelectDBscalar(string csName, string sql, out string Result)
        //{
        //    ///
        //    /// Select Scalar
        //    /// 
        //    Result = null;
        //    object CmdResult = new object();
        //    string connectionString = ConfigurationManager.ConnectionStrings[csName].ConnectionString;
        //    string provider = ConfigurationManager.ConnectionStrings[csName].ProviderName;

        //    System.Data.Common.DbProviderFactory factory = System.Data.Common.DbProviderFactories.GetFactory(provider);
        //    System.Data.Common.DbConnection con = factory.CreateConnection();
        //    con.ConnectionString = connectionString;
        //    System.Data.Common.DbCommand cmd = factory.CreateCommand();
        //    cmd.CommandText = sql;
        //    cmd.Connection = con;

        //    try
        //    {
        //        con.Open();
        //        //CmdResult = cmd.ExecuteNonQuery();
        //        CmdResult = cmd.ExecuteScalar();
        //    }
        //    catch (Exception exp)
        //    {
        //        Result = "SelectDB Error on " + csName + " : " + exp.Message + "<br />";
        //        //Utils.SaveLog("Utils.SelectDB", "", csName, sql + "; Error: " + exp.Message, Result);
        //        //Result.ForeColor = System.Drawing.Color.Red;
        //    }
        //    finally
        //    {
        //        if (con.State == ConnectionState.Open) con.Close();
        //    }
        //    return CmdResult;
        //}
        public static string GetJoinedList(string csName, string Sql, char Seperator, out string Result)
        {
            Result = null;
            string CmdResult = "";
            //string connectionString = ConfigurationManager.ConnectionStrings[csName].ConnectionString;
            //string provider = ConfigurationManager.ConnectionStrings[csName].ProviderName;
            SQLClass sql = new SQLClass("trace");
            string connectionString = sql.ConnectionString;
            string provider = sql.provider;

            System.Data.Common.DbProviderFactory factory = System.Data.Common.DbProviderFactories.GetFactory(provider);
            System.Data.Common.DbConnection con = factory.CreateConnection();
            con.ConnectionString = connectionString;
            System.Data.Common.DbCommand cmd = factory.CreateCommand();
            cmd.CommandText = Sql;
            cmd.Connection = con;
            try
            {
                con.Open();
                using (System.Data.Common.DbDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        // Get the values of the fields in the current row
                        // For example, if the first column is a string...
                        CmdResult += r.GetString(0) + Seperator;
                    }
                    r.Close();
                    CmdResult = CmdResult.Trim(Seperator);
                }
            }
            catch (Exception exp)
            {
                Result = "UpdateDB Error updating " + csName + " : " + exp.Message + "<br />";
                //WriteLog("Utils.UpdateDB", "", csName, sql + "; Error: " + exp.Message, Result);
                //Result.ForeColor = System.Drawing.Color.Red;
            }
            finally
            {
                if (con.State == ConnectionState.Open) con.Close();
            }
            return CmdResult;
            //DataTable dt = new DataTable();
            //SqlDataAdapter da = new SqlDataAdapter(Sql, objCon);
            //da.Fill(dt);
            //da.Dispose();

            //string[] arr = new string[dt.Rows.Count];
            //for (int i = 0; i < dt.Rows.Count; i++)
            //    arr[i] = dt.Rows[i][0].ToString();
            //return string.Join(Seperator, arr); ;
        }
        #endregion
    }
}
