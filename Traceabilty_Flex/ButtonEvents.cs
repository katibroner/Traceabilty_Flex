using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TraceabilityTestGui;

namespace Traceabilty_Flex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region SetActiveStations
        private void SetActiveStations(FlexLine f)
        {
            foreach (TextBlock item in TextBlockStations)
            {
                item.Background = (f.Used != null && Array.IndexOf(f.Used, item.Text) != -1) ? SystemColors.GradientActiveCaptionBrush : null;
            }
        }
        #endregion

        #region ShowPallets
        private void ShowPallets(string v)
        {
            FlexLine f = FindInCollection(v);
            DataTable d = new DataTable();
            d.Columns.Add("pallet", typeof(string));

            foreach (var item in f.PalletDictionary)
            {
                DataRow dr = d.NewRow();
                dr["pallet"] = item.Key;
                d.Rows.Add(dr);
            }
            GridPallet.ItemsSource = null;
            GridPallet.ItemsSource = d.AsDataView();
        }
        #endregion

        #region ClearInfo
        private void ClearInfo(string v)
        {
            try
            {
                MsgClear();
            }
            catch (Exception ex)
            {
                ErrorOut("At MsgClear: " + ex.Message);
            }
        }
        #endregion

        #region ShowLineManagement
        private void ShowLineManagement(FlexLine item)
        {
            try
            {
                int n = item.StationDictionary.Count;

                switch (n)
                {
                    case 2:
                        stackMachine1.Visibility = Visibility.Visible;
                        stackMachine2.Visibility = Visibility.Visible;
                        stackMachine3.Visibility = Visibility.Collapsed;
                        stackMachine4.Visibility = Visibility.Collapsed;
                        textStation1.Text = item.Stations[0];
                        textStation2.Text = item.Stations[1];
                        textStation3.Text = "";
                        textStation4.Text = "";

                        break;
                    case 3:
                        stackMachine1.Visibility = Visibility.Visible;
                        stackMachine2.Visibility = Visibility.Visible;
                        stackMachine3.Visibility = Visibility.Visible;
                        stackMachine4.Visibility = Visibility.Collapsed;
                        textStation1.Text = item.Stations[0];
                        textStation2.Text = item.Stations[1];
                        textStation3.Text = item.Stations[2];
                        textStation4.Text = "";
                        break;
                    case 4:
                        stackMachine1.Visibility = Visibility.Visible;
                        stackMachine2.Visibility = Visibility.Visible;
                        stackMachine3.Visibility = Visibility.Visible;
                        stackMachine4.Visibility = Visibility.Visible;
                        textStation1.Text = item.Stations[0];
                        textStation2.Text = item.Stations[1];
                        textStation3.Text = item.Stations[2];
                        textStation4.Text = item.Stations[3];
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                ErrorOut("At ShowLineManagement: " + ex.Message);
            }
        }
        #endregion

        #region ShowLines
        private void ShowCurrentRecipesList()
        {
            try
            {
                GridLines.ItemsSource = null;
                GridLines.ItemsSource = DTRecipes.AsDataView();
            }
            catch(Exception ex)
            {
                ErrorOut("At ShowLines: " + ex.Message);
            }
        }
        #endregion

        #region ShowTail
        private void ShowTail(string v)
        {
            try
            {
                string line = v.Replace("Line-", "Trace_");

                string query = "SELECT distinct pallet FROM [" + line + "] order by pallet";

                SQLClass sql = new SQLClass("trace");

                DataTable d = sql.SelectDB(query, out string result);
                if (result != null)
                    ErrorOut(result);

                GridPallet.ItemsSource = null;
                GridPallet.ItemsSource = d.AsDataView();

                TextBoxCurrentLine.Text = v;
            }
            catch(Exception ex)
            {
                ErrorOut("At ShowLines: " + ex.Message);
            }
        }
        #endregion

        #region ShowTrace
        private void ShowTrace(string v)
        {
            try
            {
                SQLClass sql = new SQLClass("trace");

                string query = string.Format("SELECT * FROM [Traceability].[dbo].[{0}]", v.Replace("Line-", "Trace_"));

                DataTable d = sql.SelectDB(query, out string result);
                if (result != null)
                    ErrorOut(result);

                GridTrace.ItemsSource = null;
                GridTrace.ItemsSource = d.AsDataView();
            }
            catch (Exception ex)
            {
                ErrorOut("At ShowTrace: " + ex.Message);
            }
        }
        #endregion

        #region ShowRecipe
        private void ShowRecipe(string v)
        {
            try
            {
                SQLClass sql = new SQLClass("trace");

                string query = string.Format("SELECT * FROM [Traceability].[dbo].[{0}]", v.Replace("Line-", "Receipe_"));

                DataTable d = sql.SelectDB(query, out string result);
                if (result != null)
                    ErrorOut(result);

                GridRecipe.ItemsSource = null;
                GridRecipe.ItemsSource = d.AsDataView(); //DataTable dt = ((DataView)dataGrid1.ItemsSource).ToTable();
            }
            catch(Exception ex)
            {
                ErrorOut("At ShowRecipe: " + ex.Message);
            }
        }
        #endregion

        #region Buttons
        private void ButonA_Click(object sender, RoutedEventArgs e)
        {
            string v = "Line-A";

            if (TabRecipe.IsSelected)
            {
                ShowRecipe(v);
            }
            else if (TabTrace.IsSelected)
            {
                ShowTrace(v);
            }
            else if (TabTails.IsSelected)
            {
                if (_mainservice)
                    ShowTail(v);
                else
                    ShowPallets(v);
            }
            else if (TabLineManagement.IsSelected)
            {
                textBoxCurrentLineLM.Text = v;
                FlexLine f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonB_Click(object sender, RoutedEventArgs e)
        {
            string v = "Line-B";

            if (TabRecipe.IsSelected)
            {
                ShowRecipe(v);
            }
            else if (TabTrace.IsSelected)
            {
                ShowTrace(v);
            }
            else if (TabTails.IsSelected)
            {
                if (_mainservice)
                    ShowTail(v);
                else
                    ShowPallets(v);
            }
            else if (TabLineManagement.IsSelected)
            {
                textBoxCurrentLineLM.Text = v;
                FlexLine f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonC_Click(object sender, RoutedEventArgs e)
        {
            string v = "Line-C";

            if (TabRecipe.IsSelected)
            {
                ShowRecipe(v);
            }
            else if (TabTrace.IsSelected)
            {
                ShowTrace(v);
            }
            else if (TabTails.IsSelected)
            {
                if (_mainservice)
                    ShowTail(v);
                else
                    ShowPallets(v);
            }
            else if (TabLineManagement.IsSelected)
            {
                textBoxCurrentLineLM.Text = v;
                FlexLine f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonD_Click(object sender, RoutedEventArgs e)
        {
            string v = "Line-D";

            if (TabRecipe.IsSelected)
            {
                ShowRecipe(v);
            }
            else if (TabTrace.IsSelected)
            {
                ShowTrace(v);
            }
            else if (TabTails.IsSelected)
            {
                if (_mainservice)
                    ShowTail(v);
                else
                    ShowPallets(v);
            }
            else if (TabLineManagement.IsSelected)
            {
                textBoxCurrentLineLM.Text = v;
                FlexLine f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonE_Click(object sender, RoutedEventArgs e)
        {
            string v = "Line-E";

            if (TabRecipe.IsSelected)
            {
                ShowRecipe(v);
            }
            else if (TabTrace.IsSelected)
            {
                ShowTrace(v);
            }
            else if (TabTails.IsSelected)
            {
                if (_mainservice)
                    ShowTail(v);
                else
                    ShowPallets(v);
            }
            else if (TabLineManagement.IsSelected)
            {
                textBoxCurrentLineLM.Text = v;
                FlexLine f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonF_Click(object sender, RoutedEventArgs e)
        {
            string v = "Line-F";

            if (TabRecipe.IsSelected)
            {
                ShowRecipe(v);
            }
            else if (TabTrace.IsSelected)
            {
                ShowTrace(v);
            }
            else if (TabTails.IsSelected)
            {
                if (_mainservice)
                    ShowTail(v);
                else
                    ShowPallets(v);
            }
            else if (TabLineManagement.IsSelected)
            {
                textBoxCurrentLineLM.Text = v;
                FlexLine f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonG_Click(object sender, RoutedEventArgs e)
        {
            string v = "Line-G";

            if (TabRecipe.IsSelected)
            {
                ShowRecipe(v);
            }
            else if (TabTrace.IsSelected)
            {
                ShowTrace(v);
            }
            else if (TabTails.IsSelected)
            {
                if (_mainservice)
                    ShowTail(v);
                else
                    ShowPallets(v);
            }
            else if (TabLineManagement.IsSelected)
            {
                textBoxCurrentLineLM.Text = v;
                FlexLine f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonH_Click(object sender, RoutedEventArgs e)
        {
            string v = "Line-H";

            if (TabRecipe.IsSelected)
            {
                ShowRecipe(v);
            }
            else if (TabTrace.IsSelected)
            {
                ShowTrace(v);
            }
            else if (TabTails.IsSelected)
            {
                if (_mainservice)
                    ShowTail(v);
                else
                    ShowPallets(v);
            }
            else if (TabLineManagement.IsSelected)
            {
                textBoxCurrentLineLM.Text = v;
                FlexLine f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonI_Click(object sender, RoutedEventArgs e)
        {
            string v = "Line-I";

            if (TabRecipe.IsSelected)
            {
                ShowRecipe(v);
            }
            else if (TabTrace.IsSelected)
            {
                ShowTrace(v);
            }
            else if (TabTails.IsSelected)
            {
                if (_mainservice)
                    ShowTail(v);
                else
                    ShowPallets(v);
            }
            else if (TabLineManagement.IsSelected)
            {
                textBoxCurrentLineLM.Text = v;
                FlexLine f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonJ_Click(object sender, RoutedEventArgs e)
        {
            string v = "Line-J";

            if (TabRecipe.IsSelected)
            {
                ShowRecipe(v);
            }
            else if (TabTrace.IsSelected)
            {
                ShowTrace(v);
            }
            else if (TabTails.IsSelected)
            {
                if (_mainservice)
                    ShowTail(v);
                else
                    ShowPallets(v);
            }
            else if (TabLineManagement.IsSelected)
            {
                textBoxCurrentLineLM.Text = v;
                FlexLine f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonK_Click(object sender, RoutedEventArgs e)
        {
            string v = "Line-K";

            if (TabRecipe.IsSelected)
            {
                ShowRecipe(v);
            }
            else if (TabTrace.IsSelected)
            {
                ShowTrace(v);
            }
            else if (TabTails.IsSelected)
            {
                if (_mainservice)
                    ShowTail(v);
                else
                    ShowPallets(v);
            }
            else if (TabLineManagement.IsSelected)
            {
                textBoxCurrentLineLM.Text = v;
                FlexLine f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonL_Click(object sender, RoutedEventArgs e)
        {
            string v = "Line-L";

            if (TabRecipe.IsSelected)
            {
                ShowRecipe(v);
            }
            else if (TabTrace.IsSelected)
            {
                ShowTrace(v);
            }
            else if (TabTails.IsSelected)
            {
                if (_mainservice)
                    ShowTail(v);
                else
                    ShowPallets(v);
            }
            else if (TabLineManagement.IsSelected)
            {
                textBoxCurrentLineLM.Text = v;
                FlexLine f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonM_Click(object sender, RoutedEventArgs e)
        {
            string v = "Line-M";

            if (TabRecipe.IsSelected)
            {
                ShowRecipe(v);
            }
            else if (TabTrace.IsSelected)
            {
                ShowTrace(v);
            }
            else if (TabTails.IsSelected)
            {
                if (_mainservice)
                    ShowTail(v);
                else
                    ShowPallets(v);
            }
            else if (TabLineManagement.IsSelected)
            {
                textBoxCurrentLineLM.Text = v;
                FlexLine f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonN_Click(object sender, RoutedEventArgs e)
        {
            string v = "Line-N";

            if (TabRecipe.IsSelected)
            {
                ShowRecipe(v);
            }
            else if (TabTrace.IsSelected)
            {
                ShowTrace(v);
            }
            else if (TabTails.IsSelected)
            {
                if (_mainservice)
                    ShowTail(v);
                else
                    ShowPallets(v);
            }
            else if (TabLineManagement.IsSelected)
            {
                textBoxCurrentLineLM.Text = v;
                FlexLine f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonO_Click(object sender, RoutedEventArgs e)
        {
            string v = "Line-O";

            if (TabRecipe.IsSelected)
            {
                ShowRecipe(v);
            }
            else if (TabTrace.IsSelected)
            {
                ShowTrace(v);
            }
            else if (TabTails.IsSelected)
            {
                if (_mainservice)
                    ShowTail(v);
                else
                    ShowPallets(v);
            }
            else if (TabLineManagement.IsSelected)
            {
                textBoxCurrentLineLM.Text = v;
                FlexLine f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonP_Click(object sender, RoutedEventArgs e)
        {
            string v = "Line-P";

            if (TabRecipe.IsSelected)
            {
                ShowRecipe(v);
            }
            else if (TabTrace.IsSelected)
            {
                ShowTrace(v);
            }
            else if (TabTails.IsSelected)
            {
                if (_mainservice)
                    ShowTail(v);
                else
                    ShowPallets(v);
            }
            else if (TabLineManagement.IsSelected)
            {
                textBoxCurrentLineLM.Text = v;
                FlexLine f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }
        #endregion
    }
}