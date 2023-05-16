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
        private void SetActiveStations(FlexLine f)
        {
            foreach (var item in TextBlockStations)
            {
                item.Background = (f.Used != null && Array.IndexOf(f.Used, item.Text) != -1) ? SystemColors.GradientActiveCaptionBrush : null;
            }
        }

        private void ShowPallets(string v)
        {
            var f = FindInCollection(v);
            var d = new DataTable();
            d.Columns.Add("pallet", typeof(string));

            foreach (var item in f.PalletDictionary)
            {
                var dr = d.NewRow();
                dr["pallet"] = item.Key;
                d.Rows.Add(dr);
            }
            GridPallet.ItemsSource = null;
            GridPallet.ItemsSource = d.AsDataView();
        }

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

        private void ShowLineManagement(FlexLine item)
        {
            try
            {
                var n = item.StationDictionary.Count;

                switch (n)
                {
                    case 2:
                        StackMachine1.Visibility = Visibility.Visible;
                        StackMachine2.Visibility = Visibility.Visible;
                        StackMachine3.Visibility = Visibility.Collapsed;
                        StackMachine4.Visibility = Visibility.Collapsed;
                        TextStation1.Text = item.Stations[0];
                        TextStation2.Text = item.Stations[1];
                        TextStation3.Text = "";
                        TextStation4.Text = "";

                        break;
                    case 3:
                        StackMachine1.Visibility = Visibility.Visible;
                        StackMachine2.Visibility = Visibility.Visible;
                        StackMachine3.Visibility = Visibility.Visible;
                        StackMachine4.Visibility = Visibility.Collapsed;
                        TextStation1.Text = item.Stations[0];
                        TextStation2.Text = item.Stations[1];
                        TextStation3.Text = item.Stations[2];
                        TextStation4.Text = "";
                        break;
                    case 4:
                        StackMachine1.Visibility = Visibility.Visible;
                        StackMachine2.Visibility = Visibility.Visible;
                        StackMachine3.Visibility = Visibility.Visible;
                        StackMachine4.Visibility = Visibility.Visible;
                        TextStation1.Text = item.Stations[0];
                        TextStation2.Text = item.Stations[1];
                        TextStation3.Text = item.Stations[2];
                        TextStation4.Text = item.Stations[3];
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

        private void ShowCurrentRecipesList()
        {
            try
            {
                FillRecipeDt();
                GridLines.ItemsSource = null;
                GridLines.ItemsSource = DTRecipes.AsDataView();
            }
            catch (Exception ex)
            {
                ErrorOut("At ShowLines: " + ex.Message);
            }
        }

        private void ShowTail(string line)
        {
            try
            {
                var query = string.Format("SELECT distinct pallet FROM TraceList WHERE line='{0}' order by pallet", line);

                var sql = new SqlClass("trace");

                var d = sql.SelectDb(query, out var result);
                if (result != null)
                    ErrorOut(result);

                GridPallet.ItemsSource = null;
                GridPallet.ItemsSource = d.AsDataView();

                TextBoxCurrentLine.Text = line;
            }
            catch (Exception ex)
            {
                ErrorOut("At ShowLines: " + ex.Message);
            }
        }

        private void ShowTrace(string line)
        {
            try
            {
                var sql = new SqlClass("trace");
                var query = string.Format("SELECT * FROM TraceList WHERE line='{0}'", line);

                var d = sql.SelectDb(query, out var result);

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

        private void ShowRecipe(string line)
        {
            try
            {
                var sql = new SqlClass("trace");
                var query = string.Format("SELECT * FROM RecipeList WHERE line='{0}'", line);

                var d = sql.SelectDb(query, out var result);

                if (result != null)
                    ErrorOut(result);

                GridRecipe.ItemsSource = null;
                GridRecipe.ItemsSource = d.AsDataView(); //DataTable dt = ((DataView)dataGrid1.ItemsSource).ToTable();
            }
            catch (Exception ex)
            {
                ErrorOut("At ShowRecipe: " + ex.Message);
            }
        }

        #region Buttons Events
        private void ButtonA_Click(object sender, RoutedEventArgs e)
        {
            var v = "Line-A";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
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
            var v = "Line-B";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
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
            var v = "Line-C";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
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
            var v = "Line-D";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
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
            var v = "Line-E";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
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
            var v = "Line-F";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
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
            var v = "Line-G";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
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
            var v = "Line-H";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
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
            var v = "Line-I";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
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
            var v = "Line-J";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
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
            var v = "Line-K";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
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
            var v = "Line-L";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
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
            var v = "Line-M";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
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
            var v = "Line-N";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
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
            var v = "Line-O";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
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
            var v = "Line-P";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonQ_Click(object sender, RoutedEventArgs e)
        {
            var v = "Line-Q1";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonR1_Click(object sender, RoutedEventArgs e)
        {
            var v = "Line-R1";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonR2_Click(object sender, RoutedEventArgs e)
        {
            var v = "Line-R2";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonS1_Click(object sender, RoutedEventArgs e)
        {
            var v = "Line-S1";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
                if (f != null)
                {
                    ShowLineManagement(f);
                    TrackPallet(f);
                    SetActiveStations(f);
                    ClearInfo(v);
                }
            }
        }

        private void ButtonS2_Click(object sender, RoutedEventArgs e)
        {
            var v = "Line-S2";

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
                TextBoxCurrentLineLm.Text = v;
                var f = FindInCollection(v);
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