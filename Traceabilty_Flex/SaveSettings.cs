using System;
using System.Data;
using System.Windows;
using TraceabilityTestGui;

namespace Traceabilty_Flex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SQLClass sql = new SQLClass("trace");

            if((bool)checkboxLines.IsChecked)
            {
                DataTable dt = new DataTable();
                dt = ((DataView)dataGridLineConfig.ItemsSource).ToTable();

                foreach (DataRow item in dt.Rows)
                {
                    string query =
                        string.Format(@"IF NOT EXISTS(SELECT 1 from [Traceability].[dbo].[Lines] where Line = '{0}') Insert INTO [Traceability].[dbo].[Lines] 
                                    (Line, IP, Active, Monitor, DBPath) 
                                    VALUES('{0}', '{1}', '{2}', '{3}', '{4}') 
                                    else UPDATE [Traceability].[dbo].[Lines] SET IP = '{1}', Active = '{2}', Monitor = '{3}', DBPath = '{4}', skid = '{5}' where line = '{0}'",
                                    item["Line"], item["IP"], item["Active"], item["Monitor"], item["DBPath"], item["skid"]);
                    try
                    {
                        sql.Update(query);
                    }
                    catch(Exception ex)
                    {
                        ErrorOut("At LinesSettingsSaving: " + ex.Message);
                    }
                }

                GetActiveLines();
                FillLineCollection();
                FillRecipeDT();
                FillFirstLast();
            }
            else if((bool)checkboxUsers.IsChecked)
            {
                DataTable dt = new DataTable();
                dt = ((DataView)dataGridUsers.ItemsSource).ToTable();

                string q = @"SELECT * FROM [Traceability].[dbo].[Users]";
                DataTable du = sql.SelectDB(q, out string res);
                if(dt.Rows.Count != du.Rows.Count)
                {
                    q = "truncate table [Traceability].[dbo].[Users]";
                    sql.Update(q);
                }

                foreach (DataRow item in dt.Rows)
                {
                    string query =
                        string.Format(@"IF NOT EXISTS(SELECT 1 from [Traceability].[dbo].[Users] where ID = '{0}') Insert INTO [Traceability].[dbo].[Users] 
                                    (ID, FirstName, LastName, Role, eMail, Admin) 
                                    VALUES('{0}', '{1}', '{2}', '{3}', '{4}', '{5}') 
                                    else UPDATE [Traceability].[dbo].[Users] SET FirstName = '{1}', LastName = '{2}', Role = '{3}', eMail = '{4}', Admin = '{5}' where id = '{0}'",
                                    item["ID"], item["FirstName"].ToString().Replace("'", "''"), item["LastName"].ToString().Replace("'","''"), item["Role"], item["eMail"], item["Admin"]);
                    try
                    {
                        sql.Update(query);
                    }
                    catch(Exception ex)
                    {
                        ErrorOut("At UsersSettingsSaving: " + ex.Message);
                    }
                }
            }
            else if((bool)checkboxParts.IsChecked)
            {
                DataTable dt = new DataTable();
                dt = ((DataView)dataGridParts.ItemsSource).ToTable();

                string q = @"SELECT * FROM [Traceability].[dbo].[PartsException]";
                DataTable du = sql.SelectDB(q, out string res);
                if (dt.Rows.Count != du.Rows.Count)
                {
                    q = "truncate table [Traceability].[dbo].[PartsException]";
                    sql.Update(q);
                }

                foreach (DataRow item in dt.Rows)
                {
                    string query =
                        string.Format(@"IF NOT EXISTS(SELECT 1 from [Traceability].[dbo].[PartsException] where Part = '{0}') Insert INTO [Traceability].[dbo].[PartsException] 
                                    (Part) VALUES('{0}')", item["Part"]);
                    try
                    {
                        sql.Update(query);
                    }
                    catch(Exception ex)
                    {
                        ErrorOut("At PartsSettingsSaving" + ex.Message);
                    }
                }

                GetExcludedPartsList();
            }
            else if((bool)checkboxCustomers.IsChecked)
            {
                DataTable dt = new DataTable();
                dt = ((DataView)dataGridCustomers.ItemsSource).ToTable();

                string q = @"SELECT * FROM [Traceability].[dbo].[CustomersNeedLTS]";
                DataTable du = sql.SelectDB(q, out string res);
                if (dt.Rows.Count != du.Rows.Count)
                {
                    q = "truncate table [Traceability].[dbo].[CustomersNeedLTS]";
                    sql.Update(q);
                }

                foreach (DataRow item in dt.Rows)
                {
                    string query =
                        string.Format(@"IF NOT EXISTS(SELECT 1 from [Traceability].[dbo].[CustomersNeedLTS] where Customer = '{0}') Insert INTO [Traceability].[dbo].[CustomersNeedLTS] 
                                    (Customer) VALUES('{0}')", item["Customer"]);
                    try
                    {
                        sql.Update(query);
                    }
                    catch(Exception ex)
                    {
                        ErrorOut("At CustomerSettingsSaving" + ex.Message);
                    }
                }

                GetCustomerList();
            }

            //string qry = string.Format(@"UPDATE [Traceability].[dbo].[LineUpgrade] SET [date] =  CONVERT(VARCHAR, '{0}')", LineDatePicker.SelectedDate.Value.ToString("yyyy/MM/dd"));
            //try
            //{
            //    sql.Update(qry);
            //}
            //catch (Exception ex)
            //{
            //    ErrorOut("At CustomerSettingsSaving" + ex.Message);
            //}
        }
    }
}