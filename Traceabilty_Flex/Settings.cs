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

        private void GetSettings()
        {
            DataTable d1 = GetUsers();
            dataGridUsers.ItemsSource = d1.AsDataView();

            DataTable d2 = GetLines();
            dataGridLineConfig.ItemsSource = d2.AsDataView();

            DataTable d3 = GetParts();
            dataGridParts.ItemsSource = d3.AsDataView();

            DataTable d4 = GetCustomers();
            dataGridCustomers.ItemsSource = d4.AsDataView();

            DataTable d5 = GetRecipeList();
            dataGridRecipes.ItemsSource = d5.AsDataView();
            //LineDatePicker.Text = GetDateLineUpgrade();
        }

        private string GetDateLineUpgrade()
        {
            SQLClass sql = new SQLClass("trace");//LtsMonitor
            string query = @"SELECT *
                FROM [Traceability].[dbo].[LineUpgrade]";

            DataTable d = sql.SelectDB(query, out string result);

            return d.Rows[0][0].ToString();
        }

        private DataTable GetCustomers()
        {
            SQLClass sql = new SQLClass("trace");//LtsMonitor
            string query = @"SELECT [Customer]
                FROM [Traceability].[dbo].[CustomersNeedLTS]";

            DataTable d = sql.SelectDB(query, out string result);

            return d;
        }

        private DataTable GetParts()
        {
            SQLClass sql = new SQLClass("trace");//LtsMonitor
            string query = @"SELECT [Part]
                FROM [Traceability].[dbo].[PartsException]";

            DataTable d = sql.SelectDB(query, out string result);

            return d;
        }

        private DataTable GetLines()
        {
            SQLClass sql = new SQLClass("trace");//LtsMonitor
            string query = @"SELECT [Line]
                ,[IP]
                ,[Active]
                ,[Monitor]
                ,[DBPath]
                ,[dtLastCheck]
                ,[skid]
                FROM [Traceability].[dbo].[Lines]";

            DataTable d = sql.SelectDB(query, out string result);

            return d;
        }

        private DataTable GetUsers()
        {
            SQLClass sql = new SQLClass("trace");//LtsMonitor
            string query = @"SELECT [ID]
                ,[FirstName]
                ,[LastName]
                ,[Role]
                ,[eMail]
                ,[Admin]
                FROM [Traceability].[dbo].[Users]";

            DataTable d = sql.SelectDB(query, out string result);

            return d;
        }

        private DataTable GetRecipeList()
        {
            SQLClass sql = new SQLClass("trace");
            string query = @"SELECT [recipe]
                FROM [Traceability].[dbo].[RecipeException]";

            DataTable d = sql.SelectDB(query, out string result);

            return d;


        }
        private void CheckboxLines_Checked(object sender, RoutedEventArgs e)
        {
            dataGridLineConfig.IsReadOnly = false;
        }

        private void CheckboxLines_Unchecked(object sender, RoutedEventArgs e)
        {
            dataGridLineConfig.IsReadOnly = true;

        }

        private void CheckboxUsers_Unchecked(object sender, RoutedEventArgs e)
        {
            dataGridUsers.IsReadOnly = true;
        }

        private void CheckboxUsers_Checked(object sender, RoutedEventArgs e)
        {
            dataGridUsers.IsReadOnly = false;

        }

        private void CheckboxParts_Checked(object sender, RoutedEventArgs e)
        {
            dataGridParts.IsReadOnly = false;
        }

        private void CheckboxParts_Unchecked(object sender, RoutedEventArgs e)
        {
            dataGridParts.IsReadOnly = true;
        }

        private void CheckboxCustomers_Checked(object sender, RoutedEventArgs e)
        {
            dataGridCustomers.IsReadOnly = false;
        }

        private void CheckboxCustomers_Unchecked(object sender, RoutedEventArgs e)
        {
            dataGridCustomers.IsReadOnly = true;
        }
        private void CheckboxRecipes_Checked(object sender, RoutedEventArgs e)
        {
            dataGridRecipes.IsReadOnly = false;
        }

        private void CheckboxRecipes_Unchecked(object sender, RoutedEventArgs e)
        {
            dataGridRecipes.IsReadOnly = true;
        }

    }
}