using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using Traceabilty_Flex;

namespace TraceabilityTestGui
{
    public class SQLClass
    {
        private SqlConnection conn;

        private string ServerName = "tcp:172.20.20.2";
        private string DataBaseName = "SiplacePro";
        private string UserName = "sa"; // ez
        private string Secret = @"$Sq2010"; // $Flex2016
        public string ConnectionString { get; set; }
        public string provider = "System.Data.SqlClient";
        public string Server { set { ServerName = value; } }

        public SQLClass()
        {
            ConnectionString =
           "Data Source=" + ServerName + ";" +
           "Initial Catalog=" + DataBaseName + ";" +
           "User id=" + UserName + ";" +
           "Password=" + Secret + ";";
        }

        public SQLClass(string name)
        {
            if (name == "setup")
            {
                ServerName = "tcp:172.20.20.4";
                UserName = "sa";
                Secret = "$Sq2010";
                name = DataBaseName = "SiplaceSetupCenter";
            }
            else if (name == "setup_trace")
            {
                ServerName = "tcp:172.20.20.4";
                UserName = "sa";
                Secret = "$Sq2010";
                name = DataBaseName = "TraceDB";
            }
            else if(name == "trace")
            {
                ServerName = "mignt105";
                UserName = "aoi";
                Secret = "$Flex2016";
                name = DataBaseName = "Traceability";
            }
            else if(name == "LtsMonitor")
            {
                //  ServerName = @"mignt100\sqlexpress";
                ServerName = @"mignt056\sqlexpress";
                UserName = "sa";
                Secret = "$Flex2009#$";
                //   name = DataBaseName = "LtsMon";
                name = DataBaseName = "TrcErrors";
            }
            else if(name == "login")
            {
                ServerName = "mignt105";
                UserName = "aoi";
                Secret = "$Flex2016";
                name = DataBaseName = "AOI";
            }
            else if (name == "line")
            {
                ServerName = "";
                UserName = "AraModUser";
                Secret = "#Arasql2013";
                name = DataBaseName = "SiplaceSIS";
            }

            ConnectionString =
           "Data Source=" + ServerName + ";" +
           "Initial Catalog=" + name + ";" +
           "User id=" + UserName + ";" +
           "Password=" + Secret + ";";
        }

        #region OpenConnection
        //open connection to database
        private bool OpenConnection()
        {
            try
            {
                conn = new SqlConnection(ConnectionString);
                conn.Open();
                return true;
            }
            catch (SqlException ex)
            {
                //When handling errors, you can your application's response based 
                //on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                switch (ex.Number)
                {
                    case 0:
                        MessageBox.Show("Cannot connect to server.  Contact administrator");
                        break;

                    case 1045:
                        MessageBox.Show("Invalid username/password, please try again");
                        break;
                    default:
                        MainWindow._mWindow.ErrorOut(ex.Message);
                        break;
                }
                return false;
            }
        }
        #endregion

        #region GetCount
        internal int GetCount(string query)
        {
            int count = 0;
            if (this.OpenConnection() == true)
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                try
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            count = reader.GetInt32(0);
                        }
                        reader.Close();
                    }
                }
                catch (SqlException ex)
                {
                    this.CloseConnection();
                    MainWindow._mWindow.ErrorOut("At Count (SQLClass): " + ex.Message);
                }
                this.CloseConnection();
            }
            return count;
        }
        #endregion

        #region CloseConnection
        //Close connection
        private bool CloseConnection()
        {
            try
            {
                if(conn.State == ConnectionState.Open)
                    conn.Close();
                return true;
            }
            catch (SqlException ex)
            {
                MainWindow._mWindow.ErrorOut("At Close SQL Connection" + ex.Message);
                return false;
            }
        }
        #endregion

        #region Update
        //Update statement
        public void Update(string query)
        {
            try {
                //Open connection
                if (OpenConnection() == true)
                {
                    //create mysql command
                    SqlCommand cmd = new SqlCommand()
                    {
                        //MySqlDataAdapter da = new MySqlDataAdapter();
                        //Assign the query using CommandText
                        CommandText = query,
                        //Assign the connection using Connection
                        Connection = conn
                    };
                    //da.SelectCommand = cmd;
                    //Execute query
                    cmd.ExecuteNonQuery();

                    //close connection
                    this.CloseConnection();
                }
            }
                catch(SqlException ex)
            {
                MainWindow._mWindow.ErrorOut("At Update (SQLClass): " + ex.Message);
            }
            finally
            {
                CloseConnection();
            }
            
        }
        #endregion

        #region Insert
        //Insert statement
        public void Insert(string query)
        {
            //open connection
            if (this.OpenConnection() == true)
            {
                try
                {
                    //create command and assign the query and connection from the constructor
                    SqlCommand cmd = new SqlCommand(query, conn);

                    //Execute command
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MainWindow._mWindow.ErrorOut("At Insert (SQLClass): " + ex.Message);
                }
                //close connection
                finally
                {
                    this.CloseConnection();
                }
            }
        }
        #endregion

        #region Delete
        //Delete statement
        public void Delete(string query)
        {
            //string query = "DELETE FROM tableinfo WHERE name='John Smith'";

            if (this.OpenConnection() == true)
            {
                try
                {
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.ExecuteNonQuery();
                    this.CloseConnection();
                }
                catch(Exception ex)
                {
                    MainWindow._mWindow.ErrorOut("At Update (SQLClass): " + ex.Message);
                }
                finally
                {
                    CloseConnection();
                }
            }
        }
        #endregion

        #region SelectDB
        internal DataTable SelectDB(string sql, out string res)
        {
            res = null;
            DataTable CmdResult = new DataTable();

            System.Data.Common.DbProviderFactory factory = System.Data.Common.DbProviderFactories.GetFactory(provider);
            System.Data.Common.DbConnection con = factory.CreateConnection();
            con.ConnectionString = ConnectionString;
            System.Data.Common.DbCommand cmd = factory.CreateCommand();
            cmd.CommandText = sql;
            cmd.Connection = con;
            System.Data.Common.DbDataReader reader;

            try
            {
                con.Open();
                reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                CmdResult.Load(reader);
                con.Close();
            }
            catch (Exception exp)
            {
                res = "SelectDB Error on " + "SiplacePro" + " : " + exp.Message + "<br />";
            }
            finally
            {
                if (con.State == ConnectionState.Open) con.Close();
            }
            return CmdResult;
        }
        #endregion

        #region UpdateDB
        public int UpdateDB(string csName, string sql, out string Result)
        {
            ///
            /// Update DB
            /// 
            Result = null;
            int CmdResult = 0;

            System.Data.Common.DbProviderFactory factory = System.Data.Common.DbProviderFactories.GetFactory(provider);
            System.Data.Common.DbConnection con = factory.CreateConnection();
            con.ConnectionString = ConnectionString;
            System.Data.Common.DbCommand cmd = factory.CreateCommand();
            cmd.CommandText = sql;
            cmd.Connection = con;
            try
            {
                con.Open();
                CmdResult = cmd.ExecuteNonQuery();
                con.Close();
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
        }
        #endregion

        #region GetJoinedList
        public string GetJoinedList(string csName, string Sql, char Seperator, out string Result)
        {
            Result = null;
            string CmdResult = "";

            System.Data.Common.DbProviderFactory factory = System.Data.Common.DbProviderFactories.GetFactory(provider);
            System.Data.Common.DbConnection con = factory.CreateConnection();
            con.ConnectionString = ConnectionString;
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
                con.Close();
            }
            catch (Exception exp)
            {
                Result = "UpdateDB Error updating " + csName + " : " + exp.Message + "<br />";
            }
            finally
            {
                if (con.State == ConnectionState.Open) con.Close();
            }
            return CmdResult;
        }
        #endregion
    }
}
