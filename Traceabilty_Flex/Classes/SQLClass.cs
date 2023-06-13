using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace Traceabilty_Flex
{
    public class SqlClass
    {
        private SqlConnection _conn;

        private string _serverName = "tcp:172.20.20.2";
        private readonly string _dataBaseName = "SiplacePro";
        private readonly string _userName = "sa";
        private readonly string _secret = @"$Sq2010";
        public string ConnectionString { get; set; }
        public string Provider = "System.Data.SqlClient";

        public string Server { set => _serverName = value; }

        public SqlClass()
        {
            ConnectionString =
           "Data Source=" + _serverName + ";" +
           "Initial Catalog=" + _dataBaseName + ";" +
           "User id=" + _userName + ";" +
           "Password=" + _secret + ";";
        }

        public SqlClass(string ServerName, string DataBaseName, string UserName, string Secret)
        {
            ConnectionString =
           "Data Source=" + ServerName + ";" +
           "Initial Catalog=" + DataBaseName + ";" +
           "User id=" + UserName + ";" +
           "Password=" + Secret + ";";
        }

        public SqlClass(string name)
        {
            if (name == "setup")
            {
                _serverName = "tcp:172.20.20.4";
                _userName = "sa";
                _secret = "$Sq2010";
                name = _dataBaseName = "SiplaceSetupCenter";
            }
            else if (name == "setup_trace")
            {
                _serverName = "tcp:172.20.20.4";
                _userName = "aoi";
                _secret = "$Flex2016";
                name = _dataBaseName = "TraceDB";
            }
            else if (name == "trace")
            {
                _serverName = "MIGSQLCLU4\\SMT";
                _userName = "aoi";
                _secret = "$Flex2016";
                name = _dataBaseName = "Traceability";
            }
            else if (name == "LMS_DATA")
            {
                _serverName = "MIGSQLCLU4\\SMT";
                _userName = "aoi";
                _secret = "$Flex2016";
                name = _dataBaseName = "LMS_DATA";
            }
            else if (name == "LtsMonitor")
            {
                _serverName = @"mignt056\sqlexpress";
                _userName = "sa";
                _secret = "$Flex2009#$";
                name = _dataBaseName = "TrcErrors";
            }
            else if (name == "login")
            {
                _serverName = "MIGSQLCLU4\\SMT";
                _userName = "aoi";
                _secret = "$Flex2016";
                name = _dataBaseName = "AOI";
            }
            else if (name == "line")
            {
                _serverName = "";
                _userName = "AraModUser";
                _secret = "#Arasql2013";
                name = _dataBaseName = "SiplaceSIS";
            }

            ConnectionString =
           "Data Source=" + _serverName + ";" +
           "Initial Catalog=" + name + ";" +
           "User id=" + _userName + ";" +
           "Password=" + _secret + ";";
        }

        //open connection to database
        private bool OpenConnection()
        {
            try
            {
                _conn = new SqlConnection(ConnectionString);
                _conn.Open();
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

        internal int GetCount(string query)
        {
            var count = 0;
            if (!OpenConnection()) return count;

            var cmd = new SqlCommand(query, _conn);
            try
            {
                using (var reader = cmd.ExecuteReader())
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
                CloseConnection();
                MainWindow._mWindow.ErrorOut("At Count (SQLClass): " + ex.Message);
            }
            CloseConnection();
            return count;
        }

        //Close connection
        private bool CloseConnection()
        {
            try
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();
                return true;
            }
            catch (SqlException ex)
            {
                MainWindow._mWindow.ErrorOut("At Close SQL Connection" + ex.Message);
                return false;
            }
        }

        //Update statement
        public void Update(string query)
        {
            try
            {
                //Open connection
                if (OpenConnection())
                {
                    var cmd = new SqlCommand()
                    {

                        CommandText = query,
                        Connection = _conn
                    };
                    cmd.ExecuteNonQuery();


                    CloseConnection();
                }
            }
            catch (SqlException ex)
            {
                MainWindow._mWindow.ErrorOut("At Update (SQLClass): " + ex.Message);
            }
            finally
            {
                CloseConnection();
            }

        }

        //Insert statement
        public void Insert(string query)
        {
            //open connection
            if (OpenConnection())
            {
                try
                {
                    //create command and assign the query and connection from the constructor
                    var cmd = new SqlCommand(query, _conn);

                    //Execute command
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MainWindow._mWindow.ErrorOut("At Insert (SQLClass): " + ex.Message + query);
                }
                //close connection
                finally
                {
                    CloseConnection();
                }
            }
        }

        //Delete statement
        public void Delete(string query)
        {
            if (OpenConnection())
            {
                try
                {
                    var cmd = new SqlCommand(query, _conn);
                    cmd.ExecuteNonQuery();
                    CloseConnection();
                }
                catch (Exception ex)
                {
                    MainWindow._mWindow.ErrorOut("At Update (SQLClass): " + ex.Message);
                }
                finally
                {
                    CloseConnection();
                }
            }
        }

        //Select
        internal DataTable SelectDb(string sql, out string res)
        {
            res = null;
            var cmdResult = new DataTable();

            var factory = System.Data.Common.DbProviderFactories.GetFactory(Provider);
            var con = factory.CreateConnection();
            if (con != null)
            {
                con.ConnectionString = ConnectionString;
                var cmd = factory.CreateCommand();

                if (cmd != null)
                {
                    cmd.CommandText = sql;
                    cmd.Connection = con;

                    try
                    {
                        con.Open();
                        var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                        cmdResult.Load(reader);
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
                }
            }

            return cmdResult;
        }

        //Update
        public int UpdateDb(string csName, string sql, out string result)
        {
            result = null;
            var cmdResult = 0;

            var factory = System.Data.Common.DbProviderFactories.GetFactory(Provider);
            var con = factory.CreateConnection();
            if (con != null)
            {
                con.ConnectionString = ConnectionString;
                var cmd = factory.CreateCommand();
                if (cmd != null)
                {
                    cmd.CommandText = sql;
                    cmd.Connection = con;
                    try
                    {
                        con.Open();
                        cmdResult = cmd.ExecuteNonQuery();
                        con.Close();
                    }
                    catch (Exception exp)
                    {
                        result = "UpdateDB Error updating " + csName + " : " + exp.Message + "<br />";
                    }
                    finally
                    {
                        if (con.State == ConnectionState.Open) con.Close();
                    }
                }
            }

            return cmdResult;
        }

        public string GetJoinedList(string csName, string sql, char separator, out string result)
        {
            result = null;

            var cmdResult = "";
            var factory = System.Data.Common.DbProviderFactories.GetFactory(Provider);
            var con = factory.CreateConnection();

            if (con == null) return cmdResult;

            con.ConnectionString = ConnectionString;
            var cmd = factory.CreateCommand();

            if (cmd == null) return cmdResult;

            cmd.CommandText = sql;
            cmd.Connection = con;

            try
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        cmdResult += r.GetString(0) + separator;
                    }

                    r.Close();
                    cmdResult = cmdResult.Trim(separator);
                }

                con.Close();
            }
            catch (Exception exp)
            {
                result = "UpdateDB Error updating " + csName + " : " + exp.Message + "<br />";
            }
            finally
            {
                if (con.State == ConnectionState.Open) con.Close();
            }

            return cmdResult;
        }
    }
}
