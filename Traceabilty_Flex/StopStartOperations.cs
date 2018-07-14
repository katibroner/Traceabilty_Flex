using Asm.As.Oib.SiplacePro.LineControl.Contracts;
using Asm.As.Oib.SiplacePro.LineControl.Proxy.Business.Objects;
using System;
using System.Windows;
using System.Windows.Media;
using TraceabilityTestGui;
using Asm.As.Oib.SiplacePro.LineControl.Contracts.Data;
using System.Data;
using System.Windows.Controls;

namespace Traceabilty_Flex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region ButtonsInput
        private void ButtonInput1_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxCurrentLineLM.Text == "") return;

            if ((bool)RadioInfo.IsChecked || (bool)RadioMessage.IsChecked)
                return;

            string s = GetEndPointLine(textBoxCurrentLineLM.Text);

            LineControl _LineControlProxy = InitializeLineControlPoxy(s);

            string stationPath = string.Format(prefix, textBoxCurrentLineLM.Text, textStation1.Text);

            #region DOC_BlockStationInputConveyor
            if ((string)buttonInput11.Content == "Open")
            {
                try
                {
                    if (_LineControlProxy.BlockStationInputConveyor(stationPath, true, progName))
                    {
                        buttonInput11.Content = "Blocked";
                        buttonInput11.Background = Brushes.Red;
                    }
                }
                catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }
            }
            #endregion

            #region DOC_UnBlockStationInputConveyor
            else
            {
                try
                {
                    if (_LineControlProxy.UnBlockStationInputConveyor(stationPath, progName))
                    {
                        buttonInput11.Content = "Open";
                        buttonInput11.Background = null;
                    }
                }
                catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

            }
            #endregion

            ReleaseLineControlProxy(_LineControlProxy);
        }

        private void ButtonInput2_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxCurrentLineLM.Text == "") return;

            if ((bool)RadioInfo.IsChecked || (bool)RadioMessage.IsChecked)
                return;

            string s = GetEndPointLine(textBoxCurrentLineLM.Text);

            LineControl _LineControlProxy = InitializeLineControlPoxy(s);

            string stationPath = string.Format(prefix, textBoxCurrentLineLM.Text, textStation2.Text);

            #region DOC_BlockStationInputConveyor
            if ((string)buttonInput12.Content == "Open")
            {
                try
                {
                    if (_LineControlProxy.BlockStationInputConveyor(stationPath, true, progName))
                    {
                        buttonInput12.Content = "Blocked";
                        buttonInput12.Background = Brushes.Red;
                    }
                }
                catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

            }
            #endregion

            #region DOC_UnBlockStationInputConveyor
            else
            {
                try
                {
                    if (_LineControlProxy.UnBlockStationInputConveyor(stationPath, progName))
                    {
                        buttonInput12.Content = "Open";
                        buttonInput12.Background = null;
                    }
                }
                catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

            }
            #endregion

            ReleaseLineControlProxy(_LineControlProxy);
        }

        private void ButtonInput3_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxCurrentLineLM.Text == "") return;

            if ((bool)RadioInfo.IsChecked || (bool)RadioMessage.IsChecked)
                return;

            string s = GetEndPointLine(textBoxCurrentLineLM.Text);

            LineControl _LineControlProxy = InitializeLineControlPoxy(s);

            string stationPath = string.Format(prefix, textBoxCurrentLineLM.Text, textStation3.Text);

            #region DOC_BlockStationInputConveyor
            if ((string)buttonInput13.Content == "Open")
            {
                try
                {
                    if (_LineControlProxy.BlockStationInputConveyor(stationPath, true, progName))
                    {
                        buttonInput13.Content = "Blocked";
                        buttonInput13.Background = Brushes.Red;
                    }
                }
                catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

            }
            #endregion

            #region DOC_UnBlockStationInputConveyor
            else
            {
                try
                {
                    if (_LineControlProxy.UnBlockStationInputConveyor(stationPath, progName))
                    {
                        buttonInput13.Content = "Open";
                        buttonInput13.Background = null;
                    }
                }
                catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

            }
            #endregion

            ReleaseLineControlProxy(_LineControlProxy);
        }

        private void ButtonInput4_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxCurrentLineLM.Text == "") return;

            if ((bool)RadioInfo.IsChecked || (bool)RadioMessage.IsChecked)
                return;

            string s = GetEndPointLine(textBoxCurrentLineLM.Text);

            LineControl _LineControlProxy = InitializeLineControlPoxy(s);

            string stationPath = string.Format(prefix, textBoxCurrentLineLM.Text, textStation4.Text);

            #region DOC_BlockStationInputConveyor
            if ((string)buttonInput14.Content == "Open")
            {
                try
                {
                    if (_LineControlProxy.BlockStationInputConveyor(stationPath, true, progName))
                    {
                        buttonInput14.Content = "Blocked";
                        buttonInput14.Background = Brushes.Red;
                    }
                }
                catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

            }
            #endregion

            #region DOC_UnBlockStationInputConveyor
            else
            {
                try
                {
                    if (_LineControlProxy.UnBlockStationInputConveyor(stationPath, progName))
                    {
                        buttonInput14.Content = "Input 4";
                        buttonInput14.Background = null;
                    }
                }
                catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

            }
            #endregion

            ReleaseLineControlProxy(_LineControlProxy);
        }

        internal bool CheckStation(string line, string station)
        {
            FlexLine f = FindInCollection(line);
            
            return Array.IndexOf( f.Used,station) != -1;
        }
        #endregion

        #region GetEndPointLine
        private string GetEndPointLine(string text)
        {
            string s = text.Replace("Line-", "smt-").ToLower();
            for (int i = 0; i < _LineControlEndpointAddress.Length; i++)
            {
                if (_LineControlEndpointAddress[i].Contains(s))
                    return _LineControlEndpointAddress[i];
            }
            return "";
        }
        #endregion

        #region GetStationPath
        private string GetStationPath(int i)
        {
            FlexLine line = FindInCollection(textBoxCurrentLineLM.Text);
            string st =  line.Stations[i];
            string s = "System\\" + line + "\\" + st;

            return s;
        }
        #endregion

        #region ButtonsStation
        private void ButtonStation1_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxCurrentLineLM.Text == "") return;

            string s = GetEndPointLine(textBoxCurrentLineLM.Text);

            LineControl _LineControlProxy = InitializeLineControlPoxy(s);

            string stationPath = string.Format(prefix, textBoxCurrentLineLM.Text, textStation1.Text);

            if ((bool)RadioAction.IsChecked)
            {
                #region DOC_BlockStationConveyor
                if (buttonStation1.BorderBrush == null)
                {
                    try {
                        if (_LineControlProxy.IsStationConveyorBlockSupported(stationPath))
                        {
                            if (_LineControlProxy.BlockStationConveyor(stationPath, ConveyorLanes.Right, progName))
                            {
                                buttonStation1.BorderBrush = Brushes.Red;
                            }
                        }
                        else
                        {
                            string his = string.Format("Station {0} does not support conveyor blocking.", stationPath);
                            MsgHistory(his);
                        }
                    }
                    catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

                }
                #endregion

                #region DOC_UnblockStationConveyor
                else
                {
                    try { 
                    if (_LineControlProxy.UnblockStationConveyor(stationPath, ConveyorLanes.Right, progName))
                    {
                        buttonStation1.BorderBrush = null;
                    }
                    }
                    catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

                }
                #endregion
            }
            else if((bool)RadioMessage.IsChecked )
            {
                DisplayMessenger dm = new DisplayMessenger("System\\" + textBoxCurrentLineLM.Text, stationPath, _client);
                dm.ShowDialog();
            }
            else
            {
                LineControlLineStatus lineStatus = _LineControlProxy.GetLineStatus("System\\" + textBoxCurrentLineLM.Text);
                LineControlStationStatus stationStatus = lineStatus.LineControlStationStati[0];
                PrintStation(stationStatus);
            }

            ReleaseLineControlProxy(_LineControlProxy);
        }

        internal bool TraceAdam(CheckBox check)
        {
            bool flag = false;
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(delegate ()
                {
                    if ((bool)check.IsChecked)
                        flag = true;
                }));
            }
            else
                return (bool)check.IsChecked;
            return flag;
        }

        internal bool CheckBoxAdam(CheckBox AdamSetup)
        {
            bool flag = false;
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                if ((bool)AdamSetup.IsChecked)
                    flag = true;
            }));
            return flag;
        }

        private void ButtonStation2_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxCurrentLineLM.Text == "") return;

            try
            {

                string s = GetEndPointLine(textBoxCurrentLineLM.Text);

                LineControl _LineControlProxy = InitializeLineControlPoxy(s);

                if (_LineControlProxy == null) return;

                string stationPath = string.Format(prefix, textBoxCurrentLineLM.Text, textStation2.Text);

                if ((bool)RadioAction.IsChecked)
                {
                    #region DOC_BlockStationConveyor
                    if (buttonStation2.BorderBrush == null)
                    {
                        try
                        {
                            if (_LineControlProxy.IsStationConveyorBlockSupported(stationPath))
                            {
                                if (_LineControlProxy.BlockStationConveyor(stationPath, ConveyorLanes.Right, progName))
                                {
                                    buttonStation2.BorderBrush = Brushes.Red;
                                }
                            }
                            else
                            {
                                string his = string.Format("Station {0} does not support conveyor blocking.", stationPath);
                                MsgHistory(his);
                            }
                        }
                        catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

                    }
                    #endregion

                    #region DOC_UnblockStationConveyor
                    else
                    {
                        try
                        {
                            if (_LineControlProxy.UnblockStationConveyor(stationPath, ConveyorLanes.Right, progName))
                            {
                                buttonStation2.BorderBrush = null;
                            }
                        }
                        catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

                    }
                    #endregion
                }
                else if ((bool)RadioMessage.IsChecked)
                {
                    DisplayMessenger dm = new DisplayMessenger("System\\" + textBoxCurrentLineLM.Text, stationPath, _client);
                    dm.ShowDialog();
                }
                else
                {
                    LineControlLineStatus lineStatus = _LineControlProxy.GetLineStatus("System\\" + textBoxCurrentLineLM.Text);
                    LineControlStationStatus stationStatus = lineStatus.LineControlStationStati[1];
                    PrintStation(stationStatus);
                }

                ReleaseLineControlProxy(_LineControlProxy);
            }
            catch(Exception ex)
            {
                ErrorOut(ex.Message);
            }
        }

        private void ButtonStation3_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxCurrentLineLM.Text == "") return;

            string s = GetEndPointLine(textBoxCurrentLineLM.Text);

            LineControl _LineControlProxy = InitializeLineControlPoxy(s);

            string stationPath = string.Format(prefix, textBoxCurrentLineLM.Text, textStation3.Text);

            if ((bool)RadioAction.IsChecked)
            {
                #region DOC_BlockStationConveyor
                if (buttonStation3.BorderBrush == null)
                {
                    try
                    {
                        if (_LineControlProxy.IsStationConveyorBlockSupported(stationPath))
                        {
                            if (_LineControlProxy.BlockStationConveyor(stationPath, ConveyorLanes.Right, progName))
                            {
                                buttonStation3.BorderBrush = Brushes.Red;
                            }
                        }
                        else
                        {
                            string his = string.Format("Station {0} does not support conveyor blocking.", stationPath);
                            MsgHistory(his);

                        }
                    }
                    catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

                }
                #endregion

                #region DOC_UnblockStationConveyor
                else
                {
                    try
                    {
                        if (_LineControlProxy.UnblockStationConveyor(stationPath, ConveyorLanes.Right, progName))
                        {
                            buttonStation3.BorderBrush = null;
                        }
                    }
                    catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

                }
                #endregion
            }
            else if ((bool)RadioMessage.IsChecked)
            {
                DisplayMessenger dm = new DisplayMessenger("System\\" + textBoxCurrentLineLM.Text, stationPath, _client);
                dm.ShowDialog();
            }
            else
            {
                if (_LineControlProxy != null)
                {
                    LineControlLineStatus lineStatus = _LineControlProxy.GetLineStatus("System\\" + textBoxCurrentLineLM.Text);
                    LineControlStationStatus stationStatus = lineStatus.LineControlStationStati[2];
                    PrintStation(stationStatus);
                }
            }

            ReleaseLineControlProxy(_LineControlProxy);
        }

        private void ButtonStation4_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxCurrentLineLM.Text == "") return;

            string s = GetEndPointLine(textBoxCurrentLineLM.Text);

            LineControl _LineControlProxy = InitializeLineControlPoxy(s);

            string stationPath = string.Format(prefix, textBoxCurrentLineLM.Text, textStation4.Text);

            if ((bool)RadioAction.IsChecked)
            {
                #region DOC_BlockStationConveyor
                if (buttonStation4.BorderBrush == null)
                {
                    try
                    {
                        if (_LineControlProxy.BlockStationConveyor(stationPath, ConveyorLanes.Right, progName))
                        {
                            buttonStation4.BorderBrush = Brushes.Red;
                        }
                    }
                    catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

                }
                #endregion

                #region DOC_UnblockStationConveyor
                else
                {
                    try
                    {
                        if (_LineControlProxy.IsStationConveyorBlockSupported(stationPath))
                        {
                            if (_LineControlProxy.UnblockStationConveyor(stationPath, ConveyorLanes.Right, progName))
                            {
                                buttonStation4.BorderBrush = null;
                            }
                        }
                        else
                        {
                            string his = string.Format("Station {0} does not support conveyor blocking.", stationPath);
                            MsgHistory(his);
                        }
                    }
                    catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

                }
                #endregion
            }
            else if ((bool)RadioMessage.IsChecked)
            {
                DisplayMessenger dm = new DisplayMessenger("System\\" + textBoxCurrentLineLM.Text, stationPath, _client);
                dm.ShowDialog();
            }
            else
            {
                LineControlLineStatus lineStatus = _LineControlProxy.GetLineStatus("System\\" + textBoxCurrentLineLM.Text);
                LineControlStationStatus stationStatus = lineStatus.LineControlStationStati[3];
                PrintStation(stationStatus);
            }

            ReleaseLineControlProxy(_LineControlProxy);
        }
        #endregion

        #region PrintStation
        private void PrintStation(LineControlStationStatus stationStatus)
        {
            MsgClear();

            MsgOut("STATION:                  \t" + stationStatus.Name);
            MsgOut("  HostName:            \t" + stationStatus.HostName);
            MsgOut("  HostIP:                    \t" + stationStatus.HostIP);
            MsgOut("  ConnectionState:     \t" + stationStatus.ConnectionState);
            MsgOut("  SetupName:           \t" + stationStatus.SetupName);
            MsgOut("  SoftwareVersion:     \t" + stationStatus.SoftwareVersion);
            MsgOut("-------------------------------------------------------------");
            MsgOut("RIGHT conveyor (1):");
            MsgOut("  BoardName:           \t" + stationStatus.RightConveyorStatus.BoardName);
            MsgOut("  BoardSide:              \t" + stationStatus.RightConveyorStatus.BoardSide);
            MsgOut("  ProcessState:        \t" + stationStatus.RightConveyorStatus.ProcessState);
            MsgOut("  RecipeName:          \t" + stationStatus.RightConveyorStatus.RecipeName);
        }
        #endregion
        
        #region Messages (message view)

        private void MsgClear()
        {
            ListboxStationStatus.Items.Clear();
        }

        public void MsgOut(string message)
        {

            if (!Dispatcher.CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread
            {
                Dispatcher.BeginInvoke(new Action(delegate ()
                {
                    ListboxStationStatus.Items.Add(message);
                    ListboxStationStatus.SelectedIndex = ListboxStationStatus.Items.Count - 1;
                }));
            }
            else
            {
                ListboxStationStatus.Items.Add(message);

                // Scroll to the last entry
                ListboxStationStatus.SelectedIndex = ListboxStationStatus.Items.Count - 1;
            }
        }

        public void MsgHistory(string message)
        {

            if (!Dispatcher.CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread
            {
                Dispatcher.BeginInvoke(new Action(delegate ()
                {
                    ListboxHitory.Items.Add(message);
                    ListboxHitory.SelectedIndex = ListboxHitory.Items.Count - 1;
                }));
            }
            else
            {
                ListboxHitory.Items.Add(message);

                // Scroll to the last entry
                ListboxHitory.SelectedIndex = ListboxHitory.Items.Count - 1;
            }
        }

        #endregion

        #region ButtonLine_Click
        private void ButtonLine_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxCurrentLineLM.Text == "") return;

            string s = GetEndPointLine(textBoxCurrentLineLM.Text);
            string line = textBoxCurrentLineLM.Text;

            LineControl _LineControlProxy = InitializeLineControlPoxy(s);

            if (!(bool)RadioInfo.IsChecked)
            {
                if((bool)RadioAction.IsChecked)
                {
                    if ((string)buttonLineStop.Content == "Stop Line")
                    {
                        StopAdamLine(line, "Stopped by Supervisor");
                        buttonLineStop.Content = "Start Line";
                    }
                    else if((string)buttonLineStop.Content == "Start Line")
                    {
                        StartAdamLine(line);
                        buttonLineStop.Content = "Stop Line";
                    }

                    //#region DOC_STOP_LINE;
                    //try { 
                    //if (_LineControlProxy.StopLine("System\\" + textBoxCurrentLine.Text))
                    //{
                    //    buttonLineStop.Content = "Start Line";
                    //    buttonLineStop.Background = Brushes.Red;
                    //}
                    //}
                    //catch (System.ServiceModel.FaultException ex) { MessageBox.Show(ex.Message); }

                        //#endregion
                }
                else if ((bool)RadioMessage.IsChecked)
                {
                    DisplayMessenger dm = new DisplayMessenger("System\\" + textBoxCurrentLineLM.Text, "", _client);
                    dm.ShowDialog();
                }
                else
                {
                    #region DOC_CONTINUE_LINE
                    try { 
                    if (_LineControlProxy.ContinueLine("System\\" + textBoxCurrentLineLM.Text))
                    {
                        buttonLineStop.Content = "Stop Line";
                        buttonLineStop.Background = null;
                    }
                    }
                    catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

                    #endregion
                }
            }
            else
            {
                #region DOC_LINE_STATUS
                if (_LineControlProxy == null)
                    return;

                LineControlLineStatus lineStatus = _LineControlProxy.GetLineStatus("System\\" + textBoxCurrentLineLM.Text);

                MsgClear();

                MsgOut("LINE Status");
                MsgOut("  Line:                    \t" + lineStatus.Line);
                MsgOut("  LineControlServer:       \t" + lineStatus.LineControlServerHostName);
                MsgOut("  ProductionSchedule:      \t" + lineStatus.ProductionSchedule);
                #endregion
            }

            ReleaseLineControlProxy(_LineControlProxy);
        }
        #endregion

        #region ChangeLineState
        private void ChangeLineState(int v, string line, string cause)
        {
            SQLClass sql = new SQLClass("trace");

            string query = string.Format("UPDATE [Traceability].[dbo].[Lines] SET Monitor = '{0}', Cause = '{2}' WHERE Line = '{1}'", v, line,cause);
            sql.Update(query);
        }
        #endregion

        #region StopAdamLine
        private void StopAdamLine(string line, string cause)
        {
            SQLClass sql = new SQLClass("trace");

            DataTable dtLineConfig = sql.SelectDB(string.Format("SELECT IP, Active FROM Lines WHERE Line = '{0}'", line), out string Result);

            if(dtLineConfig.Rows.Count > 0)
            {
                //try
                //{
                //    string b = dtLineConfig.Rows[0]["Active"].ToString();
                //    if (b == "0") return;
                //}
                //catch(Exception ex)
                //{
                //    ErrorOut("At StopAdam " + ex.Message);
                //}
               Result = Adam60xx.Send2Adam(line, dtLineConfig.Rows[0]["IP"].ToString(), 1, 0); // Stop conveyer, D1 normaly close

                if (Result != null)
                {
                    string Error = "PLC error: " + Result;
                    ErrorOut(Error);
                    Utils.WriteLog(Error);
                    Utils.SendMail(Utils.GetJoinedList("trace", "select eMail from [Users] where [Admin] = '20'", ';', out Result)     //GetSetting("MailCC")
                        , ""
                        , "Traceability Monitor Error"
                        , Error);
                }

                Result = Adam60xx.Send2Adam(line, dtLineConfig.Rows[0]["IP"].ToString(), 0, 1); // Stop conveyer, D1 normaly close

                if (Result != null)
                {
                    string Error = "PLC error: " + Result;
                    ErrorOut(Error);
                    Utils.WriteLog(Error);
                }

                ChangeLineState(0, line, cause);
            }
        }
        #endregion

        #region StartAdamLine
        private void StartAdamLine(string line)
        {
            SQLClass sql = new SQLClass("trace");

            DataTable dtLineConfig = sql.SelectDB(string.Format("SELECT IP FROM Lines WHERE Line = '{0}'", line), out string Result);

            if (dtLineConfig.Rows.Count > 0)
            {
                Result = Adam60xx.Send2Adam(line, dtLineConfig.Rows[0]["IP"].ToString(), 0, 0); // Stop conveyer, D1 normaly close

                if (Result != null)
                {
                    string Error = "PLC error: " + Result;
                    ErrorOut(Error);
                    Utils.WriteLog(Error);
                }

                Result = Adam60xx.Send2Adam(line, dtLineConfig.Rows[0]["IP"].ToString(), 1, 1); // Stop conveyer, D1 normaly close

                if (Result != null)
                {
                    string Error = "PLC error: " + Result;
                    ErrorOut(Error);
                    Utils.WriteLog(Error);
                }

                ChangeLineState(1, line, "");
            }
        }
        #endregion
    }
}