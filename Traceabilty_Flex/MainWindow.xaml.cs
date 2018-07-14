//using schemas.xmlsoap.org.ws._2004._08.eventing;
using System;
using System.Collections;
using System.Collections.Generic;
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
using TraceabilityTestGui;
using System.Configuration;
using System.Net;

namespace Traceabilty_Flex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            CheckFileHosts(); // check if we have hosts in hosts file

            InitializeComponent();

            CheckHost(); // check if local host is trusted
            CheckIfNotRun(); // check if mainservice is running 

            monitorWorker.DoWork += MonitorWorker_DoWork; // connect to core ( subscription to oib ? )
            monitorWorker.RunWorkerCompleted += MonitorWorker_RunWorkerCompleted;
           // qmsWorker.DoWork += QmsWorker_DoWork; // ping sending 
           // qmsWorker.RunWorkerCompleted += QmsWorker_RunWorkerCompleted;

            _timerLost.Elapsed += _timerLost_Elapsed;

            FillLineCollection(); // creates collection of lines from siplace database
            FillRecipeDT(); // fill Data Table of CURRENT Recipes [dbo.current]
            FillFirstLast(); // check which station # is the first or last in the line

            GetActiveLines(); // fill DataTable of Active Lines [dbo.lines]

            _mWindow = this;

            if (_mainservice)
                MainServiceJob();// see information in function ( running mainservice only in mignt100 !! ) 
            if (_trusted)
                TrustedJob();// see information in function ( running only in trusted pc's such mignt0664 and mignt100  ! )

            CommonJob(); // see in info in function

            ControlInit(); // initialize all buttons / combolists / text boxes, etc....
            //qmsWorker.RunWorkerAsync();

            _pingTimer = new DispatcherTimer();
            _pingTimer.Interval = TimeSpan.FromSeconds(10);
            _pingTimer.Tick += _pingTimer_Tick;
   //         _pingTimer.Start();// Open!!!

            monitorWorker.RunWorkerAsync();
        }

        private void _pingTimer_Tick(object sender, EventArgs e)
        {
            qmsWorker.RunWorkerAsync();
        }

        private void QmsWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        
                using (StreamWriter sw = File.AppendText("c:\\Logs\\PingTest.txt"))
                {
                    if (!PingResult)    
                    sw.WriteLine(DateTime.Now.ToString() + " : ping OK");
                else sw.WriteLine(DateTime.Now.ToString() + " : ping NOT OK");

                sw.Flush();
                    sw.Close();
                }


            
            //if (!PingResult)
            //    Utils.SendMail(Utils.GetJoinedList("trace", "select eMail from [Users] where [Admin] = '20'", ';', out string Result)
            //             , ""
            //             , "No answer from QMS"
            //             , "No answer from QMS.\nLast message was obtained at:  " + LastTimeFromQMS.ToString());
        }

        private void QmsWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            PingResult = SendPingToQMS();
        }

        private bool SendPingToQMS()
        {
            string json = "{\"base\":{\"flex_user_code\":\"A014\",\"password\":\"$Flex2099\",\"customer_code\":\"0000\",\"function_name\":\"lms3_get_time\"}}";

            string address = "http://10.229.8.35/qms3/web_services/ws_json.php";
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(address);
            httpWebRequest.ContentType = "text/json";
            httpWebRequest.Method = "POST";

                  using (StreamWriter streamWriter = new StreamWriter(@"C:\Tmp\qmstest633.txt", true))

            // using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(json);
            }

            try
            {
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var text = streamReader.ReadToEnd();
                    return DateTime.TryParse(text, out LastTimeFromQMS);
                }
            }
            catch (Exception ex)
            {
                MainWindow.WriteLog("SendToService\t" + ex.Message);
                MessageBox.Show(ex.ToString());
            }

            return false;
        }

        private void CheckFileHosts()
        {
            string path = @"C:\Windows\System32\drivers\etc\hosts";
            string templ = @"10.229.5.65   smt-a";
            bool success = false;

            if (!File.Exists(path))
            {
                MessageBox.Show("File:\n" + path + "\nnot found.","", MessageBoxButton.OK,
    MessageBoxImage.Information, MessageBoxResult.OK,
    MessageBoxOptions.DefaultDesktopOnly);
                Environment.Exit(1);
            }

            using (StreamReader sr = new StreamReader(path))
            {
                while(!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (line == templ)
                    {
                        success = true;
                        break;
                    }
                }
            }

            if(!success)
            {
                    MessageBox.Show( "File:\n" + path + "\ndoes not contains lines definition\n" + templ,"", MessageBoxButton.OK,
    MessageBoxImage.Information, MessageBoxResult.OK,
    MessageBoxOptions.DefaultDesktopOnly);
                Environment.Exit(1);
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
            SQLClass sql = new SQLClass("trace");
            string query = @"SELECT * FROM [Traceability].[dbo].[Lines]";

            DTActiveLines = sql.SelectDB(query, out string Result);
            if(Result != null)
                ErrorOut(Result);
        }

        internal void FillRecipeDT()
        {
            try
            {
                SQLClass sql = new SQLClass("trace");

                string query = "SELECT * FROM [Traceability].[dbo].[Current]";

                DTRecipes.Clear();
                DTRecipes = sql.SelectDB(query, out string result);

                if (result != null)
                    ErrorOut(result);

                foreach (DataRow item in DTRecipes.Rows)
                {
                    string s = item["line"].ToString().Trim();
                    RecipeDictionary[s] = item["receipe"].ToString().Trim();
                }
            }
            catch (Exception ex)
            {
                ErrorOut("At ShowLines: " + ex.Message);
            }
        }

        #region TrustedJob
        private void TrustedJob()
        {
            GetSettings(); // loading settings to datatable from [dbo].Lines / users / parts / customers

            DatePickerFrom.Text = DateTime.Now.ToLongDateString();
            DatePickerTo.Text = DateTime.Now.ToLongDateString();
        }
        #endregion

        #region ControlInit
        private void ControlInit()
        {
            FlashTextBlocs = new TextBlock[] { textInput1, textInput2, textInput3, textInput4 };
            ComboList = new ComboBox[] { comboBox1, comboBox2, comboBox3, comboBox4 };
            TextBlockList = new TextBlock[] { TextQty1, TextQty2, TextQty3, TextQty4 };
            TextBlockStations = new TextBlock[] { textStation1, textStation2, textStation3, textStation4 };
            _buttons = new Button[] { butonA, buttonB, buttonC, buttonD, buttonE, buttonF, buttonG, buttonH, buttonI, buttonJ, buttonK, buttonL, buttonM, buttonN, buttonO, buttonP };
            TabOverview.IsSelected = true;
        }
        #endregion

        #region MainServiceJob
        private void MainServiceJob() 
        {
            ClearTraceLines(); // trancate all trace db from [Traceability].[dbo].trace A/B/.../P
            displayWorker.DoWork += DisplayWorker_DoWork;
            displayWorker.RunWorkerCompleted += DisplayWorker_RunWorkerCompleted;

            displayWorker.RunWorkerAsync(); // subscription of display service

            _started = true;
            ChangeStatus(1); // set active = 1 in [dbo].status
            butStart.Visibility = Visibility.Collapsed;

            if (AdamSetup != null) AdamSetup.IsEnabled = true;
            if(AdamTrace != null) AdamTrace.IsEnabled = true;
            if(AdamPartNoID != null) AdamPartNoID.IsEnabled = true;
            if(AdamLimit != null) AdamLimit.IsEnabled = true;
            if(ComboQty != null) ComboQty.IsEnabled = true;
            //if (AdamOrder != null) AdamOrder.IsEnabled = true;
            //if (ComboMin != null) ComboMin.IsEnabled = true;
        }

        #endregion

        #region CheckIfNotRun
        private void CheckIfNotRun()
        {
            SQLClass sql = new SQLClass("trace");
            string query = "SELECT * FROM [Traceability].[dbo].[Status]";

            DataTable d = sql.SelectDB(query, out string result);

            if (result != null)
                ErrorOut("At CheckIfRun: " + result);

            if (d.Rows.Count == 0)
            {
                MessageBox.Show("Database is not accessible.", "Network issue", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(5);
            }

            int stat = Convert.ToInt32(d.Rows[0]["status"]);

            if ((stat == 0 && _trusted) || (stat == 1 && d.Rows[0]["host"].ToString() == Environment.MachineName))
                _mainservice = true;
            else
                butStart.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region CheckHost
        private void CheckHost()
        {
            string s = ConfigurationManager.AppSettings["MyHost"];
            TrustedHosts = s.Split(',');

            if(Array.IndexOf(TrustedHosts, Environment.MachineName.ToLower()) != -1)
            {
                button_Login.Content = "Trusted Host";
                button_Login.Background = Brushes.White;
                User = "11111";

                Level = 4;
                TabSettings.Visibility = Visibility.Visible;
                //TabSearch.Visibility = Visibility.Visible;
                butStart.Visibility = Visibility.Visible;
               _trusted = true; 
            }
        }
        #endregion

        #region FillFirstLast
        private void FillFirstLast()
        {
           foreach(FlexLine f in LineCollection)
            {
                string query = string.Format("SELECT * FROM [Traceability].[dbo].[{0}]", f.Name.Replace("Line-", "Receipe_"));

                SQLClass sql = new SQLClass("trace");

                DataTable dtr = sql.SelectDB(query, out string result);

                if (result != null)
                    ErrorOut("At FillFirstLast: " + result);
                //if(dtr.Rows.Count == 0)
                //{
                //   dtr = GetRecipe(RecipeDictionary[f.Name], f.Name);
                //    if(dtr.Rows.Count > 0)
                //        WriteRecipeToDBLine(dtr, f.Name);
                //}
                if (dtr.Rows.Count > 0)
                    SetFirstLast(dtr, f);
            }
        }
        #endregion

        #region DisplayWorker
        private void DisplayWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(_client != null)
                LabelLineDisplay.Background = Brushes.Green;
        }

        private void DisplayWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            DisplayConnection();
        }
        #endregion

        #region MonitorWorker
        private void MonitorWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            LabelRecipeDownload.Background = Brushes.Green;

            if (!StartTraceability())
                ErrorOut("Traceability subscribing had failt");
        }

        private void MonitorWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ConnectToCore();
        }
        #endregion

        #region RecipeWorker
        private void RecipeWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progress.IsIndeterminate = false;
            progress.Visibility = Visibility.Hidden;

            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                LabelLineControl.Background = Brushes.Green;
            }));

            _mainservice = true;
            //ClearRegistryA();
        }

        private void RecipeWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<string[]> RecipeSetupList = StartLineControl();
            if (RecipeSetupList == null || RecipeSetupList.Count == 0)
            {
                ErrorOut("Line Control subscribing had failt");
                return;
            }
            else
            {
                FillRecipes(RecipeSetupList);
            }
        }
        #endregion

        #region ClearTraceLines
        private void ClearTraceLines()
        {
            SQLClass sql = new SQLClass("trace");

            for (int i = 0; i < _lines.Length; i++)
            {
                string query = string.Format("truncate table [Traceability].[dbo].[{0}]",GetLeaf( _lines[i].Replace("Line-", "Trace_")));
                try
                {
                    sql.Update(query);
                }
                catch (SqlException ex)
                {
                    ErrorOut("At ClearTraceLines: " + ex.Message);
                }
            }
        }
        #endregion

        #region GetStatusLines
        private void GetStatusLines()
        {
            SQLClass sql = new SQLClass("trace");
            string query = @"SELECT * FROM [Traceability].[dbo].[Current]";

            DataTable d = sql.SelectDB(query, out string result);

            if (result != null)
                ErrorOut("At GetStatusLines: " + result);

            try
            {
                if (d.Rows.Count > 0)
                {
                    foreach (DataRow dr in d.Rows)
                    {
                        string line = dr["line"].ToString().Trim();
                        string recipe = dr["receipe"].ToString().Trim();
 
                        string client = recipe.Length < 10 ? "Unknown" : GetClient(recipe.Substring(0, 10));

                        RegisterLineStatus(line, client);
                    }
                }
            }
            catch(Exception ex)
            {
                ErrorOut("At GetStatusLines: " + ex.Message);
            }
        }
        #endregion

        #region RegisterLineStatus
        private bool RegisterLineStatus(string line, string client)
        {
            //if (CustomerList != null && Array.IndexOf(CustomerList, client) != -1)
            //{
            //    StatusDictionary[line] = true;
            //    return true;
            //}
            //else
            //    StatusDictionary[line] = false;
            //return false;

            return StatusDictionary[line] = (CustomerList != null && Array.IndexOf(CustomerList, client) != -1);
        }
        #endregion

        #region ClearRegistryA
        private void ClearRegistryA()
        {
            foreach (Button item in _buttons)
                item.Background = null;

            foreach (FlexLine f in LineCollection)
                f.PalletDictionary.Clear();
        }
        #endregion

        #region butStart_Click
        private void ButStart_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckIfRun())
            {
                if (!_started)
                    MainServiceJob();

   //             StartSubscriptions();
            }
        }
        #endregion

        #region StartSubscriptions
        private void StartSubscriptions()
        {
            progress.Visibility = Visibility.Visible;
            progress.IsIndeterminate = true;
            butStart.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region ChangeStatus
        private void ChangeStatus(int v)
        {
            SQLClass sql = new SQLClass("trace");
            string query = string.Format("UPDATE [Traceability].[dbo].[Status] SET [status] = '{0}', [time] = CONVERT(VARCHAR, '{1}', 103), [user] = '{2}', [host] = '{3}'", v, DateTime.Now.ToString(TimeFormat), User, Environment.MachineName);
            try
            {
                sql.Update(query);
            }
            catch(SqlException ex)
            {
                ErrorOut("At ChangeStatus: " + ex.Message);
            }
        }
        #endregion

        #region CheckIfRun
        private bool CheckIfRun()
        {
            SQLClass sql = new SQLClass("trace");
            string query = "SELECT * FROM [Traceability].[dbo].[Status]";

            DataTable d = null;

            d = sql.SelectDB(query, out string result);

            if (result != null)
                 ErrorOut("At CheckIfRun: " + result);

            int stat = (int)d.Rows[0]["status"];
            string usr = d.Rows[0]["user"].ToString().Trim();
            string hst = d.Rows[0]["host"].ToString().Trim();

            if (stat == 0)
            {
                _mainservice = true; 
                return false;
            }
            else if(stat == 1 && usr == User && hst == Environment.MachineName)
            {
                _mainservice = true;
                return false;
            }
            else
            {
                _mainservice = false;

                string s = string.Format("The service is working now.\nIt was launched by {0}\non host {1}\nat {2}", 
                    d.Rows[0]["user"].ToString(),
                    d.Rows[0]["host"].ToString(),
                    ((DateTime)d.Rows[0]["time"]).ToString(TimeFormat));
                MessageBox.Show(s);
            }
            return true;
        }
        #endregion

        #region GetCustomerList
        private void GetCustomerList()
        {
            SQLClass sql = new SQLClass("trace");

            CustomerList = sql.GetJoinedList("trace", "SELECT Customer FROM CustomersNeedLTS", ',', out string Result).Split(',');

            if (Result != null)
                ErrorOut("Error getting 'PartsException' : <br/>\n" + Result);
        }
        #endregion

        #region GetExcludedPartsList
        private void GetExcludedPartsList()
        {
            SQLClass sql = new SQLClass("trace");

            PartsException = sql.GetJoinedList("trace", "SELECT Part FROM PartsException", ',', out string Result).Split(',');
            if (Result != null)
                ErrorOut("Error getting 'PartsException' : <br/>\n" + Result);
        }
        #endregion

        #region FillLineCollection
        private void FillLineCollection()
        {
            if(LineCollection != null)
                LineCollection.Clear();

            //string query = string.Format( @"SELECT TOP (100) PERCENT StationPath, MachineId
            //    FROM dbo.Stati m  on
            //    WHERE (LastModifiedDate > CONVERT(DATETIME, '{0}', 102))
            //    ORDER BY StationPath", _lastLineDate);

            string query = @"SELECT distinct [Line] ,[Station] FROM [SiplacePro].[dbo].[1_Line_Config]";

            SQLClass sql = new SQLClass();

            DataTable d = sql.SelectDB(query, out string result);

            if (result != null)
                ErrorOut("At FillLineCollection: " + result);

            foreach (DataRow dr in d.Rows)
            {
                //string s = dr["StationPath"].ToString();
                //string[] str = s.Split('\\');
                //string line = str[1];
                //string station = str[2];
                string line = dr["Line"].ToString();
                string station = dr["Station"].ToString();

                FlexLine f = FindInCollection(line);

                if (f == null)
                {
                    f = new FlexLine(line);
                    LineCollection.Add(f);
                }
                f.AddStation(station);
            }
        }
        #endregion

        #region SetFirstLast
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

                int max = 1;
                int min = 1;

                f.Used = new string[groupedData.Count()];

                int i = 0;

                foreach (var a in groupedData)
                {
                    string st = a.station.Trim();

                    int x = f.StationDictionary[st];

                    f.Used[i++] = st;

                    if (x > max)
                        max = x;
                    if (x < min)
                        min = x;
                }

                string last = f.StationDictionary.FirstOrDefault(x => x.Value == max).Key;
                string first = f.StationDictionary.FirstOrDefault(x => x.Value == min).Key;

                f.Last = last;
                f.First = first;
            }
            catch (Exception ex)
            {
                ErrorOut("At SetFirstLastInLine: " + f.Name + "  " + ex.Message);
            }
        }
        #endregion

        #region FindInCollection
        private FlexLine FindInCollection(string line)
        {
            try
            {

                for (int i = 0; i < LineCollection.Count; i++)
                {
                    if (LineCollection[i].Name == line)
                        return LineCollection[i];
                }
            }
            catch (Exception ex)
            {
                ErrorOut("At findInCollection: "  + ex.Message);
            }

            return null;// LineCollection[LineCollection.FindIndex(a => a.Name == line)];
        }
        #endregion

        #region MainTabControl_SelectionChanged
        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabOverview == null || TabSettings == null || TabEvents == null || TabPlacements == null || TabSearch == null || BorderLine == null) return;

            if (TabOverview.IsSelected || TabSettings.IsSelected || TabEvents.IsSelected || TabPlacements.IsSelected || TabSearch.IsSelected)
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

            _placement = TabPlacements.IsSelected;
        }
        #endregion

        #region CheckPallets
        private void CheckPallets()
        {
            for (int i = 0; i < _lines.Length; i++)
            {
                string s = GetLeaf( _lines[i]).Replace("Line-","Trace_");

                SQLClass sql = new SQLClass("trace");
                string query = string.Format("SELECT TOP 1 [id] FROM [Traceability].[dbo].[{0}]", s);

                DataTable d = sql.SelectDB(query, out string result);

                if (result != null)
                    ErrorOut("At CheckPallets: " + result);

                if (d.Rows.Count > 0)
                {
                    if (!StatusDictionary.ElementAt(i).Value)
                        ClearTrace(s);
                    else
                        _buttons[i].Background = _mainservice ? Brushes.Green : Brushes.Yellow;
                }
                else if (StatusDictionary.ElementAt(i).Value)
                    _buttons[i].Background = Brushes.LightGray;
                else
                    _buttons[i].Background = null;
            }
        }

        private void ClearTrace(string v)
        {
            SQLClass sql = new SQLClass("trace");
            string query = string.Format("truncate table [Traceability].[dbo].[{0}]", v);
            try
            {
                sql.Update(query);
            }
            catch (SqlException ex)
            {
                ErrorOut("At ClearTrace: " + ex.Message);
            }
        }
        #endregion

        #region AddMessage
        private void AddMessage(string v)
        {
            if (v == "") return;

            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                if(ListEvents.Items.Count > 200)
                    ClearListBox(ListEvents);

                ListEvents.Items.Add(DateTime.Now.ToString("HH:mm:ss") + " " + v);
            }));
        }

        private void AddMessage(TextBox tbx, string v)
        {
            if (v == "") return;

            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                tbx.Text = v;
            }));
        }

        private void AddMessage(string v, ListBox listBoxEvents)
        {
            if (v == "") return;

            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                if (listBoxEvents.Items.Count > 100)
                    listBoxEvents.Items.Clear();

                listBoxEvents.Items.Add(DateTime.Now.ToString("HH:mm:ss") + " " + v);
                if (!(listBoxEvents.Items.IsEmpty))
                {
                    listBoxEvents.SelectedIndex = listBoxEvents.Items.Count - 1;
                    listBoxEvents.ScrollIntoView(listBoxEvents.SelectedItem);
                }
            }));
        }
        #endregion

        #region ErrorMessage
        private void ErrorMessage(string v)
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                ListErrors.Items.Add(DateTime.Now.ToString("HH:mm:ss") + " " + v);
            }));
        }
        #endregion

        #region ButtonDelete_Click
        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!_mainservice)
                return;

            IList list = GridPallet.SelectedItems;
            SQLClass sql = new SQLClass("trace");
            string query;
            string line = TextBoxCurrentLine.Text.Replace("Line-","Trace_");

            if (list.Count == 0) return;

            foreach (var item in list)
            {
               string s = ((DataRowView)item).Row.ItemArray[0].ToString();
               query = string.Format("DELETE FROM {0} WHERE pallet = '{1}'", line, s);

               sql.Delete(query);

                FlexLine f = FindInCollection(TextBoxCurrentLine.Text);
                if (f.PalletDictionary.ContainsKey(s))
                    f.PalletDictionary.Remove(s);
            }

            query = "SELECT distinct pallet FROM [" + line + "] order by pallet";

            DataTable dt = sql.SelectDB(query, out string result);

            if (result != null)
                ErrorOut("At ButtonDelete_Click: " +  result);

            GridPallet.ItemsSource = null;
            GridPallet.ItemsSource = dt.AsDataView();
        }
        #endregion

        #region ButStop_Click
        private void ButStop_Click(object sender, RoutedEventArgs e)
        {
            UnsubscribeTraceability();

            if (Level == 4 && _mainservice)
            {
                ClearTraceLines();
            }

            Close();
        }
        #endregion

        #region ButClean_Click
        private void ButClean_Click(object sender, RoutedEventArgs e)
        {
            LstMsgBox.Items.Clear();
            GridRecipe.ItemsSource = null;
            GridTrace.ItemsSource = null;
            GridPallet.ItemsSource = null;
            ListErrors.Items.Clear();
            ListEvents.Items.Clear();
            CheckPallets();
            //Cleaning();
        }
        #endregion

        #region CheckIfDBEmpty
        public void CheckIfDBEmpty(string line)
        {
            string s = line.Replace("Line-", "Trace_");

            SQLClass sql = new SQLClass("trace");
            string query = string.Format("SELECT distinct pallet FROM [{0}] order by pallet", s);

            DataTable d = sql.SelectDB(query, out string result);

            if (result != null)
            {
                ErrorOut("At CheckIfDBEmpty: " + result);
                return ;
            }
            try
            {
                int index = LineCollection.FindIndex(a => a.Name == line);

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
            catch(Exception ex)
            {
                ErrorOut("At CheckIfDBEmpty: " + ex.Message);
            }
        }
        #endregion

        #region Button_Login_Click
        private void Button_Login_Click(object sender, RoutedEventArgs e)
        {
            if (button_Login.Content == null || button_Login.Content.ToString() == "")
            {
                Login log = new Login()
                {
                    Owner = this
                };

                bool? result = log.ShowDialog();

                if (result ?? false)
                {
                    SQLClass sql = new SQLClass("login");
                    string query = string.Format("SELECT * FROM [AOI].[dbo].[employers] WHERE work_number='{0}'", User);

                    DataTable d = sql.SelectDB(query, out string res);
                    if (res != null)
                        ErrorOut("At Login: " + res);

                    if (d.Rows.Count > 0)
                    {
                        string psw = d.Rows[0]["password"].ToString().Trim();
                        if (psw == Password)
                        {
                            button_Login.Content = d.Rows[0]["name"].ToString().Trim();
                            button_Login.Background = Brushes.White;
                            User = d.Rows[0]["work_number"].ToString().Trim();
                            Password = psw;

                            int lvl = (int)d.Rows[0]["lvl"];

                            if (lvl == 4)
                            {
                                Level = 4;
                                TabSettings.Visibility = Visibility.Visible;
                                butStart.Visibility = Visibility.Visible;
                                TrustedJob();
                            }
                            else if(lvl == 3)
                            {
                                Level = 3;
                                TabSettings.Visibility = Visibility.Visible;
                                butStart.Visibility = Visibility.Collapsed;
                            }
                            else if(lvl == 2)
                            {
                                Level = 2;
                                TabSettings.Visibility = Visibility.Collapsed;
                                butStart.Visibility = Visibility.Collapsed;

                            }
                            else
                            {
                                Level  = 1;
                                TabSettings.Visibility = Visibility.Collapsed;
                                butStart.Visibility = Visibility.Collapsed;

                            }
                        }
                    }
                }
            }
            else
            {
                User = "";
                Password = "";
                button_Login.Background = (Brush)Application.Current.MainWindow.FindResource("buttonLogin2");
                button_Login.Content = null;

                TabSettings.Visibility = Visibility.Collapsed;
                butStart.Visibility = Visibility.Collapsed;
                TabSearch.Visibility = Visibility.Collapsed;
                Level = 1;
            }
        }
        #endregion

        #region RegisterPallet
        internal void RegisterPallet(string line, string pallet, string station, string board, string setup, string recipe, out bool last, out bool over)
        {
            last = false;
            over = false;

            try
            {
                FlexLine f = FindInCollection(line);

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
                int qty = Convert.ToInt32(ComboQty.Text);

                #region
                //Dictionary<int, List<string>> groups = f.PalletDictionary.GroupBy(x => x.Value)
                //   .ToDictionary(x => x.Key, x => x.Select(i => i.Key).ToList());


                //for (int i = 1; i < 5; i++)
                //{
                //    if (groups.ContainsKey(i) && groups[i].Count > qty)
                //        return true;
                //}
#endregion

                int k = 1;

                if (f.PalletDictionary.ContainsKey(pallet))
                {
                    k = f.PalletDictionary[pallet];
                    int count = 0;
                    for (int i = 0; i < f.PalletDictionary.Count; i++)
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
            //var dValue = from row in DTRecipes.AsEnumerable()
            //             where row.Field<string>("line").Trim() == line
            //             select row.Field<string>("receipe").Trim();

 //           if (dValue.First() != board)// && !RecipeDBisEmpty(line))
            if(RecipeDictionary[line] != recipe)
            {
                RecipeDictionary[line] = recipe;
                FillOneRecipe(new string[] { setup, recipe, line });
            }
        }

        private bool RecipeDBisEmpty(string line)
        {
            string nl = line.Replace("Line-", "Receipe_");
            DataTable d = null;

            string query = string.Format("SELECT 1 [pn] FROM [Traceability].[dbo].[{0}]", nl);

            SQLClass sql = new SQLClass("trace");

            d = sql.SelectDB(query, out string result);
            if (result != null)
                ErrorOut(result);
            return d.Rows.Count > 0;
        }

        #region PutToDictionary
        private void PutToDictionary(string line, string pallet)
        {
            if (!FifoDictionary.ContainsKey(line))
                FifoDictionary.Add(line, new List<string>());
            if (!FifoDictionary[line].Contains(pallet))
                FifoDictionary[line].Add(pallet);
        }
        #endregion

        #region PullFromDictionary
        private void PullFromDictionary(string line, string pallet, out bool delay)
        {
            delay = false;

            if (!FifoDictionary.ContainsKey(line))
                return;

            if (!FifoDictionary[line].Contains(pallet))
            {
                if(!FindInLost(pallet))
                    ErrorOut(line + " does not contain pallet " + pallet);
                return;
            }

            string s = FifoDictionary[line].ElementAt(0);
            FifoDictionary[line].RemoveAt(0);

            if (s != pallet)
            {
                FifoDictionary[line].Remove(pallet);

                if (CompareByPallet(s, line))
                {
                    ClearTraceLine(line, s);
                    return;
                }

                Lost lost = new Lost()
                {
                    line = line,
                    pallet = s,
                    time = DateTime.Now.ToUniversalTime()
                };

                LostPallets.Add(lost);
                delay = true;

                if (!_timerLost.Enabled)
                {
                    _timerLost.Enabled = true;
                    _timerLost.Start();
                }
            }
        }
        #endregion

        #region ClearTraceLine
        private void ClearTraceLine(string line, string pallet)
        {
            SQLClass sql = new SQLClass("trace");

            string query = string.Format("delete from [Traceability].[dbo].[{0}] where pallet = '{1}'", line.Replace("Line-", "Trace_"), pallet);
            sql.Update(query);
        }
        #endregion

        #region FindInLost
        private bool FindInLost(string pallet)
        {
            foreach (var item in LostPallets)
            {
                if (item.pallet == pallet)
                {
                    LostPallets.Remove(item);
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region _timerLost_Elapsed
        private void _timerLost_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Task task = Task.Run(() => LookForLost());
        }
        #endregion

        #region LookForLost
        private void LookForLost()
        {
            foreach (Lost item in LostPallets)
            {
                if( DateTime.Now.ToUniversalTime().Subtract(item.time).Seconds > 60 * Convert.ToInt32(ComboMin.Text))
                {
                    if (!CompareByPallet(item.pallet, item.line))
                        EmergencyStopMethod(item.line, null, null, "", "The sequence is broken. " + " Pallet: " + item.pallet, TraceAdam(AdamOrder));
                    else
                        ClearTraceLine(item.line, item.pallet);

                    LostPallets.Remove(item);
                }
            }

            if(LostPallets.Count == 0)
            {
                _timerLost.Stop();
                _timerLost.Enabled = false;
            }
        }
        #endregion

        #region CompareByPallet
        private bool CompareByPallet(string pallet, string line)
        {
            string board = GetBoard(line);

            DataTable d1 = GetDTFromRecipe(line);
            DataTable d2 = GetDTFromTrace(pallet, board);

            if (d2.Rows.Count == 0 || d1.Rows.Count == 0)
                return false;

            return GetDifferentRecords(d1, d2);
        }
        #endregion

        #region GetDTFromTrace
        private DataTable GetDTFromTrace(string pallet, string board)
        {
            SQLClass sql = new SQLClass("setup_trace");

            string query = string.Format(@"SELECT     TOP (100) PERCENT dbo.PackagingUnit.ComponentBarcode AS PN, dbo.RefDesignator.Name AS RefDes, dbo.PackagingUnit.PackagingUniqueId AS PUID, 
                       '0' as Stam1,'0' as Stam2, '0' as Stam3
FROM         dbo.Placement INNER JOIN
                      dbo.TracePlacement ON dbo.Placement.PlacementGroupId = dbo.TracePlacement.PlacementGroupId FULL OUTER JOIN
                      dbo.Recipe INNER JOIN
                      dbo.Job INNER JOIN
                      dbo.TraceData INNER JOIN
                      dbo.TraceJob ON dbo.TraceData.Id = dbo.TraceJob.TraceDataId ON dbo.Job.Id = dbo.TraceJob.JobId ON dbo.Recipe.id = dbo.Job.RecipeId FULL OUTER JOIN
                      dbo.PCBBarcode ON dbo.TraceData.PCBBarcodeId = dbo.PCBBarcode.Id ON dbo.TracePlacement.TraceDataId = dbo.TraceData.Id FULL OUTER JOIN
                      dbo.RefDesignator ON dbo.Placement.RefDesignatorId = dbo.RefDesignator.Id FULL OUTER JOIN
                      dbo.Charge ON dbo.Placement.ChargeId = dbo.Charge.Id FULL OUTER JOIN
                      dbo.PackagingUnit ON dbo.Charge.PackagingUnitId = dbo.PackagingUnit.Id
WHERE   (dbo.PCBBarcode.Barcode = N'{0}') and (dbo.Recipe.Name like N'%{1}')", pallet, board);

            DataTable d2 = sql.SelectDB(query, out string Result);
            if (Result != null)
                ErrorOut(Result);

            return d2;
        }
        #endregion

        #region GetBoard
        private string GetBoard(string line)
        {
            SQLClass sql = new SQLClass("trace");
            string query = string.Format(@"SELECT * FROM [Traceability].[dbo].[Current] WHERE line = '{0}'", line);

            DataTable d = sql.SelectDB(query, out string result);

            if (result != null)
                ErrorOut("At GetBoard: " + result);

            try
            {
                if (d.Rows.Count > 0)
                       return d.Rows[0]["receipe"].ToString().Trim();
            }
            catch (Exception ex)
            {
                ErrorOut("At GetBoard: " + ex.Message);
            }

            return "";
        }
        #endregion

        #region GetDifferentRecords
        private bool GetDifferentRecords(DataTable FirstDataTable, DataTable SecondDataTable)
        {
            //Create Empty Table  
            DataTable ResultDataTable = new DataTable("ResultDataTable");

            //use a Dataset to make use of a DataRelation object  
            using (DataSet ds = new DataSet())
            {
                //Add tables  
                ds.Tables.AddRange(new DataTable[] { FirstDataTable.Copy(), SecondDataTable.Copy() });

                //Get Columns for DataRelation  
                DataColumn[] firstColumns = new DataColumn[2];
                firstColumns[0] = ds.Tables[0].Columns[0];
                firstColumns[1] = ds.Tables[0].Columns[1];

                DataColumn[] secondColumns = new DataColumn[2];
                secondColumns[0] = ds.Tables[1].Columns[0];
                secondColumns[1] = ds.Tables[1].Columns[1];

                //Create DataRelation  
                DataRelation r1 = new DataRelation(string.Empty, firstColumns, secondColumns, false);
                ds.Relations.Add(r1);

                DataRelation r2 = new DataRelation(string.Empty, secondColumns, firstColumns, false);
                ds.Relations.Add(r2);

                //Create columns for return table  
                for (int i = 0; i < SecondDataTable.Columns.Count; i++)
                {
                    ResultDataTable.Columns.Add(SecondDataTable.Columns[i].ColumnName, SecondDataTable.Columns[i].DataType);
                }

                //If FirstDataTable Row not in SecondDataTable, Add to ResultDataTable.  
                ResultDataTable.BeginLoadData();
                foreach (DataRow parentrow in ds.Tables[0].Rows)
                {
                    DataRow[] childrows = parentrow.GetChildRows(r1);
                    if (childrows == null || childrows.Length == 0)
                        ResultDataTable.LoadDataRow(parentrow.ItemArray, true);
                }

                //If SecondDataTable Row not in FirstDataTable, Add to ResultDataTable.  
                foreach (DataRow parentrow in ds.Tables[1].Rows)
                {
                    DataRow[] childrows = parentrow.GetChildRows(r2);
                    if (childrows == null || childrows.Length == 0)
                        ResultDataTable.LoadDataRow(parentrow.ItemArray, true);
                }
                ResultDataTable.EndLoadData();
            }

            return ResultDataTable.Rows.Count == 0;
        }
        #endregion


        internal void FillRecipeList(string board, string line, string setup, string recipe)
        {
            //if(RecipeList.Contains(line))
            {
                //RecipeList.Remove(line);
                CheckRecipe(line, board, setup, recipe);
            }

            _filled = RecipeList.Count == 0;
            //Console.WriteLine(RecipeList.Count.ToString());
        }


        #region GetDTFromRecipe
        private DataTable GetDTFromRecipe(string line)
        {
            string nl = line.Replace("Line-", "Receipe_");
            DataTable d = null;

            string query = string.Format("SELECT [pn], [rf], [station], [loc], [track], [div] FROM [Traceability].[dbo].[{0}]", nl);

            SQLClass sql = new SQLClass("trace");

            d = sql.SelectDB(query, out string result);
            if (result != null)
                ErrorOut(result);
            return d;
        }
        #endregion
        #endregion

        #region RegisterPalletA
        internal void RegisterPalletA(string line, string pallet, string station)
        {
            try
            {
                FlexLine f = FindInCollection(line);
                int index = LineCollection.FindIndex(a => a.Name == line);
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
        #endregion

        #region AnimateBoard
        private void AnimateBoard(FlexLine f, string station, string pallet, string line)
        {
            if (TabLineManagement.IsSelected && line == textBoxCurrentLineLM.Text)
            {
                int i = f.StationDictionary[station];

                switch (i)
                {
                    case 1:
                        Task task = Task.Run(() => Move1());
                        //Move1();
                        break;
                    case 2:
                        Task task1 = Task.Run(() => Move2());
                        //Move2();
                        break;
                    case 3:
                        Task task2 = Task.Run(() => Move3());
                        //Move3();
                        break;
                    case 4:
                        Task task3 = Task.Run(() => Move4());
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
                Storyboard sb4 = (this.FindResource("MovingImage4") as Storyboard);
                Timeline.SetDesiredFrameRate(sb4, 30);
                //     sb4.Completed += delegate { MyImg4.Visibility = Visibility.Hidden; };
                sb4.Completed += Sb4_Completed;
                sb4.Begin();
            }));
        }

        private void Sb4_Completed(object sender, EventArgs e)
        {
            MyImg4.Opacity = 0;
            Storyboard sb = (this.FindResource("MovingImage14") as Storyboard);
            sb.Begin();
        }

        private void Move3()
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                MyImg3.Opacity = 1;
                MyImg3.Visibility = Visibility.Visible;
                Storyboard sb3 = (this.FindResource("MovingImage3") as Storyboard);
                //sb3.Completed += delegate { MyImg3.Visibility = Visibility.Hidden; };
                Timeline.SetDesiredFrameRate(sb3, 30);
                sb3.Completed += Sb3_Completed;
                sb3.Begin();
            }));
        }

        private void Sb3_Completed(object sender, EventArgs e)
        {
            MyImg3.Opacity = 0;
            Storyboard sb = (this.FindResource("MovingImage13") as Storyboard);
            sb.Begin();
        }

        private void Move2()
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                MyImg2.Opacity = 1;
                MyImg2.Visibility = Visibility.Visible;
                Storyboard sb2 = (this.FindResource("MovingImage2") as Storyboard);
                Timeline.SetDesiredFrameRate(sb2, 30);
                //sb2.Completed += delegate { MyImg2.Visibility = Visibility.Hidden; };
                sb2.Completed += Sb2_Completed;
                sb2.Begin();
            }));
        }

        private void Sb2_Completed(object sender, EventArgs e)
        {
            MyImg2.Opacity = 0;
            Storyboard sb = (this.FindResource("MovingImage12") as Storyboard);
            sb.Begin();
        }

        private void Move1()
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                MyImg1.Opacity = 1;
                MyImg1.Visibility = Visibility.Visible;
                Storyboard sb1 = (this.FindResource("MovingImage1") as Storyboard);
                Timeline.SetDesiredFrameRate(sb1, 30);
                //     sb1.Completed += delegate { MyImg1.Visibility = Visibility.Hidden; };
                sb1.Completed += Sb1_Completed;
                sb1.Begin();
            }));
        }

        private void Sb1_Completed(object sender, EventArgs e)
        {
            MyImg1.Opacity = 0;
            Storyboard sb = (this.FindResource("MovingImage11") as Storyboard);
            sb.Begin();
        }
        #endregion

        #region TrackPallet
        internal void TrackPallet(string line, string station, string pallet)
        {
            if(TabLineManagement.IsSelected && line == textBoxCurrentLineLM.Text)
            {
                try
                {
                    FlexLine f = FindInCollection(line);
                    ClearInputButtons();
                    AnimateBoard(f, station, pallet, line);

                    if (f.PalletDictionary.Count > 0)
                    {
                        for (int i = 0; i < f.PalletDictionary.Count; i++)
                        {
                            if (i >= f.PalletDictionary.Count)
                            {
                                ErrorOut("PalletDictionary smaller than it has to be");
                                continue;
                            }
                            TextBlock b = FlashTextBlocs[f.PalletDictionary.ElementAt(i).Value];
                            b.Background = SystemColors.GradientActiveCaptionBrush;
                            string tt = f.PalletDictionary.ElementAt(i).Key;
                            ComboList[f.PalletDictionary[tt]].Items.Add(tt);
                        }

                        for (int j = 0; j < ComboList.Length; j++)
                        {
                            int k = ComboList[j].Items.Count;
                            if (k != 0)
                            {
                                TextBlockList[j].Text = k.ToString();
                                var flashButton = FindResource("FlashButton" + (j + 1).ToString()) as Storyboard;
                                flashButton.Begin();
                            }
                        }
                    }
                }
                catch(Exception ex)
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

                    if (f.PalletDictionary.Count > 0)// && CheckIfDBEmpty(f.Name))
                    {
                        for (int i = 0; i < f.PalletDictionary.Count; i++)
                        {
                            TextBlock b = FlashTextBlocs[f.PalletDictionary.ElementAt(i).Value];
                            b.Background = SystemColors.GradientActiveCaptionBrush;
                            string tt = f.PalletDictionary.ElementAt(i).Key;
                            ComboList[f.PalletDictionary[tt]].Items.Add(tt);
                        }

                        for (int j = 0; j < ComboList.Length; j++)
                        {
                            int k = ComboList[j].Items.Count;
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
            string s = line.Replace("Line-", "Trace_");

            SQLClass sql = new SQLClass("trace");
            string query = string.Format("SELECT distinct pallet FROM [{0}] order by pallet", s);

            DataTable d = sql.SelectDB(query, out string result);

            if (result != null)
            {
                ErrorOut("At CheckIfDBEmpty: " + result);
                return false;
            }

            return d.Rows.Count > 0;
        }
        #endregion

        #region ClearInputButtons
        private void ClearInputButtons()
        {
            for (int i = 0; i < FlashTextBlocs.Length; i++)
            {
                FlashTextBlocs[i].Background = null;
                FlashTextBlocs[i].ToolTip = null;
                ComboList[i].Items.Clear();
                TextBlockList[i].Text = "0";
                var flashButton = FindResource("FlashButton" + (i + 1).ToString()) as Storyboard;
                flashButton.Stop();
            }
        }
        #endregion

        #region Radio_Checked
        private void RadioAction_Checked(object sender, RoutedEventArgs e)
        {
            if(textBoxCurrentLineLM.Text == "")
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
                GetLineState(textBoxCurrentLineLM.Text);
        }

        private void RadioInfo_Checked(object sender, RoutedEventArgs e)
        {
            if (buttonLineStop != null)
                buttonLineStop.Content = "Line Info";
        }

        private void RadioMessage_Checked(object sender, RoutedEventArgs e)
        {
            if (Level < 3)
            {
                RadioInfo.IsChecked = true;
                MessageBox.Show("You have not permissions to change line state!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
                buttonLineStop.Content = "Line Control";
        }
        #endregion

        #region GetLineState
        private void GetLineState(string text)
        {
            SQLClass sql = new SQLClass("trace");

            string query = string.Format("SELECT Monitor FROM [Traceability].[dbo].[Lines] WHERE Line = '{0}'", text);

            DataTable d = sql.SelectDB(query, out string result);
            if (result != null)
                ErrorOut("At GetLineState: " + result);

            if(d.Rows.Count > 0)
                if((bool)d.Rows[0]["Monitor"] == true)
                    buttonLineStop.Content = "Stop Line";
            else
                    buttonLineStop.Content = "Start Line";
        }
        #endregion

        #region GetLeaf
        public static string GetLeaf(object strWithBackSlash)
        {
            if (strWithBackSlash == null) return "";

            string[] arr = strWithBackSlash.ToString().Split('\\');
            return arr[arr.Length - 1];
        }
        #endregion
 
        #region MenuItem_Click
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

            string pth = _dir + "\\" + box.Name + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";

            using (StreamWriter sw = new StreamWriter(pth))
            {
                foreach (var item in box.Items)
                {
                    sw.WriteLine(item.ToString());
                }
            }

            box.Items.Clear();
        }
        #endregion

        #region ButtonRefresh_Click
        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            GetSettings();
        }
        #endregion

        #region Cleaning
        public void Cleaning()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        #endregion

        private void ButLines_Click(object sender, RoutedEventArgs e)
        {
            string query = @"SELECT TOP (100) PERCENT StationPath, MachineId
                FROM dbo.Station
                WHERE (LastModifiedDate > CONVERT(DATETIME, '2017-07-01 00:00:00', 102))
                ORDER BY StationPath";

            SQLClass sql = new SQLClass("setup");

            DataTable d = sql.SelectDB(query, out string result);

            if (result != null)
                ErrorOut("At Button Lines: " + result);

            Lines ln = new Lines(d);
            ln.ShowDialog();
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
            catch (Exception ex)
            {

            }
        }
    }
}
