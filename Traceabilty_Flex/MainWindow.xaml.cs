using System;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Configuration;

namespace Traceabilty_Flex
{
    public partial class MainWindow : Window
    {
        public static MainWindow mn;
        private DispatcherTimer _settingsRefreshTimer;
        public MainWindow()
        {
            InitializeComponent();
            _settingsRefreshTimer = new DispatcherTimer();
            _settingsRefreshTimer.Interval = TimeSpan.FromMinutes(1); // One minute interval
            _settingsRefreshTimer.Tick += SettingsRefreshTimer_Tick;
            _settingsRefreshTimer.Start();

            GridLines.ItemsSource = DTRecipes.AsDataView();

            //Shrink_DB_Log.Tick += Shrink_DB_Log_Tick;
            //Shrink_DB_Log.Interval = new TimeSpan(1, 0, 0);
            //Shrink_DB_Log.Start();

            CheckHost(); // Check if local host is trusted

            monitorWorker.DoWork += MonitorWorker_DoWork; // connect to core ( subscription to oib ? )
            monitorWorker.RunWorkerCompleted += MonitorWorker_RunWorkerCompleted;

            recipeWorker.DoWork += RecipeWorker_DoWork;
            recipeWorker.RunWorkerCompleted += RecipeWorker_RunWorkerCompleted;

            FillLineCollection(); // creates collection of lines from siplace database
            FillRecipeDt(); // fill Data Table of CURRENT Recipes [dbo.current]
            FillFirstLast(); // check which station # is the first or last in the line

            GetActiveLines(); // fill DataTable of Active Lines [dbo.lines]

            _mWindow = this;

            if (_mainservice)
                MainServiceJob();// see information in function ( running mainservice only in mignt100 !! ) 
            if (_trusted)
                TrustedJob();// see information in function ( running only in trusted pc's such mignt0664 and mignt100  ! )

            CommonJob(); // see in info in function

            ControlInit(); // initialize all buttons / combolists / text boxes, etc....

            monitorWorker.RunWorkerAsync();
            recipeWorker.RunWorkerAsync();
        }

        private void SettingsRefreshTimer_Tick(object sender, EventArgs e)
        {
            GetSettings();
        }

        private void Shrink_DB_Log_Tick(object sender, EventArgs e)
        {
            try
            {
                SqlClass sql = new SqlClass("MIGSQLCLU4\\SMT", "Traceability", "shahar", "shachar222");
                //string qry = @"ALTER DATABASE Traceability SET RECOVERY SIMPLE
                //            DBCC SHRINKFILE (Traceability_log, 9)
                //            ALTER DATABASE Traceability
                //            SET RECOVERY FULL";
                string qry = @"DBCC SHRINKFILE (Traceability_log, 9)";

                sql.Update(qry);
                LogWriter.WriteLog("In Shrink_DB Success");
            }
            catch (Exception ex)
            {
                LogWriter.WriteLog("In Shrink_DB Error" + ex.ToString());
            }
        }


        private void CommonJob()
        {
            GetExcludedPartsList(); // parts that should be checked 
            GetCustomerList(); // get customers in one string
            GetStatusLines(); // get a table of current lines ( line, recepie, and time )
           
        }

        private void GetActiveLines()
        {

            var sql = new SqlClass("trace");
            var query = @"SELECT * FROM Lines";

            DTActiveLines = sql.SelectDb(query, out var result);
            if (result != null)
                ErrorOut(result);
        }

        internal void FillRecipeDt()
        {
            try
            {
                var sql = new SqlClass("trace");
                var query = "SELECT * FROM [Current] order by line ASC";

                DTRecipes.Clear();
                DTRecipes = sql.SelectDb(query, out var result);

                Dispatcher.BeginInvoke(new Action(delegate ()
                {
                    try
                    {
                        GridLines.ItemsSource = null;
                        GridLines.ItemsSource = DTRecipes.AsDataView();
                    }
                    catch (Exception ex)
                    {
                        ErrorOut("At ShowLines: " + ex.Message);
                    }
                }));

                if (result != null)
                    ErrorOut(result);

                foreach (DataRow item in DTRecipes.Rows)
                {
                    var s = item["line"].ToString().Trim();
                    RecipeDictionary[s] = item["receipe"].ToString().Trim();
                }
            }
            catch (Exception ex)
            {
                ErrorOut("At ShowLines: " + ex.Message);
            }
        }

        private void TrustedJob()
        {
            GetSettings(); // loading settings to data table from [dbo].Lines / users / parts / customers
        }

        private void ControlInit()
        {
            FlashTextBlocs = new TextBlock[] { TextInput1, TextInput2, TextInput3, TextInput4 };
            ComboList = new ComboBox[] { ComboBox1, ComboBox2, ComboBox3, ComboBox4 };
            TextBlockList = new TextBlock[] { TextQty1, TextQty2, TextQty3, TextQty4 };
            TextBlockStations = new TextBlock[] { TextStation1, TextStation2, TextStation3, TextStation4 };
            _buttons = new Button[] { ButtonA, ButtonB, ButtonC, ButtonD, ButtonE, ButtonF, ButtonG, ButtonH, ButtonI, ButtonJ, ButtonK, ButtonL, ButtonM, ButtonN, ButtonO, ButtonP, ButtonQ1, ButtonQ2, ButtonR1, ButtonR2, ButtonS1, ButtonS2 };
            TabOverview.IsSelected = true;
        }

        private void MainServiceJob()
        {
            ClearTraceLines();
            displayWorker.DoWork += DisplayWorker_DoWork;
            displayWorker.RunWorkerCompleted += DisplayWorker_RunWorkerCompleted;
            displayWorker.RunWorkerAsync(); // subscription of display service

            _started = true;
            ButStart.Visibility = Visibility.Collapsed;
        }

        private void CheckHost()
        {
            var s = ConfigurationManager.AppSettings["MyHost"];
            TrustedHosts = s.Split(',');

            if (Array.IndexOf(TrustedHosts, Environment.MachineName.ToLower()) != -1)
            {
                ButtonLogin.Content = "Trusted Host";
                ButtonLogin.Background = Brushes.White;
                User = "11111";

                Level = 4;
                TabSettings.Visibility = Visibility.Visible;
                ButStart.Visibility = Visibility.Visible;
                _trusted = true;
            }
        }

        private void FillFirstLast()
        {
            foreach (var f in LineCollection)
            {
                var query = string.Format("SELECT * FROM RecipeList WHERE line='{0}'", f.Name);
                var sql = new SqlClass("trace");

                var dtr = sql.SelectDb(query, out var result);

                if (result != null)
                    ErrorOut("At FillFirstLast: " + result);

                if (dtr.Rows.Count > 0)
                    SetFirstLast(dtr, f);
            }
        }

        private void DisplayWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_client != null)
                LabelLineDisplay.Background = Brushes.Green;
        }

        private void DisplayWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            DisplayConnection();
        }

        private void MonitorWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            LabelRecipeDownload.Background = Brushes.Green;

            if (!StartTraceability())
                ErrorOut("Traceability subscribing had fault");
        }

        private void MonitorWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ConnectToCore();
        }

        private void RecipeWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Progress.IsIndeterminate = false;
            Progress.Visibility = Visibility.Hidden;

            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                LabelLineControl.Background = Brushes.Green;
            }));

            _mainservice = true;
        }

        private void RecipeWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var recipeSetupList = StartLineControl();
            if (recipeSetupList == null || recipeSetupList.Count == 0)
            {
                ErrorOut("Line Control subscribing had fault");
                return;
            }
            else
            {
                FillRecipes(recipeSetupList);
            }
        }

        private void ClearTraceLines()
        {
            var sql = new SqlClass("trace");

            var query = string.Format("truncate table [Traceability].[dbo].[TraceList]");

            try
            {
                sql.Update(query);
            }
            catch (SqlException ex)
            {
                ErrorOut("At ClearTraceLines: " + ex.Message);
            }

        }

        private void GetStatusLines()
        {
            var sql = new SqlClass("trace");
            var query = @"SELECT * FROM [Current]";

            var d = sql.SelectDb(query, out var result);

            if (result != null)
                ErrorOut("At GetStatusLines: " + result);

            try
            {
                if (d.Rows.Count > 0)
                {
                    foreach (DataRow dr in d.Rows)
                    {
                        var line = dr["line"].ToString().Trim();
                        var recipe = dr["receipe"].ToString().Trim();

                        var client = recipe.Length < 10 ? "Unknown" : GetClient(recipe.Substring(0, 10));

                        RegisterLineStatus(line, client, recipe);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOut("At GetStatusLines: " + ex.Message);
            }
        }
        public static bool isBoardException(string boardToCheck)
        {
            return ProgrammExceptionList.Any(word => boardToCheck.Contains(word));
        }
        private bool RegisterLineStatus(string line, string client, string recipe)
        {
            if (RecipeExceptionList != null)
                foreach (var str in RecipeExceptionList)
                {
                    if (recipe.Contains(str))
                    {
                        StatusDictionary[line] = true;
                        return true;
                    }
                }
            if (CustomerList != null && Array.IndexOf(CustomerList, client) != -1)
            {
                StatusDictionary[line] = true;
                return true;
            }
            else
                StatusDictionary[line] = false;
            return false;
        }


        private void ButStart_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckIfRun())
            {
                if (!_started)
                    MainServiceJob();
            }
        }

        private void ChangeStatus(int v)
        {
            var sql = new SqlClass("trace");
            var query =
                $"UPDATE Status SET [status] = '{v}', [time] = CONVERT(VARCHAR, '{DateTime.Now.ToString(TimeFormat)}', 103), [user] = '{User}', [host] = '{Environment.MachineName}'";
            try
            {
                sql.Update(query);
            }
            catch (SqlException ex)
            {
                ErrorOut("At ChangeStatus: " + ex.Message);
            }
        }

        private bool CheckIfRun()
        {
            var sql = new SqlClass("trace");
            var query = "SELECT * FROM Status";

            DataTable d = null;

            d = sql.SelectDb(query, out var result);

            if (result != null)
                ErrorOut("At CheckIfRun: " + result);

            var stat = (int)d.Rows[0]["status"];
            var usr = d.Rows[0]["user"].ToString().Trim();
            var hst = d.Rows[0]["host"].ToString().Trim();

            if (stat == 0)
            {
                _mainservice = true;
                return false;
            }
            else if (stat == 1 && usr == User && hst == Environment.MachineName)
            {
                _mainservice = true;
                return false;
            }
            else
            {
                _mainservice = false;

                var s =
                    $"The service is working now.\nIt was launched by {d.Rows[0]["user"].ToString()}\non host {d.Rows[0]["host"].ToString()}\nat {((DateTime)d.Rows[0]["time"]).ToString(TimeFormat)}";
                MessageBox.Show(s);
            }
            return true;
        }

        private void GetCustomerList()
        {
            var sql = new SqlClass("trace");

            CustomerList = sql.GetJoinedList("trace", "SELECT Customer FROM CustomersNeedLTS", ',', out var Result).Split(',');

            if (Result != null)
                ErrorOut("Error getting 'PartsException' : <br/>\n" + Result);
        }

        private void GetExcludedPartsList()
        {
            var sql = new SqlClass("trace");

            PartsException = sql.GetJoinedList("trace", "SELECT Part FROM PartsException", ',', out var Result).Split(',');
            if (Result != null)
                ErrorOut("Error getting 'PartsException' : <br/>\n" + Result);
        }

        private void FillLineCollection()
        {
            LineCollection?.Clear();

            var query = @"SELECT distinct [Line] ,[Station] FROM [SiplacePro].[dbo].[1_Line_Config] Where Line Like 'L%'";

            var sql = new SqlClass();

            var d = sql.SelectDb(query, out var result);

            if (result != null)
                ErrorOut("At FillLineCollection: " + result);

            foreach (DataRow dr in d.Rows)
            {
                var line = dr["Line"].ToString();
                var station = dr["Station"].ToString();


                if (line.Count() > 6) continue;//fixing Error of 'Line-Q-'

                if (line == "Line-R")
                    line = "Line-R1";
                if (line == "Line-S")
                    line = "Line-S1";
                if (line == "Line-Q")
                    line = "Line-Q1";

                var f = FindInCollection(line);
                if (f == null)
                {
                    f = new FlexLine(line);
                    LineCollection?.Add(f);
                }
                f.AddStation(station);
            }

            var l = new FlexLine("Line-R2");
            LineCollection?.Add(l);
            l.AddStation("Sipl1-X4S_R");
            l.AddStation("Sipl2-X4S_R");
            l = new FlexLine("Line-S2");
            LineCollection?.Add(l);
            l.AddStation("Sipl1-X4S_S");
            l.AddStation("Sipl2-X4S_S");
            l = new FlexLine("Line-Q2");
            LineCollection?.Add(l);
            l.AddStation("Sipl1_TXV2_Q");
            l.AddStation("Sipl2_TX_Q");
            l.AddStation("Sipl3_TXV2_Q");
            l.AddStation("Sipl4_TXV2_Q");
        }

        private void SetFirstLast(DataTable dtr, FlexLine f)
        {
            try
            {
                var groupedData = from b in dtr.AsEnumerable()
                                  group b by b.Field<string>("Station") into g
                                  select new
                                  {
                                      station = g.Key,
                                      List = g.ToList()
                                  };

                var max = 1;
                var min = 1;

                f.Used = new string[groupedData.Count()];

                var i = 0;

                foreach (var a in groupedData)
                {
                    var st = a.station.Trim();

                    var x = f.StationDictionary[st];

                    f.Used[i++] = st;

                    if (x > max)
                        max = x;
                    if (x < min)
                        min = x;
                }

                var last = f.StationDictionary.FirstOrDefault(x => x.Value == max).Key;
                var first = f.StationDictionary.FirstOrDefault(x => x.Value == min).Key;

                f.Last = last;
                f.First = first;
            }
            catch (Exception ex)
            {
                ErrorOut("At SetFirstLast: " + f.Name + "  " + ex.Message);
            }
        }

        private FlexLine FindInCollection(string line)
        {
            try
            {
                foreach (var t in LineCollection)
                {
                    if (t.Name == line)
                        return t;
                }
            }
            catch (Exception ex)
            {
                ErrorOut("At findInCollection: " + ex.Message);
            }

            return null;
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabOverview == null || TabSettings == null || TabEvents == null || BorderLine == null) return;

            if (TabOverview.IsSelected || TabSettings.IsSelected || TabEvents.IsSelected)
            {
                BorderLine.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (TabTails.IsSelected)
                {
                    ShowCurrentRecipesList();
                    CheckPallets();
                }
                else if (TabLineManagement.IsSelected)
                {
                    CheckPallets();
                }

                BorderLine.Visibility = Visibility.Visible;
            }
        }

        private void CheckPallets()
        {
            
            for (var i = 0; i < LineList.Count; i++)
            {
                var sql = new SqlClass("trace");
                var query = string.Format("SELECT TOP 1 [id] FROM TraceList WHERE line='{0}'", LineList[i]);

                var d = sql.SelectDb(query, out var result);

                if (result != null)
                    ErrorOut("At CheckPallets: " + result);

                if (d.Rows.Count > 0)
                {
                    if (!StatusDictionary.ElementAt(i).Value)
                        ClearTrace(LineList[i]);
                    else
                        _buttons[i].Background = _mainservice ? Brushes.Green : Brushes.Yellow;
                }
                else if (StatusDictionary.ElementAt(i).Value)
                    _buttons[i].Background = Brushes.LightGray;
                else
                    _buttons[i].Background = null;
            }
        }

        private void ClearTrace(string line)
        {
            var sql = new SqlClass("trace");
            var query = string.Format("DELETE FROM TraceList WHERE line='{0}'", line);

            try
            {
                sql.Update(query);
            }
            catch (SqlException ex)
            {
                ErrorOut("At ClearTrace: " + ex.Message);
            }
        }

        private void AddMessage(string v)
        {
            if (v == "") return;

            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                if (ListEvents.Items.Count > 200)
                    ClearListBox(ListEvents);

                ListEvents.Items.Add(DateTime.Now.ToString("HH:mm:ss") + " " + v);
            }));
        }


        private void ErrorMessage(string v)
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                ListErrors.Items.Add(DateTime.Now.ToString("HH:mm:ss") + " " + v);
            }));
        }

        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!_mainservice)
                return;

            var list = GridPallet.SelectedItems;
            var sql = new SqlClass("trace");
            string query;
            var line = TextBoxCurrentLine.Text;

            if (list.Count == 0) return;

            foreach (var item in list)
            {
                var s = ((DataRowView)item).Row.ItemArray[0].ToString();
                query = string.Format("DELETE FROM TraceList WHERE line='{0}' and pallet='{1}'", line, s);

                sql.Delete(query);

                var f = FindInCollection(TextBoxCurrentLine.Text);
                if (f.PalletDictionary.ContainsKey(s))
                    f.PalletDictionary.Remove(s);
            }

            query = string.Format("SELECT distinct pallet FROM TraceList WHERE line='{0}' order by pallet", line);

            var dt = sql.SelectDb(query, out var result);

            if (result != null)
                ErrorOut("At ButtonDelete_Click: " + result);

            GridPallet.ItemsSource = null;
            GridPallet.ItemsSource = dt.AsDataView();
        }

        private void ButStop_Click(object sender, RoutedEventArgs e)
        {
            
            PasswordDialog passwordDialog = new PasswordDialog();
            if (passwordDialog.ShowDialog() == true)
            {
                string enteredPassword = passwordDialog.Password;

                
                if (enteredPassword == "katibroner") 
                {
                    
                    UnsubscribeTraceability();

                    if (Level == 4 && _mainservice)
                    {
                        ClearTraceLines();
                    }

                    
                    Close();
                }
                else
                {
                    MessageBox.Show("Incorrect password. The program continues to run.");
                   
                }
            }
            else
            {
                
                MessageBox.Show("Closing cancelled.");
            }
        }

        private void ButClean_Click(object sender, RoutedEventArgs e)
        {
            LstMsgBox.Items.Clear();
            GridRecipe.ItemsSource = null;
            GridTrace.ItemsSource = null;
            GridPallet.ItemsSource = null;
            ListErrors.Items.Clear();
            ListEvents.Items.Clear();
            CheckPallets();
        }

        public void CheckIfDBEmpty(string line)
        {
            var sql = new SqlClass("trace");
            var query = string.Format("SET DEADLOCK_PRIORITY HIGH SELECT distinct pallet FROM TraceList WHERE line='{0}' order by pallet", line);

            var d = sql.SelectDb(query, out var result);

            if (result != null)
            {
                ErrorOut("At CheckIfDBEmpty: SelectDb " + result);
                return;
            }
            try
            {
                var index = LineCollection.FindIndex(a => a.Name == line);

                Dispatcher.BeginInvoke(new Action(delegate ()
                {
                    if (TabTails.IsSelected && TextBoxCurrentLine.Text == line)
                    {
                        GridPallet.ItemsSource = null;
                        GridPallet.ItemsSource = d.AsDataView();
                    }

                    if (d.Rows.Count > 0)
                        _buttons[index].Background = Brushes.Green;
                    else if (StatusDictionary.ElementAt(index).Value)
                        _buttons[index].Background = Brushes.LightGray;
                    else
                        _buttons[index].Background = null;
                }));
            }
            catch (Exception ex)
            {
                ErrorOut("At CheckIfDBEmpty: " + ex.Message);
            }
        }

        private void Button_Login_Click(object sender, RoutedEventArgs e)
        {
            if (ButtonLogin.Content == null || ButtonLogin.Content.ToString() == "")
            {
                var log = new Login()
                {
                    Owner = this
                };

                var result = log.ShowDialog();

                if (result ?? false)
                {
                    var sql = new SqlClass("login");
                    var query = $"SELECT * FROM [AOI].[dbo].[employers] WHERE work_number='{User}'";

                    var d = sql.SelectDb(query, out var res);
                    if (res != null)
                        ErrorOut("At Login: " + res);

                    if (d.Rows.Count > 0)
                    {
                        var psw = d.Rows[0]["password"].ToString().Trim();
                        if (psw == Password)
                        {
                            ButtonLogin.Content = d.Rows[0]["name"].ToString().Trim();
                            ButtonLogin.Background = Brushes.White;
                            User = d.Rows[0]["work_number"].ToString().Trim();
                            Password = psw;

                            var lvl = (int)d.Rows[0]["lvl"];

                            if (lvl == 4)
                            {
                                Level = 4;
                                TabSettings.Visibility = Visibility.Visible;
                                ButStart.Visibility = Visibility.Visible;
                                TrustedJob();
                            }
                            else if (lvl == 3)
                            {
                                Level = 3;
                                TabSettings.Visibility = Visibility.Visible;
                                ButStart.Visibility = Visibility.Collapsed;
                            }
                            else if (lvl == 2)
                            {
                                Level = 2;
                                TabSettings.Visibility = Visibility.Collapsed;
                                ButStart.Visibility = Visibility.Collapsed;

                            }
                            else
                            {
                                Level = 1;
                                TabSettings.Visibility = Visibility.Collapsed;
                                ButStart.Visibility = Visibility.Collapsed;

                            }
                        }
                    }
                }
            }
            else
            {
                User = "";
                Password = "";
                ButtonLogin.Content = null;
                ButtonLogin.Background = (Brush)Application.Current.MainWindow.FindResource("ButtonLogin2");
                TabSettings.Visibility = Visibility.Collapsed;
                ButStart.Visibility = Visibility.Collapsed;
                Level = 1;
            }
        }

        internal void RegisterPallet(string line, string pallet, string station, string board, string setup, string recipe, out bool last, out bool over)
        {
            last = false;
            over = false;

            try
            {
                var f = FindInCollection(line);

                if (Array.IndexOf(f.Used, station) == -1)
                    return;

                if (station == f.First)
                {
                    CheckRecipe(line, board, setup, recipe);

                    if (!f.PalletDictionary.ContainsKey(pallet))
                        f.PalletDictionary.Add(pallet, 1);
                    else
                        f.PalletDictionary[pallet] = 1;
                }
                if (station == f.Last)
                {
                    f.PalletDictionary.Remove(pallet);
                    last = true;
                }
                else if (station != f.First && station != f.Last)
                {
                    if (f.PalletDictionary.ContainsKey(pallet))
                        f.PalletDictionary[pallet]++;
                    else
                        f.PalletDictionary.Add(pallet, f.StationDictionary[station]);
                }
                var qty = 8;

                var k = 1;

                if (f.PalletDictionary.ContainsKey(pallet))
                {
                    k = f.PalletDictionary[pallet];
                    var count = 0;
                    for (var i = 0; i < f.PalletDictionary.Count; i++)
                    {
                        if (f.PalletDictionary.ElementAt(i).Value == k)
                            count++;
                    }
                    if (count > qty)
                        over = true;
                }
            }
            catch (Exception ex)
            {
                ErrorOut("At RegisterPallet: " + ex.Message);
            }
        }

        internal void CheckRecipe(string line, string board, string setup, string recipe)
        {
            if (RecipeDictionary[line] != recipe)
            {
                RecipeDictionary[line] = recipe;
                FillOneRecipe(new string[] { setup, recipe, line });
            }
        }

        internal void RegisterPalletA(string line, string pallet, string station)
        {
            try
            {
                var f = FindInCollection(line);
                var index = LineCollection.FindIndex(a => a.Name == line);
                if (station == f.First)
                {
                    if (f.PalletDictionary.ContainsKey(pallet))
                        return;
                    f.PalletDictionary.Add(pallet, 1);
                    _buttons[index].Background = Brushes.Yellow;
                }
                if (station == f.Last)
                {
                    f.PalletDictionary.Remove(pallet);
                    if (f.PalletDictionary.Count == 0)
                        _buttons[index].Background = null;
                    else if (StatusDictionary.ElementAt(index).Value)
                        _buttons[index].Background = Brushes.LightGray;

                }
                else if (station != f.First && station != f.Last)
                {
                    if (f.PalletDictionary.ContainsKey(pallet))
                        f.PalletDictionary[pallet]++;
                    else
                        f.PalletDictionary.Add(pallet, f.StationDictionary[station]);

                    _buttons[index].Background = Brushes.Yellow;
                }
            }
            catch (Exception ex)
            {
                ErrorOut("At RegisterPallet: " + ex.Message);
            }
        }

        private void AnimateBoard(FlexLine f, string station, string pallet, string line)
        {
            if (TabLineManagement.IsSelected && line == TextBoxCurrentLineLm.Text)
            {
                var i = f.StationDictionary[station];

                switch (i)
                {
                    case 1:
                        var task = Task.Run(() => Move1());
                        //Move1();
                        break;
                    case 2:
                        var task1 = Task.Run(() => Move2());
                        //Move2();
                        break;
                    case 3:
                        var task2 = Task.Run(() => Move3());
                        //Move3();
                        break;
                    case 4:
                        var task3 = Task.Run(() => Move4());
                        //Move4();
                        break;
                }
            }
        }

        private void Move4()
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                MyImg4.Opacity = 1;
                MyImg4.Visibility = Visibility.Visible;
                var sb4 = (this.FindResource("MovingImage4") as Storyboard);
                Timeline.SetDesiredFrameRate(sb4, 30);
                sb4.Completed += Sb4_Completed;
                sb4.Begin();
            }));
        }

        private void Sb4_Completed(object sender, EventArgs e)
        {
            MyImg4.Opacity = 0;
            var sb = (this.FindResource("MovingImage14") as Storyboard);
            sb.Begin();
        }

        private void Move3()
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                MyImg3.Opacity = 1;
                MyImg3.Visibility = Visibility.Visible;
                var sb3 = (this.FindResource("MovingImage3") as Storyboard);
                //sb3.Completed += delegate { MyImg3.Visibility = Visibility.Hidden; };
                Timeline.SetDesiredFrameRate(sb3, 30);
                sb3.Completed += Sb3_Completed;
                sb3.Begin();
            }));
        }

        private void Sb3_Completed(object sender, EventArgs e)
        {
            MyImg3.Opacity = 0;
            var sb = (this.FindResource("MovingImage13") as Storyboard);
            sb.Begin();
        }

        private void Move2()
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                MyImg2.Opacity = 1;
                MyImg2.Visibility = Visibility.Visible;
                var sb2 = (this.FindResource("MovingImage2") as Storyboard);
                Timeline.SetDesiredFrameRate(sb2, 30);
                //sb2.Completed += delegate { MyImg2.Visibility = Visibility.Hidden; };
                sb2.Completed += Sb2_Completed;
                sb2.Begin();
            }));
        }

        private void Sb2_Completed(object sender, EventArgs e)
        {
            MyImg2.Opacity = 0;
            var sb = (this.FindResource("MovingImage12") as Storyboard);
            sb.Begin();
        }

        private void Move1()
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                MyImg1.Opacity = 1;
                MyImg1.Visibility = Visibility.Visible;
                var sb1 = (this.FindResource("MovingImage1") as Storyboard);
                Timeline.SetDesiredFrameRate(sb1, 30);
                //     sb1.Completed += delegate { MyImg1.Visibility = Visibility.Hidden; };
                sb1.Completed += Sb1_Completed;
                sb1.Begin();
            }));
        }

        private void Sb1_Completed(object sender, EventArgs e)
        {
            MyImg1.Opacity = 0;
            var sb = (this.FindResource("MovingImage11") as Storyboard);
            sb.Begin();
        }

        internal void TrackPallet(string line, string station, string pallet)
        {
            if (TabLineManagement.IsSelected && line == TextBoxCurrentLineLm.Text)
            {
                try
                {
                    var f = FindInCollection(line);
                    ClearInputButtons();
                    AnimateBoard(f, station, pallet, line);

                    if (f.PalletDictionary.Count > 0)
                    {
                        for (var i = 0; i < f.PalletDictionary.Count; i++)
                        {
                            if (i >= f.PalletDictionary.Count)
                            {
                                ErrorOut("PalletDictionary smaller than it has to be");
                                continue;
                            }
                            var b = FlashTextBlocs[f.PalletDictionary.ElementAt(i).Value];
                            b.Background = SystemColors.GradientActiveCaptionBrush;
                            var tt = f.PalletDictionary.ElementAt(i).Key;
                            ComboList[f.PalletDictionary[tt]].Items.Add(tt);
                        }

                        for (var j = 0; j < ComboList.Length; j++)
                        {
                            var k = ComboList[j].Items.Count;
                            if (k != 0)
                            {
                                TextBlockList[j].Text = k.ToString();
                                var flashButton = FindResource("FlashButton" + (j + 1).ToString()) as Storyboard;
                                flashButton.Begin();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorOut("At TrackPallet1: " + ex.Message);
                }
            }
        }

        internal void TrackPallet(FlexLine f)
        {
            try
            {
                ClearInputButtons();

                if (!CheckDBPallets(f.Name))
                    f.PalletDictionary.Clear();

                if (f.PalletDictionary.Count > 0)
                {
                    for (var i = 0; i < f.PalletDictionary.Count; i++)
                    {
                        var b = FlashTextBlocs[f.PalletDictionary.ElementAt(i).Value];
                        b.Background = SystemColors.GradientActiveCaptionBrush;
                        var tt = f.PalletDictionary.ElementAt(i).Key;
                        ComboList[f.PalletDictionary[tt]].Items.Add(tt);
                    }

                    for (var j = 0; j < ComboList.Length; j++)
                    {
                        var k = ComboList[j].Items.Count;
                        if (k != 0)
                        {
                            TextBlockList[j].Text = k.ToString();
                            var flashButton = FindResource("FlashButton" + (j + 1).ToString()) as Storyboard;
                            flashButton.Begin();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOut("At TrackPallet2: " + ex.Message);
            }
        }

        private bool CheckDBPallets(string line)
        {
            var sql = new SqlClass("trace");
            var query = string.Format("SELECT distinct pallet FROM TraceList WHERE line='{0}' order by pallet", line);

            var d = sql.SelectDb(query, out var result);

            if (result != null)
            {
                ErrorOut("At CheckDBPallets: " + result);
                return false;
            }

            return d.Rows.Count > 0;
        }

        private void ClearInputButtons()
        {
            for (var i = 0; i < FlashTextBlocs.Length; i++)
            {
                FlashTextBlocs[i].Background = null;
                FlashTextBlocs[i].ToolTip = null;
                ComboList[i].Items.Clear();
                TextBlockList[i].Text = "0";
                var flashButton = FindResource("FlashButton" + (i + 1).ToString()) as Storyboard;
                flashButton.Stop();
            }
        }

        private void RadioAction_Checked(object sender, RoutedEventArgs e)
        {
            if (TextBoxCurrentLineLm.Text == "")
            {
                MessageBox.Show("Choose the line!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                RadioInfo.IsChecked = true;
                return;
            }
            if (Level < 3)
            {
                RadioInfo.IsChecked = true;
                MessageBox.Show("You have not permissions to change line state!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
                GetLineState(TextBoxCurrentLineLm.Text);
        }

        private void RadioInfo_Checked(object sender, RoutedEventArgs e)
        {
            if (ButtonLineStop != null)
                ButtonLineStop.Content = "Line Info";
        }

        private void RadioMessage_Checked(object sender, RoutedEventArgs e)
        {
            if (Level < 3)
            {
                RadioInfo.IsChecked = true;
                MessageBox.Show("You have not permissions to change line state!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
                ButtonLineStop.Content = "Line Control";
        }

        private void GetLineState(string text)
        {
            var sql = new SqlClass("trace");

            var query = $"SELECT Adam FROM Lines WHERE Line = '{text}'";

            var d = sql.SelectDb(query, out var result);
            if (result != null)
                ErrorOut("At GetLineState: " + result);

            if (d.Rows.Count > 0)
                if ((bool)d.Rows[0]["Adam"] == true)
                    ButtonLineStop.Content = "Stop Line";
                else
                    ButtonLineStop.Content = "Start Line";
        }

        public static string GetLeaf(object strWithBackSlash)
        {
            if (strWithBackSlash == null) return "";

            var arr = strWithBackSlash.ToString().Split('\\');
            return arr[arr.Length - 1];
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            ClearListBox(ListErrors);
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            ClearListBox(LstMsgBox);
        }

        private void ClearAllBoxes()
        {
            ClearListBox(LstMsgBox);
            ClearListBox(ListErrors);
            ClearListBox(ListEvents);
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            ClearListBox(ListEvents);
        }

        private void ClearListBox(ListBox box)
        {
            if (box.Items.Count == 0) return;

            if (!Directory.Exists(_dir))
                Directory.CreateDirectory(_dir);

            var pth = _dir + "\\" + box.Name + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";

            using (var sw = new StreamWriter(pth))
            {
                foreach (var item in box.Items)
                {
                    sw.WriteLine(item.ToString());
                }
            }

            box.Items.Clear();
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            GetSettings();
        }

        public void Cleaning()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }


        private void ComboDelay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboDelay.Text != "")
                _delay = Convert.ToInt32(((System.Windows.Controls.ContentControl)ComboDelay.SelectedValue).Content);
        }

        private void AdamTrace_Checked(object sender, RoutedEventArgs e)
        {
            _checkLine = true;
        }

        private void AdamTrace_Unchecked(object sender, RoutedEventArgs e)
        {
            _checkLine = false;
        }

        private void MenuItem_Click2(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(ListErrors.SelectedItem.ToString());
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void ButtonQ2_Click(object sender, RoutedEventArgs e)
        {
            var v = "Line-Q2";

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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            PasswordDialog passwordDialog = new PasswordDialog();
            if (passwordDialog.ShowDialog() == true)
            {
                string enteredPassword = passwordDialog.Password;

                // Check the password you entered
                if (enteredPassword != "katibroner")
                {
                    MessageBox.Show("Incorrect password. Closing cancelled.");
                    e.Cancel = true; // Cancel closure
                }
            }
            else
            {
                e.Cancel = true; // Cancel close if window is closed without entering password
            }
        }
    }
}
