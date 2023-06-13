using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Traceabilty_Flex.Classes
{
    class SiplacePro
    {
        private static string GetConnectionString()
        {
            string conString = @"Data Source=172.20.20.2;Initial Catalog=SiplacePro;Persist Security Info=True;User ID=aoi; Password=$Flex2016";
            return conString;
        }
        public static SqlConnection con = new SqlConnection();
        public static SqlCommand cmd = new SqlCommand("", con);//INSERT, UPDATE, DELETE, SELECT
        public static SqlDataReader rd;// SqlDataReader
        //public static DataSet ds;
        //public static SqlDataAdapter da;


        //SELECT, INSERT, UPDATE, DELETE
        public static string sql;
        //Open Database
        public static void openConnection()
        {
            if (con.State == ConnectionState.Closed)
            {
                con.ConnectionString = GetConnectionString();
                con.Open();
                //MessageBox.Show("The connection is "+ con.State.ToString());
            }
        }
        //Close DataBase
        public static void closeConnection()
        {
            if (con.State == ConnectionState.Open)
            {
                con.Close();
                //MessageBox.Show("The connection is " + con.State.ToString());
            }
        }
    }
}
