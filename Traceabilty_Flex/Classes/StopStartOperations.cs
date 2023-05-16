using Asm.As.Oib.SiplacePro.LineControl.Contracts;
using System;
using System.Windows;
using System.Windows.Media;
using Asm.As.Oib.SiplacePro.LineControl.Contracts.Data;

namespace Traceabilty_Flex
{

    public partial class MainWindow
    {
        #region ButtonsInput
        private void ButtonInput1_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxCurrentLineLm.Text == "") return;

            if (RadioMessage.IsChecked != null && (RadioInfo.IsChecked != null && ((bool)RadioInfo.IsChecked || (bool)RadioMessage.IsChecked)))
                return;

            var s = GetEndPointLine(TextBoxCurrentLineLm.Text);

            var lineControlProxy = InitializeLineControlPoxy(s);

            var stationPath = string.Format(prefix, TextBoxCurrentLineLm.Text, TextStation1.Text);

            #region DOC_BlockStationInputConveyor
            if ((string)ButtonInput11.Content == "Open")
            {
                try
                {
                    if (lineControlProxy.BlockStationInputConveyor(stationPath, true, progName))
                    {
                        ButtonInput11.Content = "Blocked";
                        ButtonInput11.Background = Brushes.Red;
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
                    if (lineControlProxy.UnBlockStationInputConveyor(stationPath, progName))
                    {
                        ButtonInput11.Content = "Open";
                        ButtonInput11.Background = null;
                    }
                }
                catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

            }
            #endregion

            ReleaseLineControlProxy(lineControlProxy);
        }

        private void ButtonInput2_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxCurrentLineLm.Text == "") return;

            if (RadioMessage.IsChecked != null && (RadioInfo.IsChecked != null && ((bool)RadioInfo.IsChecked || (bool)RadioMessage.IsChecked)))
                return;

            var s = GetEndPointLine(TextBoxCurrentLineLm.Text);

            var lineControlProxy = InitializeLineControlPoxy(s);

            var stationPath = string.Format(prefix, TextBoxCurrentLineLm.Text, TextStation2.Text);

            #region DOC_BlockStationInputConveyor
            if ((string)ButtonInput12.Content == "Open")
            {
                try
                {
                    if (lineControlProxy.BlockStationInputConveyor(stationPath, true, progName))
                    {
                        ButtonInput12.Content = "Blocked";
                        ButtonInput12.Background = Brushes.Red;
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
                    if (lineControlProxy.UnBlockStationInputConveyor(stationPath, progName))
                    {
                        ButtonInput12.Content = "Open";
                        ButtonInput12.Background = null;
                    }
                }
                catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

            }
            #endregion

            ReleaseLineControlProxy(lineControlProxy);
        }

        private void ButtonInput3_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxCurrentLineLm.Text == "") return;

            if (RadioMessage.IsChecked != null && (RadioInfo.IsChecked != null && ((bool)RadioInfo.IsChecked || (bool)RadioMessage.IsChecked)))
                return;

            var s = GetEndPointLine(TextBoxCurrentLineLm.Text);

            var lineControlProxy = InitializeLineControlPoxy(s);

            var stationPath = string.Format(prefix, TextBoxCurrentLineLm.Text, TextStation3.Text);

            #region DOC_BlockStationInputConveyor
            if ((string)ButtonInput13.Content == "Open")
            {
                try
                {
                    if (lineControlProxy.BlockStationInputConveyor(stationPath, true, progName))
                    {
                        ButtonInput13.Content = "Blocked";
                        ButtonInput13.Background = Brushes.Red;
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
                    if (lineControlProxy.UnBlockStationInputConveyor(stationPath, progName))
                    {
                        ButtonInput13.Content = "Open";
                        ButtonInput13.Background = null;
                    }
                }
                catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

            }
            #endregion

            ReleaseLineControlProxy(lineControlProxy);
        }

        private void ButtonInput4_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxCurrentLineLm.Text == "") return;

            if (RadioMessage.IsChecked != null && (RadioInfo.IsChecked != null && ((bool)RadioInfo.IsChecked || (bool)RadioMessage.IsChecked)))
                return;

            var s = GetEndPointLine(TextBoxCurrentLineLm.Text);

            var lineControlProxy = InitializeLineControlPoxy(s);

            var stationPath = string.Format(prefix, TextBoxCurrentLineLm.Text, TextStation4.Text);

            #region DOC_BlockStationInputConveyor
            if ((string)ButtonInput14.Content == "Open")
            {
                try
                {
                    if (lineControlProxy.BlockStationInputConveyor(stationPath, true, progName))
                    {
                        ButtonInput14.Content = "Blocked";
                        ButtonInput14.Background = Brushes.Red;
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
                    if (lineControlProxy.UnBlockStationInputConveyor(stationPath, progName))
                    {
                        ButtonInput14.Content = "Input 4";
                        ButtonInput14.Background = null;
                    }
                }
                catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

            }
            #endregion

            ReleaseLineControlProxy(lineControlProxy);
        }

        internal bool CheckStation(string line, string station)
        {
            var f = FindInCollection(line);

            return Array.IndexOf(f.Used, station) != -1;
        }
        #endregion

        #region GetEndPointLine
        private string GetEndPointLine(string text)
        {
            var s = text.Replace("Line-", "smt-").ToLower();
            for (var i = 0; i < _LineControlEndpointAddress.Length; i++)
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
            var line = FindInCollection(TextBoxCurrentLineLm.Text);
            var st = line.Stations[i];
            var s = "System\\" + line + "\\" + st;

            return s;
        }
        #endregion

        #region ButtonsStation
        private void ButtonStation1_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxCurrentLineLm.Text == "") return;

            var s = GetEndPointLine(TextBoxCurrentLineLm.Text);

            var lineControlProxy = InitializeLineControlPoxy(s);

            var stationPath = string.Format(prefix, TextBoxCurrentLineLm.Text, TextStation1.Text);

            if (RadioAction.IsChecked != null && (bool)RadioAction.IsChecked)
            {
                #region DOC_BlockStationConveyor
                if (ButtonStation1.BorderBrush == null)
                {
                    try
                    {
                        if (lineControlProxy.IsStationConveyorBlockSupported(stationPath))
                        {
                            if (lineControlProxy.BlockStationConveyor(stationPath, ConveyorLanes.Right, progName))
                            {
                                ButtonStation1.BorderBrush = Brushes.Red;
                            }
                        }
                        else
                        {
                            var his = $"Station {stationPath} does not support conveyor blocking.";
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
                        if (lineControlProxy.UnblockStationConveyor(stationPath, ConveyorLanes.Right, progName))
                        {
                            ButtonStation1.BorderBrush = null;
                        }
                    }
                    catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

                }
                #endregion
            }
            else if (RadioMessage.IsChecked != null && (bool)RadioMessage.IsChecked)
            {

            }
            else
            {
                var lineStatus = lineControlProxy.GetLineStatus("System\\" + TextBoxCurrentLineLm.Text);
                var stationStatus = lineStatus.LineControlStationStati[0];
                PrintStation(stationStatus);
            }

            ReleaseLineControlProxy(lineControlProxy);
        }

       

        private void ButtonStation2_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxCurrentLineLm.Text == "") return;

            try
            {

                var s = GetEndPointLine(TextBoxCurrentLineLm.Text);

                var lineControlProxy = InitializeLineControlPoxy(s);

                if (lineControlProxy == null) return;

                var stationPath = string.Format(prefix, TextBoxCurrentLineLm.Text, TextStation2.Text);

                if (RadioAction.IsChecked != null && (bool)RadioAction.IsChecked)
                {
                    #region DOC_BlockStationConveyor
                    if (ButtonStation2.BorderBrush == null)
                    {
                        try
                        {
                            if (lineControlProxy.IsStationConveyorBlockSupported(stationPath))
                            {
                                if (lineControlProxy.BlockStationConveyor(stationPath, ConveyorLanes.Right, progName))
                                {
                                    ButtonStation2.BorderBrush = Brushes.Red;
                                }
                            }
                            else
                            {
                                var his = $"Station {stationPath} does not support conveyor blocking.";
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
                            if (lineControlProxy.UnblockStationConveyor(stationPath, ConveyorLanes.Right, progName))
                            {
                                ButtonStation2.BorderBrush = null;
                            }
                        }
                        catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

                    }
                    #endregion
                }
                else if (RadioMessage.IsChecked != null && (bool)RadioMessage.IsChecked)
                {

                }
                else
                {
                    var lineStatus = lineControlProxy.GetLineStatus("System\\" + TextBoxCurrentLineLm.Text);
                    var stationStatus = lineStatus.LineControlStationStati[1];
                    PrintStation(stationStatus);
                }

                ReleaseLineControlProxy(lineControlProxy);
            }
            catch (Exception ex)
            {
                ErrorOut(ex.Message);
            }
        }

        private void ButtonStation3_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxCurrentLineLm.Text == "") return;

            var s = GetEndPointLine(TextBoxCurrentLineLm.Text);

            var lineControlProxy = InitializeLineControlPoxy(s);

            var stationPath = string.Format(prefix, TextBoxCurrentLineLm.Text, TextStation3.Text);

            if (RadioAction.IsChecked != null && (bool)RadioAction.IsChecked)
            {
                #region DOC_BlockStationConveyor
                if (ButtonStation3.BorderBrush == null)
                {
                    try
                    {
                        if (lineControlProxy.IsStationConveyorBlockSupported(stationPath))
                        {
                            if (lineControlProxy.BlockStationConveyor(stationPath, ConveyorLanes.Right, progName))
                            {
                                ButtonStation3.BorderBrush = Brushes.Red;
                            }
                        }
                        else
                        {
                            var his = $"Station {stationPath} does not support conveyor blocking.";
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
                        if (lineControlProxy.UnblockStationConveyor(stationPath, ConveyorLanes.Right, progName))
                        {
                            ButtonStation3.BorderBrush = null;
                        }
                    }
                    catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

                }
                #endregion
            }
            else if (RadioMessage.IsChecked != null && (bool)RadioMessage.IsChecked)
            {

            }
            else
            {
                if (lineControlProxy != null)
                {
                    var lineStatus = lineControlProxy.GetLineStatus("System\\" + TextBoxCurrentLineLm.Text);
                    var stationStatus = lineStatus.LineControlStationStati[2];
                    PrintStation(stationStatus);
                }
            }

            ReleaseLineControlProxy(lineControlProxy);
        }

        private void ButtonStation4_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxCurrentLineLm.Text == "") return;

            var s = GetEndPointLine(TextBoxCurrentLineLm.Text);

            var lineControlProxy = InitializeLineControlPoxy(s);

            var stationPath = string.Format(prefix, TextBoxCurrentLineLm.Text, TextStation4.Text);

            if (RadioAction.IsChecked != null && (bool)RadioAction.IsChecked)
            {
                #region DOC_BlockStationConveyor
                if (ButtonStation4.BorderBrush == null)
                {
                    try
                    {
                        if (lineControlProxy.BlockStationConveyor(stationPath, ConveyorLanes.Right, progName))
                        {
                            ButtonStation4.BorderBrush = Brushes.Red;
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
                        if (lineControlProxy.IsStationConveyorBlockSupported(stationPath))
                        {
                            if (lineControlProxy.UnblockStationConveyor(stationPath, ConveyorLanes.Right, progName))
                            {
                                ButtonStation4.BorderBrush = null;
                            }
                        }
                        else
                        {
                            var his = $"Station {stationPath} does not support conveyor blocking.";
                            MsgHistory(his);
                        }
                    }
                    catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

                }
                #endregion
            }
            else if (RadioMessage.IsChecked != null && (bool)RadioMessage.IsChecked)
            {

            }
            else
            {
                var lineStatus = lineControlProxy.GetLineStatus("System\\" + TextBoxCurrentLineLm.Text);
                var stationStatus = lineStatus.LineControlStationStati[3];
                PrintStation(stationStatus);
            }

            ReleaseLineControlProxy(lineControlProxy);
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

        private void MsgOut(string message)
        {

            if (!Dispatcher.CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread
            {
                Dispatcher.BeginInvoke(new Action(delegate
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

        private void MsgHistory(string message)
        {

            if (!Dispatcher.CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread
            {
                Dispatcher.BeginInvoke(new Action(delegate
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
            if (TextBoxCurrentLineLm.Text == "") return;

            var s = GetEndPointLine(TextBoxCurrentLineLm.Text);
            var line = TextBoxCurrentLineLm.Text;

            var lineControlProxy = InitializeLineControlPoxy(s);

            if (RadioInfo.IsChecked != null && !(bool)RadioInfo.IsChecked)
            {
                if (RadioAction.IsChecked != null && (bool)RadioAction.IsChecked)
                {
                    if ((string)ButtonLineStop.Content == "Stop Line")
                    {
                        StopAdamLine(line, "Stopped by Supervisor");
                        ButtonLineStop.Content = "Start Line";
                    }
                    else if ((string)ButtonLineStop.Content == "Start Line")
                    {
                        StartAdamLine(line);
                        ButtonLineStop.Content = "Stop Line";
                    }
                }
                else if (RadioMessage.IsChecked != null && (bool)RadioMessage.IsChecked)
                {
                 
                }
                else
                {
                    #region DOC_CONTINUE_LINE
                    try
                    {
                        if (lineControlProxy.ContinueLine("System\\" + TextBoxCurrentLineLm.Text))
                        {
                            ButtonLineStop.Content = "Stop Line";
                            ButtonLineStop.Background = null;
                        }
                    }
                    catch (System.ServiceModel.FaultException ex) { ErrorOut(ex.Message); }

                    #endregion
                }
            }
            else
            {
                #region DOC_LINE_STATUS
                if (lineControlProxy == null)
                    return;

                var lineStatus = lineControlProxy.GetLineStatus("System\\" + TextBoxCurrentLineLm.Text);

                MsgClear();

                MsgOut("LINE Status");
                MsgOut("  Line:                    \t" + lineStatus.Line);
                MsgOut("  LineControlServer:       \t" + lineStatus.LineControlServerHostName);
                MsgOut("  ProductionSchedule:      \t" + lineStatus.ProductionSchedule);
                #endregion
            }

            ReleaseLineControlProxy(lineControlProxy);
        }
        #endregion

        #region ChangeLineState
        private void ChangeLineState(int v, string line, string cause)
        {
            var sql = new SqlClass("trace");

            var query = string.Format("UPDATE Lines SET Adam = '{0}', dtLastCheck='{1}', Cause = '{2}' WHERE Line = '{3}'", v, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), cause, line );
            sql.Update(query);
        }
        #endregion

        #region StopAdamLine
        private void StopAdamLine(string line, string cause)
        {
            var sql = new SqlClass("trace");

            var dtLineConfig = sql.SelectDb($"SELECT IP FROM Lines WHERE Line = '{line}' AND Active=1", out var result);

            if (dtLineConfig.Rows.Count > 0)
            {
                result = Adam60Xx.Send2Adam(line, dtLineConfig.Rows[0]["IP"].ToString(), 1, 0); // Stop conveyer, D1 normaly close

                if (result != null)
                {
                    var error = "PLC error: " + result;
                    ErrorOut(error);
                    LogWriter.WriteLog(error);
                }

                result = Adam60Xx.Send2Adam(line, dtLineConfig.Rows[0]["IP"].ToString(), 0, 1); // Stop conveyer, D1 normaly close

                if (result != null)
                {
                    var error = "PLC error: " + result;
                    ErrorOut(error);
                    LogWriter.WriteLog(error);
                }

                ChangeLineState(0, line, cause);
            }
        }
        #endregion

        #region StartAdamLine
        private void StartAdamLine(string line)
        {
            var sql = new SqlClass("trace");

            var dtLineConfig = sql.SelectDb($"SELECT IP FROM Lines WHERE Line = '{line}'", out var result);

            if (dtLineConfig.Rows.Count > 0)
            {
                result = Adam60Xx.Send2Adam(line, dtLineConfig.Rows[0]["IP"].ToString(), 0, 0); // Stop conveyer, D1 normaly close

                if (result != null)
                {
                    var error = "PLC error: " + result;
                    ErrorOut(error);
                    LogWriter.WriteLog(error);
                }

                result = Adam60Xx.Send2Adam(line, dtLineConfig.Rows[0]["IP"].ToString(), 1, 1); // Stop conveyer, D1 normaly close

                if (result != null)
                {
                    var error = "PLC error: " + result;
                    ErrorOut(error);
                    LogWriter.WriteLog(error);
                }

                ChangeLineState(1, line, "");
            }
        }
        #endregion
    }
}