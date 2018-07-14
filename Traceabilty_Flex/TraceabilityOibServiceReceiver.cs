//-----------------------------------------------------------------------
// <copyright file="TraceabilityOibServiceReceiver.cs" company="ASM Assembly Systems GmbH & Co. KG">
//     Copyright (c) ASM Assembly Systems GmbH & Co. KG. All rights reserved.
// </copyright>
// <email>oib-support.siplace@asmpt.com</email>
// <summary>
//    This code is part of the OIB SDK. 
//    Use and redistribution is free without any warranty. 
// </summary>
//-----------------------------------------------------------------------

#region using

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.ServiceModel;
using www.siplace.com.OIB._2012._03.Traceability.Contracts.Data;
using www.siplace.com.OIB._2012._03.Traceability.Contracts.Service;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Traceabilty_Flex;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Net;


#endregion

namespace TraceabilityTestGui
{
    /// <summary>
    /// Receiver class for the Setup Center events
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class TraceabilityOibServiceReceiver : ITraceabilityDataDuplex
    {
        #region Fields
        private readonly MainWindow m_Form;
        private string endpoint = "http://mignt048:1405/Asm.As.Oib.WS.Eventing.Services/SubscriptionManager";


        public EndpointAddress _endpointAddress { get{ return new EndpointAddress(endpoint); } }
        public NetTcpBinding _binding { get; set; }
        #endregion

        
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="TraceabilityOibServiceReceiver"/> class.
        /// </summary>
        /// <param name="form">The form.</param>
        public TraceabilityOibServiceReceiver(MainWindow form)
        {
            m_Form = form;
           _binding = InitiallizeProxy();
        }

        private NetTcpBinding InitiallizeProxy()
        {
            NetTcpBinding binding = new NetTcpBinding();
            binding.Security.Mode = SecurityMode.None;
            binding.CloseTimeout = TimeSpan.FromMinutes(10);
            binding.OpenTimeout = TimeSpan.FromMinutes(10);
            binding.ReceiveTimeout = TimeSpan.FromMinutes(10);
            binding.ReliableSession.InactivityTimeout = binding.SendTimeout = TimeSpan.MaxValue;
            binding.ReliableSession.Enabled = true;
            binding.PortSharingEnabled = true;

            // Create the endpoint

            return binding;
        }
        #endregion // Constructor

        #region ITraceabilityDataDuplex Members

        public NewTraceabilityDataResponse NewTraceabilityData(TraceabilityDataRequest request)
        {
            NewTraceabilityDataResponse response = null;
            try
            {
                response = new NewTraceabilityDataResponse();
                TraceabilityData trcData = request.TraceabilityData;
                string board = string.Empty;
                string line = string.Empty;
                string station = string.Empty;
                string pallet = string.Empty;
                string setup = string.Empty;
                string recipe = string.Empty;
                string stationID = string.Empty;
                int panels = 0;

                if (m_Form != null)
                {
                    if (trcData != null)
                    {
                        if (trcData.Jobs != null)
                        {
                            line = MainWindow.GetLeaf(trcData.Line);
                            pallet = trcData.BoardID;
                            board = MainWindow.GetLeaf(trcData.Jobs[0].BoardName);
                            setup = MainWindow.GetLeaf(trcData.Jobs[0].Setup);
                            recipe = MainWindow.GetLeaf(trcData.Jobs[0].Recipe);
                            stationID = trcData.MachineID;
                            panels = trcData.Panels.Length;

                            if (!MainWindow.StatusDictionary[line])
                                return response;
                        }

                        station = trcData.Station != null ? MainWindow.GetLeaf(trcData.Station) : "";

                        if ((trcData.ErrorCodes != null) && (trcData.ErrorCodes.ErrorCodesList != null))
                        {
                            foreach (ErrorStruct error in trcData.ErrorCodes.ErrorCodesList)
                            {
                                if (error.ErrorReasons != null)
                                {
                                    foreach (ReasonStruct reason in error.ErrorReasons)
                                    {
                                        m_Form.ErrorOut(" ErrorLevel  = " + error.ErrorLevel + " " + line + "Program: " + board + "Station: " + station + "Pallet: " + pallet + "  ErrorReason = " + reason.Reason + " Source = " + reason.Source);
                                    }
                                }
                            }
                        }
                    }
                }
                
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                if (pallet.StartsWith("NO_PCB_BARCODE"))
                {
                    if (m_Form.CheckStation(line, station))
                    {
                        m_Form.ErrorOut(line + ", " + station + ", " + "NO_PCB_BARCODE");
                    }
                    
                    return response;
                }

                if (MainWindow._mainservice)
                {
                    int cnt = WriteTraceToDBLines(line, trcData, station);
                    TurnOnLightLine(line);

                    m_Form.RegisterPallet(line, pallet, station, board, setup, recipe, out bool last, out bool over);

            //        if(over)
              //          OverlimitStation(line, station);






                    if (last)
                    {
                        GetActiveLines();
                        Task task = Task.Run(() => CompareResults(line, pallet, board, setup,(bool)m_Form.DTActiveLines.Select("Line = '" + line + "'")[0]["Active"],
                            recipe, false,
                            m_Form._delay
                            ));
                    }

                    m_Form.TrackPallet(line, station, pallet);

                    //open only one of them
                    //Task task = Task.Run(() => SendStationToQMS(line, stationID, pallet, cnt, panels));
                }
                else
                {
                    ShowActivity(trcData, line, station, pallet, recipe);
                    m_Form.RegisterPalletA(line, pallet, station);
                    m_Form.TrackPallet(line, station, pallet);
                }
            }
            catch (Exception dumpRequestException)
            {
                m_Form.ExceptionOut("Exception during printing information in NewTraceabilityData", dumpRequestException, false);
            }

            return response;
        }

        /// //////////////////////////////////////////////////////////////////////////////////////////////////


        private void GetActiveLines()
        {
            SQLClass sql = new SQLClass("trace");
            string query = @"SELECT * FROM [Traceability].[dbo].[Lines]";

            m_Form.DTActiveLines = sql.SelectDB(query, out string Result);
            if (Result != null)
                m_Form.ErrorOut(Result);
        }


        private void SendStationToQMS(string line, string stationID, string pallet, int cnt, int panels)
        {
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            int board_qty = 1;
            int sub_event = 2;

            line = line.Remove(0, 5);
            //if (line != "E")
            //    return;
            string s =              "{\"line\":\"" + line + "\"," +
                                    "\"machine\":" + Convert.ToInt32( stationID) + "," +
                                    "\"sub_event\":" + Convert.ToInt32(sub_event) + "," +
                                    "\"pass_time\":\"" + time + "\"," +
                                    "\"boards_qty\":" + Convert.ToInt32(board_qty) + "," +
                                    "\"cards_qty\":" + Convert.ToInt32(panels) + "," +
                                    "\"comp_place\":" + Convert.ToInt32(cnt) + "," +
                                    "\"pallet_id\":\"" + pallet + "\"}";

           string json = json = @"{""data"": {""rows"":"
              + "[" + s + "]"
              + @"},""base"": {""flex_user_code"":""A014"",""password"":""$Flex2099"",""customer_code"":""0000"",""function_name"":""lms3_oib_insert_qty""}}";

            string address = "http://10.229.8.35/qms3/web_services/ws_json.php";
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(address);
            httpWebRequest.ContentType = "text/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
      //      using (StreamWriter streamWriter = new StreamWriter(@"C:\Tmp\qmstest6.txt", true))
            {
                streamWriter.Write(json);
            }

            try
            {
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var text = streamReader.ReadToEnd();
                    if (text.IndexOf("OK") == -1)
                    {
                        new LogWriter(json + "\n" + text,"QMS");
                   //     Utils.SendMail("error occurs", "error occurs while sending to qms\n" + DateTime.Now.ToLongTimeString());
                    }
                }
            }
            catch (Exception ex)
            {
                string lineeerror = MainWindow.GetAllFootprints(ex);
               MainWindow.WriteLog("SendToService\t" + lineeerror + " " + ex.Message);
            }
        }

        private void OverlimitStation(string line, string station)
        {
            m_Form.EmergencyStopMethod(line, null, null, "", "Exceeding the limit pallets after station: " + station, (bool)m_Form.AdamLimit.IsChecked);
        }
        #endregion

        #region ShowActivity
        private void ShowActivity(TraceabilityData trcData, string line, string station, string pallet, string board)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");

            m_Form.MessageOut(time 
                + "\tProgram:  " + board
                + "\tStation:  " + station
                + "\tPallet:  "  + pallet);

            if (board != MainWindow.RecipeDictionary[line])
            {
                m_Form.FillRecipeDT();
                DataTable d = GetRecipe(board, line);
                m_Form.SetFirstLastInLine(d, line);
            }
        }
        #endregion

        #region TurnOnColorLine
        private void TurnOnLightLine(string line)
        {
           int index= MainWindow.LineCollection.FindIndex(a => a.Name == line);
            MainWindow._buttons[index].Background = Brushes.Green;
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

        #region CompareResults
        private void CompareResults(string line, string pallet, string board, string setup, bool b, string recipe, bool flag, int delay)
        {
            Stopwatch st = new Stopwatch();
            st.Start();

            DataTable d1 = GetDTFromDBRecipe(line); 
            DataTable d2 = GetDTFromDBTrace(line, pallet);

            if (d1.Rows.Count == 0)
            {
                m_Form.FillOneRecipe(new string[] { setup, recipe, line });
                d1 = GetDTFromDBRecipe(line);
                if (d1.Rows.Count == 0)
                {
                    m_Form.ErrorOut(line + " Recipe is empty.");
                    return;
                }
            }
            if(d2.Rows.Count == 0)
            {
                m_Form.ErrorOut(line + " Trace is empty.");
                return;
            }
            DataTable d = null;
            Object thisLock = new Object();
            lock (thisLock)
            { 
                d = GetDifferentRecords(d1, d2);
            }
            string s = "";
            DataTable dRet = null;
            DataTable dDiff = null;

            int diff = d.Rows.Count;
            bool last_ch = false;


          //  PrintToFile_new(d2, pallet, line);


            if (diff > 0)
            {
              //  if (diff < 10)
               // {
                    foreach (DataRow item in d.Rows)
                    {
                        if (Array.IndexOf(MainWindow.PartsException, item[0].ToString().Trim()) != -1)
                            d.Rows.Remove(item);
                    }

                    if (d.Rows.Count == 0) return;
               // }

                if (!LastChance(pallet, recipe, d1, out dRet, out dDiff))
                {
                    m_Form.FillOneRecipe(new string[] { setup, recipe, line }); // this is for restarting the program
                    d1 = GetDTFromDBRecipe(line);

                    if (!flag)
                    {
                        string ti = DateTime.Now.ToString("HH:mm:ss");
                 
                        StartDelay(line, pallet, board, setup, b, recipe, delay);

                        m_Form.MessageOut(ti
                    + "   " + line + "\tDiff:\t0"
                    + "\t" + pallet
                    + "\tRecipe:\t" + d1.Rows.Count.ToString()
                    + "\tTrace:\t" + d2.Rows.Count.ToString()
                    + "\tTime:\t" + st.ElapsedMilliseconds + "\t"
                    + "Delayed");
                        m_Form.ErrorOut("Pallet " + pallet + " delayed");
                        return;
                    }

                    Task task2 = Task.Run(() => PrintToFile(d, pallet, line, d1, dRet));

                    List<string> lt = new List<string>();
                    string ms = "Pallet: " + pallet;

                    foreach (DataRow dr in d.Rows)
                        ms  = ms + Environment.NewLine + (dr[0].ToString().Trim() + "\t" + dr[1].ToString().Trim() + "\t" + dr[2].ToString().Trim() + "\tLocation: " + dr[3].ToString().Trim() + "\tFeeder: " + dr[4].ToString().Trim() + "\tTrack: " + dr[5].ToString().Trim());

                    lt.Add(ms);
                    s = GetMissedStations(d);
                    List<string[]> MissedList = GetMissedArray(line, pallet, d);
                    m_Form.EmergencyStopMethod(line, lt, MissedList, recipe, "Missed components at stations:" + Environment.NewLine + s, b);
               }
                else
                {
                    d2 = dRet;
                    diff = 0;
                    last_ch = true;
                }
            }

            st.Stop();

            string time = DateTime.Now.ToString("HH:mm:ss");
            s = last_ch ? "Rechecked" : (s == "" ? "" : ("  Missed stations: " + s));

            m_Form.MessageOut(time 
                + "   " + line + "\tDiff:\t" + diff.ToString() 
                + "\t" + pallet 
                + "\tRecipe:\t" + d1.Rows.Count.ToString() 
                + "\tTrace:\t"  + d2.Rows.Count.ToString() 
                + "\tTime:\t"   + st.ElapsedMilliseconds  + "\t"
                + s);

            ClearTraceLine(line, pallet);

            m_Form.CheckIfDBEmpty(line);
        }

        private void CompareResults_new(string line, string pallet, string board, string setup, bool b, string recipe, bool flag, int delay)
        {
            Stopwatch st = new Stopwatch();
            st.Start();

            SQLClass sql = new SQLClass("trace");
            string query = string.Format(@"SELECT  receipe.[pn]
      ,receipe.[rf] 
      ,receipe.station
      ,receipe.track
      ,receipe.div
      ,receipe.tower
      ,receipe.lvl
 from Traceability.dbo.{0} as receipe
 except
 select distinct trace.pn
 ,trace.rf
 ,trace.station
 ,trace.track
 ,trace.div
 ,trace.tower
 ,trace.lvl
  from [Traceability].[dbo].[{1}] as trace 
  where trace.pallet = '{2}'
 ", line.Replace("Line-", "Receipe_"), line.Replace("Line-", "Trace_"),pallet);

            DataTable d = sql.SelectDB(query, out string Result);

            int nTrace = GetNumberInTrace(line, pallet);
            int nRecipe = GetNumberInRecipe(line);

            string s = "";
            DataTable dDiff = null;

            int diff = d.Rows.Count;
            bool last_ch = false;

            if (diff > 0)
            {
 
                    //foreach (DataRow item in d.Rows)
                    //{
                    //    if (Array.IndexOf(MainWindow.PartsException, item[0].ToString().Trim()) != -1)
                    //        d.Rows.Remove(item);
                    //}

                    //if (d.Rows.Count == 0) return;

                if (!LastChance_new(pallet, recipe, out dDiff, line))
                {
                    m_Form.FillOneRecipe(new string[] { setup, recipe, line });

                    if (!flag)
                    {
                        string ti = DateTime.Now.ToString("HH:mm:ss");

                        StartDelay_new(line, pallet, board, setup, b, recipe, delay);

                        m_Form.MessageOut(ti
                    + "   " + line + "\tDiff:\t0"
                    + "\t" + pallet
                    + "\tRecipe:\t" + nRecipe.ToString()
                    + "\tTrace:\t" + nTrace.ToString()
                    + "\tTime:\t" + st.ElapsedMilliseconds + "\t"
                    + "Delayed");
                        m_Form.ErrorOut(" Pallet" + pallet + " delayed");
                        return;
                    }

           //         Task task = Task.Run(() => PrintToFile_new(d, pallet, line));

                    List<string> lt = new List<string>();
                    string ms = "Pallet: " + pallet;

                    foreach (DataRow dr in d.Rows)
                        ms = ms + "\n" + (dr["pn"].ToString().Trim() + "\t" + dr["rf"].ToString().Trim() + "\t" + dr["station"].ToString().Trim() + "\tLocation: " + " " + "\tFeeder: " + dr["track"].ToString().Trim() + "\tTrack: " + dr["div"].ToString().Trim());

                    lt.Add(ms);
                    s = GetMissedStations(d);
                    m_Form.EmergencyStopMethod(line, lt, null, "", "Missed components at stations:\n" + s, b);
                }
                else
                {
                    diff = 0;
                    last_ch = true;
                }
            }

            st.Stop();

            string time = DateTime.Now.ToString("HH:mm:ss");
            s = last_ch ? "Rechecked" : (s == "" ? "" : ("  Missed stations: " + s));

            m_Form.MessageOut(time
                + "   " + line + "\tDiff:\t" + diff.ToString()
                + "\t" + pallet
                + "\tRecipe:\t" + nRecipe.ToString()
                + "\tTrace:\t" + nTrace.ToString()
                + "\tTime:\t" + st.ElapsedMilliseconds + "\t"
                + s);

            ClearTraceLine(line, pallet);

            m_Form.CheckIfDBEmpty(line);
        }

        private void StartDelay_new(string line, string pallet, string board, string setup, bool b, string recipe, int delay)
        {
            try
            {
                BackgroundWorker barInvoker = new BackgroundWorker();
                barInvoker.DoWork += delegate
                {
                    Thread.Sleep(TimeSpan.FromSeconds(delay));
                    Task task = Task.Run(() => CompareResults_new(line, pallet, board, setup, b, recipe, true, delay));
                };
                barInvoker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                m_Form.ErrorOut(ex.Message);
            }
        }

        private bool LastChance_new(string pallet, string board, out DataTable dDiff, string line)
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
WHERE     (dbo.PCBBarcode.Barcode = N'{0}') and (dbo.Recipe.Name like N'%{1}')", pallet, board);

            DataTable d2 = sql.SelectDB(query, out string Result);
            if (Result != null)
                m_Form.ErrorOut(Result);

            dDiff = null;

            DataTable d1 = GetDTFromDBRecipe(line);

            if (d2.Rows.Count == 0 || d1.Rows.Count == 0)
                return false;

            dDiff = GetDifferentRecords(d1, d2);

            if (dDiff.Rows.Count > 0)
                return false;

            return true;
        }

        private void PrintToFile_new(DataTable d, string pallet, string line)
        {
            string dir = @"C:\Tmp\Logs2";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            string fil = Path.Combine(dir, pallet.Replace("/", "_")) + "(" + line + ")" + ".txt";

            using (StreamWriter sw = new StreamWriter(fil))
            {
                foreach (DataRow dr in d.Rows)
                {
                    sw.WriteLine(dr["pn"].ToString().Trim() + "\t" + dr["rf"].ToString().Trim() + "\t" + dr["station"].ToString().Trim() + dr["track"].ToString().Trim() + dr["div"].ToString().Trim());
                }
            }
        }

        private int GetNumberInRecipe(string line)
        {
            SQLClass sql = new SQLClass("trace");
            string query = string.Format(@"SELECT COUNT( [id])
  FROM [Traceability].[dbo].[{0}]", line.Replace("Line-", "Receipe_"));

            int i = sql.GetCount(query);

            return i > 0 ? i : 0;
        }

        private int GetNumberInTrace(string line, string pallet)
        {
            SQLClass sql = new SQLClass("trace");
            string query = string.Format(@"SELECT COUNT( [id])
  FROM [Traceability].[dbo].[{0}] where pallet = '{1}'", line.Replace("Line-","Trace_"), pallet);

            int i = sql.GetCount(query);

            return i > 0 ? i : 0;
        }

        private void StartDelay(string line, string pallet, string board, string setup, bool b, string recipe, int delay)
        {
            try
            {
                BackgroundWorker barInvoker = new BackgroundWorker();
                barInvoker.DoWork += delegate
                {
                    Thread.Sleep(TimeSpan.FromSeconds(delay));
                    Task task = Task.Run(() => CompareResults(line, pallet, board, setup, b, recipe, true, delay));
                };
                barInvoker.RunWorkerAsync();
                //await Task.Run(async () =>
                //{
                //    await Task.Delay(TimeSpan.FromSeconds(delay));
                //    CompareResults(line, pallet, board, setup, b, recipe, true);
                //});

                //var t = Task.Factory.StartNew(() =>
                //{
                //    Console.WriteLine("Start");
                //    Task.Delay(TimeSpan.FromSeconds(Convert.ToInt32(m_Form.ComboDelay.Text))).Wait();
                //    CompareResults(line, pallet, board, setup, b, recipe, true);
                //    Console.WriteLine("Done");
                //});
                //t.Wait();
            }
            catch(Exception ex)
            {
                m_Form.ErrorOut(ex.Message);
            }
        }

        private bool LastChance(string pallet, string board, DataTable d1, out DataTable d2, out DataTable dDiff)
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
WHERE     (dbo.PCBBarcode.Barcode = N'{0}') and (dbo.Recipe.Name like N'%{1}')", pallet, board);

            d2 = sql.SelectDB(query, out string Result);
            if (Result != null)
                m_Form.ErrorOut(Result);

            dDiff = null;

            if (d2.Rows.Count == 0 || d1.Rows.Count == 0)
                return false;

            Object thisLock = new Object();
            lock (thisLock)
            {
                dDiff = GetDifferentRecords(d1, d2);
            }


     

            if (dDiff.Rows.Count > 0)
                return false;

            return true;
        }

        private bool CompareRecipes(string line, string board)
        {
            SQLClass sql = new SQLClass("trace");

            string query = "SELECT * FROM [Traceability].[dbo].[Current]";

            DataTable d = sql.SelectDB(query, out string result);
            if (result != null)
                m_Form.ErrorOut(result);

            DataRow[] dr = d.Select(string.Format("{0} = '{1}'", "line", line));

            string res = "";

            if (dr != null && dr.Length > 0)
                res = dr[0][1].ToString().Trim();

            return board == res;
        }

        #endregion

        #region GetMissedArray
        private List<string[]> GetMissedArray(string line, string pallet, DataTable d)
        {
            try
            {
                List<string[]> list = new List<string[]>();
                //lt = new List<string>();

                var result = from r in d.AsEnumerable()
                             group r by new { placeCol = r[0], station = r[2] } into groupby
                             select new
                             {
                                 Value = groupby.Key,
                                 ColumnValues = groupby
                             };

                foreach (var item in result)
                {
                    string comp = item.Value.placeCol.ToString().Trim().Replace(" ","");
                    string station = item.Value.station.ToString().Trim().Replace(" ", "");
                    // string[] str = new string[] { pallet, "", s, "", "", "", "", "", ""};
                    string[] str = new string[] { pallet, "", comp, station, "", "", "", "", "" };
                    string ms = "Pallet: " + pallet + "; " + "PN: " + comp;
                    //  lt.Add(ms);
                    list.Add(str);
                }
                return list;
            }
            catch (Exception ex)
            {
                MainWindow.WriteLog(ex.Message);
                return null;

            }
            
        }
        #endregion

        #region PrintToFile
        private void PrintToFile(DataTable d, string pallet, string line, DataTable d1, DataTable d2)
        {
            string dir = @"C:\Tmp\Logs\";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            string fil = Path.Combine(dir, pallet.Replace("/", "_")) + "(" + line + ")" + ".txt";

            using (StreamWriter sw = new StreamWriter(fil))
            {
                foreach(DataRow dr in d.Rows)
                {
                    sw.WriteLine(dr[0].ToString().Trim() + "\t" + dr[1].ToString().Trim() + "\t" + dr[2].ToString().Trim());
                }
            }
        }
        #endregion

        #region GetMissedStations
        private string GetMissedStations(DataTable d)
        {
            string s = "";

            try
            {
                var groupedData = from b in d.AsEnumerable()
                                  group b by b.Field<string>("Station") into g
                                  select new
                                  {
                                      station = g.Key,
                                      List = g.ToList(),
                                  };

                foreach (var a in groupedData)
                {
                    s = s + a.station.Trim() + " ";
                }
            }
            catch(Exception ex) { m_Form.ErrorOut(ex.Message); }

            return s;
        }
        #endregion

        #region Compare two DataTables and return a DataTable with DifferentRecords  
        /// <summary>  
        /// Compare two DataTables and return a DataTable with DifferentRecords  
        /// </summary>  
        /// <param name="FirstDataTable">FirstDataTable</param>  
        /// <param name="SecondDataTable">SecondDataTable</param>  
        /// <returns>DifferentRecords</returns>  
        public DataTable GetDifferentRecords(DataTable FirstDataTable, DataTable SecondDataTable)
	        {  
	            //Create Empty Table  
	            DataTable ResultDataTable = new DataTable("ResultDataTable");  
	 
	            //use a Dataset to make use of a DataRelation object  
	            using (DataSet ds = new DataSet())  
	            {  
	                //Add tables  
	                ds.Tables.AddRange(new DataTable[] { FirstDataTable.Copy(), SecondDataTable.Copy()});  
	 
	                //Get Columns for DataRelation  
	                DataColumn[] firstColumns = new DataColumn[2];
                    firstColumns[0] = ds.Tables[0].Columns[0];
                    firstColumns[1] = ds.Tables[0].Columns[1];
                //for (int i = 0; i<firstColumns.Length; i++)  
                //{  
                //    firstColumns[i] = ds.Tables[0].Columns[i];  
                //}  

                    DataColumn[] secondColumns = new DataColumn[2];
                    secondColumns[0] = ds.Tables[1].Columns[0];
                    secondColumns[1] = ds.Tables[1].Columns[1];

                //for (int i = 0; i<secondColumns.Length; i++)  
                //{  
                //    secondColumns[i] = ds.Tables[1].Columns[i];  
                //}  

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
	 
	            return ResultDataTable;  
	        }
        #endregion

        #region GetDTFromDBTrace
        private DataTable GetDTFromDBTrace(string line, string pallet)
        {
            string nl = line.Replace("Line-","Trace_");
            DataTable d = null;

            string query = string.Format("SELECT [pn], [rf], [station], [loc], [track], [div], [unitID]" +
                " FROM [Traceability].[dbo].[{0}]" +
                " WHERE [pallet] = '{1}'", nl, pallet);

            SQLClass sql = new SQLClass("trace");

            d = sql.SelectDB(query, out string result);
            if (result != null)
                m_Form.ErrorOut(result);

            return d;
        }
        #endregion

        #region GetDTFromDBRecipe
        private DataTable GetDTFromDBRecipe(string line)
        {
            string nl = line.Replace("Line-", "Receipe_");
            DataTable d = null;

            string query = string.Format("SELECT [pn], [rf], [station], [loc], [track], [div] FROM [Traceability].[dbo].[{0}]", nl);

            SQLClass sql = new SQLClass("trace");

            d = sql.SelectDB(query, out string result);
            if (result != null)
                m_Form.ErrorOut(result);
            return d;
        }
        #endregion

        #region CleanReceipeLine
        private void CleanReceipeLine(string line)
        {
            SQLClass sql = new SQLClass("trace");

            string query = string.Format("truncate table [Traceability].[dbo].[{0}]", line.Replace("Line-", "Receipe_"));

            sql.Update(query);
        }
        #endregion

        #region RegisterReceipe
        private void RegisterReceipe(string board, string line)
        {
            SQLClass sql = new SQLClass("trace");

            string query = 
                string.Format("IF NOT EXISTS(SELECT 1 from [Traceability].[dbo].[Current] where line = '{0}') Insert INTO [Traceability].[dbo].[Current] (line, receipe, tm) VALUES('{0}', '{1}', '{2}') else UPDATE [Traceability].[dbo].[Current] SET receipe = '{1}', tm = '{2}' where line = '{0}'", 
                line, board, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            sql.Insert(query);
        }
        #endregion

        #region CheckReceipes
        private bool CheckReceipes(string board, string line)
        {
            SQLClass sql = new SQLClass("trace");

            string query = string.Format("SELECT * FROM [Traceability].[dbo].[Current] WHERE receipe = '{0}' and line = '{1}'", board, line);

            DataTable d = sql.SelectDB(query, out string result);
            if (result != null)
                m_Form.ErrorOut(result);

            if (d.Rows.Count > 0)
                return true;

            return false;
        }
        #endregion

        #region WriteTraceToDBLines
        private int WriteTraceToDBLines(string line, TraceabilityData trcData, string station)
        {
            int count = 0;

            if (trcData != null)
            {
                string pallet = trcData.BoardID.Trim();
                string recipe = trcData.Jobs[0].BoardName;

                Dictionary<string, Comp> dicComp = FillCompDictionary(trcData, out List<Comp> wrongList, line, station,out bool specialstop);
                SQLClass sql = new SQLClass("trace");


                if (trcData.Panels != null)
                {
                    for (int i = 0; i < trcData.Panels.Length; i++)
                    {
                        Panel p = trcData.Panels[i];

                        if (p.Packagings != null)
                        {
                            foreach (PanelPackaging c in p.Packagings)
                            {
                              
                                if (c.ReferenceDesignators != null)
                                {
                                    PanelRefDes[] pp = c.ReferenceDesignators;

                                    Comp cm = dicComp[c.PackagingRefID];

                                    for (int j = 0; j < pp.Length; j++)
                                    {
                                        string rf = pp[j].Name;
                                        
                                            string query = string.Format("INSERT INTO [Traceability].[dbo].[{0}] "
                                            + "  ([station],[pn],[rf],[loc],[track],[div],[tower],[lvl],[pallet],[unitID],[batch]) "
                                            + "VALUES "
                                            + "  (" + "'" + station + "'"
                                            + "  ," + "'" + cm.PN + "'"
                                            + "  ," + "'" + rf + "'"
                                            + "  ," + "'" + cm.Location.ToString() + "'"
                                            + "  ," + "'" + cm.Track + "'"
                                            + "  ," + "'" + cm.Division.ToString() + "'"
                                            + "  ," + "'" + cm.Tower.ToString() + "'"
                                            + "  ," + "'" + cm.Level.ToString() + "'"
                                            + "  ," + "'" + pallet + "'"
                                            + "  ," + "'" + cm.UnitID.Trim() + "'"
                                            + "  ," + "'" + cm.Batch.Trim() + "'"
                                            + "  )", line.Replace("Line-", "Trace_"));

                                        sql.Insert(query);
                                    }
                                    count += c.ReferenceDesignators.Length;
                                }
                            }
                        }
                    }
                }

                if (wrongList != null && wrongList.Count > 0)
                    CallEmergencyStop(wrongList, station, line, pallet, recipe);
    //            else if (specialstop == false)
    //            {
    //                string msg = "";
    //                foreach (Comp item in wrongList)
    //                {
    //                    string[] str = new string[] {
    //                item.UnitID,
    //                item.Batch,
    //                item.PN,
    //                station,
    //                item.Location.ToString(),
    //                item.Division.ToString(),
    //                item.Tower.ToString(),
    //                item.Level.ToString(),
    //                item.Track.ToString()
    //            };
                        

    //                   msg = line + "; Station: " + station + "; Pallet: " + pallet + "; PN: " + item.PN  +"; UnitID: " + item.UnitID + "; Batch: " + item.Batch;
    //                }
    //                MainWindow._mWindow.ErrorOut("error: " + msg);
    ////                Utils.SendMail(Utils.GetJoinedList("trace", "select eMail from [Users] where [Admin] = '20'", ';', out string Result)
    ////, ""
    ////, "Traceability Monitor Error"
    ////, msg);


    //            }
            }
            return count;
        }
        #endregion

        #region CallEmergencyStop
        private void CallEmergencyStop(List<Comp> wrongList, string station, string line, string pallet, string recipe)
        {
            List<string[]> list = new List<string[]>();
            List<string> lt = new List<string>();

            foreach (Comp item in wrongList)
            {
                string[] str = new string[] {
                    item.UnitID,
                    item.Batch,
                    item.PN,
                    station,
                    item.Location.ToString(),
                    item.Division.ToString(),
                    item.Tower.ToString(),
                    item.Level.ToString(),
                    item.Track.ToString()
                };
                list.Add(str);

                string ms = line + "; Station: " + station + "; Pallet: " + pallet + "; PN: " + item.PN + "; UnitID: " + item.UnitID + "; Batch: " + item.Batch ;
                lt.Add(ms);
            }
            
          bool  check = m_Form.TraceAdam(m_Form.AdamPartNoID);
            bool check2 = (bool)m_Form.DTActiveLines.Select("Line = '" + line + "'")[0]["Active"];

            new LogWriter("adam has stoped in line:" + line + "line Activation" + check2.ToString(),"error");

            m_Form.EmergencyStopMethod(line, lt, list, recipe, "Part does not have Unique ID", (bool)m_Form.DTActiveLines.Select("Line = '" + line + "'")[0]["Active"]);
        }
        #endregion

        #region FillCompDictionary
        private Dictionary<string, Comp> FillCompDictionary(TraceabilityData trcData, out List<Comp> wrongList,string line, string station, out bool specialflag)
        {
            Dictionary<string, Comp> dic = new Dictionary<string, Comp>();
            wrongList = new List<Comp>();
            specialflag = true;

            if (trcData.Locations != null)
            {
                foreach (Location location in trcData.Locations)
                {
                    int loc = location.Loc;

                    if (location.Positions != null)
                    {
                        foreach (Position position in location.Positions)
                        {
                            for (int i = 0; i < position.PackagingUnits.Length; i++)
                            {
                                int track = position.Track;
                                int div = position.Div;
                                int tower = position.Tower;
                                int level = position.Level;
                                string pn = position.PackagingUnits[i].ComponentBarcode;
                                string key = position.PackagingUnits[i].Id;
                                string pID = position.PackagingUnits[i].PackagingId;
                                string pBatch = position.PackagingUnits[i].BatchId;

                                Comp c = new Comp(pn, loc, track, div, tower, level, i, pID, string.IsNullOrEmpty( pBatch ) ? "" : pBatch);
                                dic.Add(key, c);
                                bool is_skid = (bool)m_Form.DTActiveLines.Select("Line = '" + line + "'")[0]["skid"];
                                new LogWriter(line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level,"");

                                //if (i == 0 )
                                //{
                                //    if (Regex.IsMatch(pID, MainWindow._patBatch))
                                //        is_skid = true;
                                //    else is_skid = false;

                                //}


                        
                                
                                // bool flag = false;

                                //if (is_skid)
                                //{
                                //    if (!Regex.IsMatch(pID, MainWindow._patBatch))
                                //        if (Array.IndexOf(MainWindow.PartsException, pn) == -1)
                                //        {
                                //            new LogWriter("**********ERROR************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level);
                                //            wrongList.Add(c);
                                //        }
                                //}


                                if (is_skid) 
                                {
                                    if (tower > 0)
                                    { // || !Regex.IsMatch(pBatch, MainWindow._patBatch) // 
                                        if (pBatch == null)
                                        {
                                            if (Array.IndexOf(MainWindow.PartsException, pn) == -1)
                                            {
                                                new LogWriter("**********ERROR (IS_SKID is TRUE tower > 0!)************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level, "error");
                                                // specialflag = false; // send only mails, don't stop adam!
                                                wrongList.Add(c);

                                            }
                                        }

                                        else if (!Regex.IsMatch(pBatch, MainWindow._patBatch) && !pBatch.Contains("_"))
                                        {
                                            MainWindow._mWindow.ErrorOut("error:  ( SKID IS TRUE!)" + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level);
                                            new LogWriter("**********ERROR (IS_SKID is TRUE tower > 0!)************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level, "error");
                                            wrongList.Add(c);

                                        }

                                    }
                                    else
                                    {
                                        if (!Regex.IsMatch(pID, MainWindow._patBatch))
                                        {
                                            if (Array.IndexOf(MainWindow.PartsException, pn) == -1)
                                            {
                                                new LogWriter("**********ERROR (IS_SKID is TRUE tower <= 0!)************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level,"error");
                                                MainWindow._mWindow.ErrorOut("error:  ( SKID IS TRUE!)" + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level);

                                                wrongList.Add(c);

                                            }


                                        }
                                    }
                                  
                                }

                                else if (!is_skid)
                                    if (tower > 0)
                                    {
                                        if (pBatch == null || (!Regex.IsMatch(pBatch, MainWindow._patUnitID) && !Regex.IsMatch(pBatch, MainWindow._patBatch)))
                                        {
                                            if (Array.IndexOf(MainWindow.PartsException, pn) == -1)
                                            {
                                             //   specialflag = false;
                                                new LogWriter("**********ERROR (IS_SKID is FALSE!) tower > 0 !************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level,"error");
                                                MainWindow._mWindow.ErrorOut("error:  ( SKID IS FALSE!)" + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level);

                                                wrongList.Add(c);

                                            }

                                        }


                                    }
                                    else
                                    {
                                        if (!Regex.IsMatch(pID, MainWindow._patUnitID))
                                        {
                                            if (Array.IndexOf(MainWindow.PartsException, pn) == -1)
                                            {
                                                new LogWriter("**********ERROR (IS_SKID is FALSE!) tower <= 0************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level,"error");
                                                MainWindow._mWindow.ErrorOut("error:  ( SKID IS FALSE!)" + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level);

                                                wrongList.Add(c);

                                            }


                                        }


                                    }
                                //else if (!is_skid)
                                //     if ( !Regex.IsMatch(pID, MainWindow._patUnitID))
                                //      {


                                //           if (pBatch == null || !Regex.IsMatch(pBatch, MainWindow._patBatch))
                                //             {
                                //                if (Array.IndexOf(MainWindow.PartsException, pn) == -1)
                                //                    {
                                //                        new LogWriter("**********ERROR (IS_SKID is FALSE!)************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level);
                                //                        wrongList.Add(c);

                                //                    }
                                //             }
                                //      }  

                            } // end for
                        }
                    }
                }
            }
            return dic;
        }
        #endregion

        #region RefsToString
        private string RefsToString(PanelRefDes[] pp)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < pp.Length; i++)
            {
                sb.Append(pp[i].Name).Append(",");
            }
            return sb.ToString();
        }
        #endregion

        #region GetStationLocation
        private bool GetStationLocation(string line, string station)
        {
            foreach (FlexLine f in MainWindow.LineCollection)
            {
                try
                {
                    if (f.Name == line)
                    {
                        if (station == f.Last)
                            return true;
                    }
                }
                catch (Exception ex)
                {
                    MainWindow._mWindow.ErrorOut(ex.Message);
                }
            }
            return false;
        }
        #endregion

        #region GetRecipe
        private DataTable GetRecipe(string board, string line)
        {
            string query = string.Format(@"SELECT     TOP (100) PERCENT AliasName_3.ObjectName AS Setup, dbo.CFolder.bstrDisplayName AS Line, dbo.AliasName.ObjectName AS RecipeName, 
                      dbo.CComponentPlacement.bstrRefDesignator AS RefDes, AliasName_2.ObjectName AS PN, AliasName_1.ObjectName AS Station, dbo.CHeadSchedule.lHeadIndex AS Location, 
                      dbo.CPickupLink.lTrack AS Track, dbo.CPickupLink.lReserve AS Division, dbo.CPickupLink.lTower AS Tower, dbo.CPickupLink.lLevel AS [Level]
FROM         dbo.CFolder INNER JOIN
                      dbo.CRecipe INNER JOIN
                      dbo.AliasName ON dbo.CRecipe.OID = dbo.AliasName.PID INNER JOIN
                      dbo.CHeadSchedule ON dbo.CRecipe.OID = dbo.CHeadSchedule.PID INNER JOIN
                      dbo.AliasName AS AliasName_1 ON dbo.CHeadSchedule.spStation = AliasName_1.PID INNER JOIN
                      dbo.CHeadStep ON dbo.CHeadSchedule.OID = dbo.CHeadStep.PID INNER JOIN
                      dbo.CPickupLink ON dbo.CRecipe.OID = dbo.CPickupLink.PID AND dbo.CHeadStep.lPickupLink = dbo.CPickupLink.lIndex INNER JOIN
                      dbo.AliasName AS AliasName_2 ON dbo.CPickupLink.spComponentRef = AliasName_2.PID INNER JOIN
                      dbo.CPlacementLink ON dbo.CRecipe.OID = dbo.CPlacementLink.PID AND dbo.CHeadStep.lPlacementLink = dbo.CPlacementLink.lIndex INNER JOIN
                      dbo.CComponentPlacement ON dbo.CPlacementLink.spComponentPlacement = dbo.CComponentPlacement.OID ON dbo.CFolder.OID = dbo.AliasName.FolderID INNER JOIN
                      dbo.AliasName AS AliasName_3 ON dbo.CRecipe.spSetupRef = AliasName_3.PID
WHERE     (dbo.AliasName.ObjectName = N'{0}') AND (dbo.CFolder.bstrDisplayName = N'{1}')
ORDER BY PN",board, line);

            SQLClass sql = new SQLClass();

            DataTable d = sql.SelectDB(query, out string result);
            if (result != null)
                m_Form.ErrorOut(result);

            return d;
        }
        #endregion
    }
    static class AsyncUtils
    {
        static public void DelayCall(int msec, Action fn)
        {
            // Grab the dispatcher from the current executing thread
            Dispatcher d = Dispatcher.CurrentDispatcher;

            // Tasks execute in a thread pool thread
            new Task(() => {
                System.Threading.Thread.Sleep(msec);   // delay

                // use the dispatcher to asynchronously invoke the action 
                // back on the original thread
                d.BeginInvoke(fn);
            }).Start();
        }
    }
}