using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Traceabilty_Flex.Classes
{
    public class SQLdb
    {
        private SqlConnection conn;
        private readonly string connectionString = "";
        public SQLdb(string ServerName, string DataBaseName, string UserName, string Secret)
        {
            connectionString =
           "Data Source=" + ServerName + ";" +
           "Initial Catalog=" + DataBaseName + ";" +
           "User id=" + UserName + ";" +
           "Password=" + Secret + ";";
        }
        private bool OpenConnection()
        {
            try
            {
                conn = new SqlConnection(connectionString);
                conn.Open();
                return true;
            }
            catch (SqlException ex)
            {
                switch (ex.Number)
                {
                    case 0:
                        //MessageBox.Show("Cannot connect to server.  Contact administrator");
                        break;
                    case 53:
                        //MessageBox.Show("Cannot connect to server. check your internet connection");
                        break;

                    case 1045:
                        //MessageBox.Show("Invalid username/password, please try again");
                        break;
                    default:
                        //MessageBox.Show("connection problem");
                        break;

                }
                return false;
            }
        }
        private bool CloseConnection()
        {
            try
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();
                return true;
            }
            catch (SqlException ex)
            {
                //MessageBox.Show(ex.Message);
                return false;
            }
        }
        public void Insert(string query)
        {
            if (this.OpenConnection() == true)
            {
                try
                {
                    SqlCommand cmd = new SqlCommand(query, conn);

                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    this.CloseConnection();
                    //MessageBox.Show(ex.Message);
                }
                this.CloseConnection();
            }
        }
        public void Update(string query)
        {
            if (this.OpenConnection() == true)
            {
                try
                {
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    this.CloseConnection();
                    //MessageBox.Show(ex.Message);
                }
                this.CloseConnection();
            }
        }
        public void Delete(string query)
        {
            if (this.OpenConnection() == true)
            {
                try
                {
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    this.CloseConnection();
                    //MessageBox.Show(ex.Message);
                }
                this.CloseConnection();
            }
        }
    }
}
