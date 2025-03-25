using System;
using System.Data;
using System.Data.Odbc;
using System.Windows;
using TraceabilityTestGui;

namespace Traceabilty_Flex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string[] ProgrammExceptionList;

        private void GetSettings()
        {
            var d1 = GetUsers();
            DataGridUsers.ItemsSource = d1.AsDataView();

            var d2 = GetLines();
            DataGridLineConfig.ItemsSource = d2.AsDataView();

            var d3 = GetParts();
            DataGridParts.ItemsSource = d3.AsDataView();

            var d4 = GetCustomers();
            DataGridCustomers.ItemsSource = d4.AsDataView();

            var d5 = GetRecipeList();
            DataGridRecipes.ItemsSource = d5.AsDataView();

            var d6 = GetProgrammList();
            DataGridProgramm.ItemsSource = d6.AsDataView();
        }

        private DataTable GetCustomers()
        {
            var sql = new SqlClass("trace");
            var query = @"SELECT [Customer] FROM CustomersNeedLTS";
            var d = sql.SelectDb(query, out var result);
            return d;
        }

        private DataTable GetParts()
        {
            var sql = new SqlClass("trace");
            var query = @"SELECT [Part] FROM PartsException";
            var d = sql.SelectDb(query, out var result);
            return d;
        }

        private DataTable GetLines()
        {
            var sql = new SqlClass("trace");
            var query = @"SELECT Line, IP, Active, Adam, dtLastCheck FROM Lines order by Line asc";
            var d = sql.SelectDb(query, out var result);
            return d;
        }

        private DataTable GetUsers()
        {
            var sql = new SqlClass("trace");//LtsMonitor
            var query = @"SELECT * FROM Users";
            var d = sql.SelectDb(query, out var result);
            return d;
        }

        private DataTable GetRecipeList()
        {
            var sql = new SqlClass("trace");
            string query = @"SELECT [recipe] FROM RecipeException";

            var dt = sql.SelectDb(query, out string result);

            var reclist = new string[dt.Rows.Count];
            for (var i = 0; i < dt.Rows.Count; i++)
            {
                reclist[i] = dt.Rows[i][0].ToString().Trim();
            }
            RecipeExceptionList = reclist;

            return dt;
        }
        private DataTable GetProgrammList()
        {
            var sql = new SqlClass("trace");
            string query = @"SELECT TOP (1000) [Programm] FROM [Traceability].[dbo].[ProgrammsException1]";
            DataTable dt = new DataTable();
            dt = sql.SelectDb(query, out string result);
            var proglist = new string[dt.Rows.Count];
            for (var i = 0; i < dt.Rows.Count; i++)
            {
                proglist[i] = dt.Rows[i][0].ToString().Trim();
            }
            ProgrammExceptionList = proglist;

            return dt;
        }
        private void CheckboxLines_Checked(object sender, RoutedEventArgs e)
        {
            DataGridLineConfig.IsReadOnly = false;
        }

        private void CheckboxLines_Unchecked(object sender, RoutedEventArgs e)
        {
            DataGridLineConfig.IsReadOnly = true;

        }

        private void CheckboxUsers_Unchecked(object sender, RoutedEventArgs e)
        {
            DataGridUsers.IsReadOnly = true;
        }

        private void CheckboxUsers_Checked(object sender, RoutedEventArgs e)
        {
            DataGridUsers.IsReadOnly = false;

        }

        private void CheckboxParts_Checked(object sender, RoutedEventArgs e)
        {
            DataGridParts.IsReadOnly = false;
        }

        private void CheckboxParts_Unchecked(object sender, RoutedEventArgs e)
        {
            DataGridParts.IsReadOnly = true;
        }

        private void CheckboxCustomers_Checked(object sender, RoutedEventArgs e)
        {
            DataGridCustomers.IsReadOnly = false;
        }

        private void CheckboxCustomers_Unchecked(object sender, RoutedEventArgs e)
        {
            DataGridCustomers.IsReadOnly = true;
        }
        private void CheckboxRecipes_Checked(object sender, RoutedEventArgs e)
        {
            DataGridRecipes.IsReadOnly = false;
        }

        private void CheckboxRecipes_Unchecked(object sender, RoutedEventArgs e)
        {
            DataGridRecipes.IsReadOnly = true;
        }
        private void CheckboxProgramms_Checked(object sender, RoutedEventArgs e)
        {
            DataGridProgramm.IsReadOnly = false;
        }
        private void CheckboxProgramms_Unchecked(object sender, RoutedEventArgs e)
        {
            DataGridProgramm.IsReadOnly = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var sql = new SqlClass("trace");

            if (CheckboxLines.IsChecked != null && (bool)CheckboxLines.IsChecked)
            {
                var dt = ((DataView)DataGridLineConfig.ItemsSource).ToTable();

                foreach (DataRow item in dt.Rows)
                {
                    var query =$@"IF NOT EXISTS(SELECT 1 from Lines where Line = '{item["Line"]}') Insert INTO Lines 
                                    (Line, IP, Active, Adam) 
                                    VALUES('{item["Line"]}', '{item["IP"]}', '{item["Active"]}', '{item["Adam"]}') 
                                    else UPDATE Lines SET IP = '{item["IP"]}', Active = '{item["Active"]}', Adam = '{item["Adam"]}' where line = '{item["Line"]}'";
                    try
                    {
                        sql.Update(query);
                    }
                    catch (Exception ex)
                    {
                        ErrorOut("At LinesSettingsSaving: " + ex.Message);
                    }
                }
                GetActiveLines();
                FillLineCollection();
                FillRecipeDt();
                FillFirstLast();
            }
            if (CheckboxUsers.IsChecked != null && (bool)CheckboxUsers.IsChecked)
            {
                var dt = ((DataView)DataGridUsers.ItemsSource).ToTable();
                var q = @"SELECT * FROM Users";
                var du = sql.SelectDb(q, out var res);

                if (dt.Rows.Count != du.Rows.Count)
                {
                    q = "truncate table Users";
                    sql.Update(q);
                }

                foreach (DataRow item in dt.Rows)
                {
                    var query =
                        $@"IF NOT EXISTS(SELECT 1 from Users where ID = '{item["ID"]}') Insert INTO Users 
                                    (ID, FirstName, LastName, Role, eMail, Admin) 
                                    VALUES('{item["ID"]}', '{item["FirstName"].ToString().Replace("'", "''")}', '{item["LastName"].ToString().Replace("'", "''")}', '{item["Role"]}', '{item["eMail"]}', '{item["Admin"]}') 
                                    else UPDATE Users SET FirstName = '{item["FirstName"].ToString().Replace("'", "''")}', LastName = '{item["LastName"].ToString().Replace("'", "''")}', Role = '{item["Role"]}', eMail = '{item["eMail"]}', Admin = '{item["Admin"]}' where id = '{item["ID"]}'";
                    try
                    {
                        sql.Update(query);
                    }
                    catch (Exception ex)
                    {
                        ErrorOut("At UsersSettingsSaving: " + ex.Message);
                    }
                }
            }

            if (CheckboxParts.IsChecked != null && (bool)CheckboxParts.IsChecked)
            {
                var dt = ((DataView)DataGridParts.ItemsSource).ToTable();

                var q = @"SELECT * FROM PartsException";
                var du = sql.SelectDb(q, out var res);
                if (dt.Rows.Count != du.Rows.Count)
                {
                    q = "truncate table PartsException";
                    sql.Update(q);
                }

                foreach (DataRow item in dt.Rows)
                {
                    var query =
                        $@"IF NOT EXISTS(SELECT 1 from PartsException where Part = '{item["Part"]}') Insert INTO PartsException 
                                    (Part) VALUES('{item["Part"]}')";
                    try
                    {
                        sql.Update(query);
                    }
                    catch (Exception ex)
                    {
                        ErrorOut("At PartsSettingsSaving" + ex.Message);
                    }
                }

                GetExcludedPartsList();
            }

            if (CheckboxCustomers.IsChecked != null && (bool)CheckboxCustomers.IsChecked)
            {
                var dt = new DataTable();
                dt = ((DataView)DataGridCustomers.ItemsSource).ToTable();

                var q = @"SELECT * FROM CustomersNeedLTS";
                var du = sql.SelectDb(q, out var res);
                if (dt.Rows.Count != du.Rows.Count)
                {
                    q = "truncate table CustomersNeedLTS";
                    sql.Update(q);
                }

                foreach (DataRow item in dt.Rows)
                {
                    var query =
                        string.Format(@"IF NOT EXISTS(SELECT 1 from CustomersNeedLTS where Customer = '{0}') Insert INTO CustomersNeedLTS 
                                    (Customer) VALUES('{0}')", item["Customer"]);
                    try
                    {
                        sql.Update(query);
                    }
                    catch (Exception ex)
                    {
                        ErrorOut("At CustomerSettingsSaving" + ex.Message);
                    }
                }

                GetCustomerList();
            }

            if (CheckboxRecipes.IsChecked != null && (bool)CheckboxRecipes.IsChecked)
            {
                var dt = new DataTable();
                dt = ((DataView)DataGridRecipes.ItemsSource).ToTable();

                var q = @"SELECT * FROM RecipeException";
                var du = sql.SelectDb(q, out var res);
                if (dt.Rows.Count != du.Rows.Count)
                {
                    q = "truncate table RecipeException";
                    sql.Update(q);
                }

                foreach (DataRow item in dt.Rows)
                {
                    var query =string.Format(@"IF NOT EXISTS(SELECT 1 from RecipeException where recipe = '{0}') Insert INTO RecipeException
                                    (recipe) VALUES('{0}')", item["recipe"]);
                    try
                    {
                        sql.Update(query);
                    }
                    catch (Exception ex)
                    {
                        ErrorOut("At CustomerSettingsSaving" + ex.Message);
                    }
                }
                GetRecipeList();
            }
            if(CheckboxProgramms.IsChecked != null && (bool)CheckboxProgramms.IsChecked)
            {
                var dt = new DataTable();
                dt = ((DataView)DataGridProgramm.ItemsSource).ToTable();

                var q = @"SELECT * FROM ProgrammsException1";
                var du = sql.SelectDb(q, out var res);
                if (dt.Rows.Count != du.Rows.Count)
                {
                    q = "truncate table ProgrammsException1";
                    sql.Update(q);
                    foreach (DataRow item in dt.Rows)
                    {
                        var query = string.Format(@"IF NOT EXISTS(SELECT 1 from ProgrammsException1 where Programm = '{0}') Insert INTOProgrammsException1
                                    (Programm) VALUES('{0}')", item["Programm"]);
                        try
                        {
                            sql.Update(query);
                        }
                        catch (Exception ex)
                        {
                            ErrorOut("At CustomerSettingsSaving" + ex.Message);
                        }
                    }
                    GetProgrammList();
                }

            }
        }

    }
}