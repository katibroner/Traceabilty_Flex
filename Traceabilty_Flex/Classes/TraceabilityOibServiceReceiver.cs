using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using TraceabilityTestGui;
using Traceabilty_Flex.Classes;
using www.siplace.com.OIB._2012._03.Traceability.Contracts.Data;
using www.siplace.com.OIB._2012._03.Traceability.Contracts.Service;

namespace Traceabilty_Flex
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class TraceabilityOibServiceReceiver : ITraceabilityDataDuplex
    {
        private readonly MainWindow _mainForm;
        private const string Endpoint = "http://smtoib:1405/Asm.As.Oib.WS.Eventing.Services/SubscriptionManager";
        public SQLdb sqlValidSide = new SQLdb(@"10.229.1.144\SMT", "Traceability", "aoi", "$Flex2016");

        public TraceabilityOibServiceReceiver(MainWindow form)
        {
            _mainForm = form;
            InitiallizeProxy();
        }

        private void InitiallizeProxy()
        {
            var binding = new NetTcpBinding
            {
                Security = { Mode = SecurityMode.None },
                CloseTimeout = TimeSpan.FromMinutes(10),
                OpenTimeout = TimeSpan.FromMinutes(10),
                ReceiveTimeout = TimeSpan.FromMinutes(10)
            };

            binding.ReliableSession.InactivityTimeout = binding.SendTimeout = TimeSpan.MaxValue;
            binding.ReliableSession.Enabled = true;
            binding.PortSharingEnabled = true;

            // Create the endpoint
        }

        public NewTraceabilityDataResponse NewTraceabilityData(TraceabilityDataRequest request)
        {
            var line = string.Empty;
            NewTraceabilityDataResponse response = null;
            try
            {
                response = new NewTraceabilityDataResponse();
                var trcData = request.TraceabilityData;
                var board = string.Empty;
                var boardSide = string.Empty;
                line = string.Empty;
                var station = string.Empty;
                var pallet = string.Empty;
                var setup = string.Empty;
                var recipe = string.Empty;
                var stationID = string.Empty;
                var panels = 0;
                var Lane = string.Empty;



                if (_mainForm != null)
                {
                    if (trcData != null)
                    {
                        if (trcData.Jobs != null)
                        {
                            line = MainWindow.GetLeaf(trcData.Line);
                            pallet = trcData.BoardID == null ? "" : trcData.BoardID;
                            board = MainWindow.GetLeaf(trcData.Jobs[0].BoardName);
                            boardSide = MainWindow.GetLeaf(trcData.Jobs[0].BoardSide);
                            setup = MainWindow.GetLeaf(trcData.Jobs[0].Setup);
                            recipe = MainWindow.GetLeaf(trcData.Jobs[0].Recipe);
                            stationID = trcData.MachineID;
                            panels = trcData.Panels != null ? trcData.Panels.Length : 0;
                            Lane = trcData.Lane;
                            if (line == "Line-R" || line == "Line-S" || line == "Line-Q")
                                line = GetLineSide(line, Lane);

                            if (line == "Line-C" || line == "Line-N" || line == "Line-G")
                                line = line;

                            ShowActivity(trcData, line, station, pallet, recipe);
                            if (!MainWindow.StatusDictionary[line])
                                return response;
                        }

                        station = trcData.Station != null ? MainWindow.GetLeaf(trcData.Station) : "";

                        if ((trcData.ErrorCodes != null) && (trcData.ErrorCodes.ErrorCodesList != null))
                        {
                            foreach (var error in trcData.ErrorCodes.ErrorCodesList)
                            {
                                if (error.ErrorReasons != null)
                                {
                                    foreach (var reason in error.ErrorReasons)
                                    {
                                        _mainForm.ErrorOut(" ErrorLevel  = " + error.ErrorLevel + " " + line + " Program: " + board + " Station: " + station + " Pallet: " + pallet + "  ErrorReason = " + reason.Reason + " Source = " + reason.Source);
                                    }
                                }
                            }
                        }
                    }
                }
                checkSideValidation(board, pallet, line);
                if (pallet.StartsWith("NO_PCB_BARCODE"))
                {
                    if (_mainForm != null && _mainForm.CheckStation(line, station))
                    {
                        _mainForm.ErrorOut(line + ", " + station + ", " + "NO_PCB_BARCODE");
                        _mainForm.EmergencyStopMethod(line + " " + station, null, null, recipe, "NO_PCB_BARCODE", true, "", "", "");
                    }
                    return response;
                }
                if (MainWindow._mainservice)
                {
                    if (recipe != board)
                        return response;//Only For dual line, temporery.

                    WriteTraceToDbLines(line, trcData, station);
                    TurnOnLightLine(line);

                    if (_mainForm != null)
                    {
                        _mainForm.RegisterPallet(line, pallet, station, board, setup, recipe, out var last, out var over);

                        if (last)
                        {
                            GetActiveLines();
                            var task = Task.Run(() => CompareResults(line, pallet, board, setup, true, recipe, false, _mainForm._delay, Lane, boardSide));
                        }
                        _mainForm.TrackPallet(line, station, pallet);
                    }
                }
                else
                {
                    ShowActivity(trcData, line, station, pallet, recipe);
                    if (_mainForm != null)
                    {
                        _mainForm.RegisterPalletA(line, pallet, station);
                        _mainForm.TrackPallet(line, station, pallet);
                    }
                }
            }
            catch (Exception dumpRequestException)
            {
                _mainForm?.ExceptionOut("Exception during printing information in NewTraceabilityData + Line " + line, dumpRequestException, false);
            }
            return response;
        }
       
        private static bool isBoardDubleSided(string board)
        {
            string originalBoard = board;
            string str = "cs";
            int index = originalBoard.IndexOf(str);
            string newBoardWithinCS = originalBoard.Substring(0, index);
            DataTable tbl = new DataTable();
            SiplacePro.openConnection();
            SiplacePro.sql = "SELECT    TOP (100) PERCENT dbo.AliasName.ObjectName FROM dbo.CBoard " +
                "INNER JOIN dbo.AliasName ON dbo.CBoard.OID = dbo.AliasName.PID " +
                "WHERE(dbo.AliasName.ObjectName LIKE N'%" + newBoardWithinCS + "%')";
            SiplacePro.cmd.CommandText = SiplacePro.sql;
            SiplacePro.rd = SiplacePro.cmd.ExecuteReader();
            tbl.Load(SiplacePro.rd);
            bool x = false;
            for (int i = 0; i < tbl.Rows.Count; i++)
            {
                if (tbl.Rows[i][0].ToString().Contains("ps"))
                {

                    x = true;
                }
            }
            return x;
        }
        private void checkSideValidation(string board, string pallet, string line)
        {
            if (!MainWindow.isBoardException(board))
            {
                if (board.Contains("cs"))
                {
                    if (isBoardDubleSided(board))
                    {
                        DataTable palletInTraceDB = new DataTable();
                        traceDB.openConnection();
                        traceDB.sql = "SELECT DISTINCT TOP(100) PERCENT dbo.PCBBarcode.Barcode AS PCBBarcode, SUBSTRING(dbo.Recipe.Name, 15, LEN(dbo.Recipe.Name)) AS Recipe " +
                                  "FROM dbo.PCBBarcode FULL OUTER JOIN dbo.Setup INNER JOIN dbo.Recipe INNER JOIN dbo.Job INNER JOIN " +
                                   "dbo.TraceData INNER JOIN dbo.TraceJob ON dbo.TraceData.Id = dbo.TraceJob.TraceDataId ON dbo.Job.Id = dbo.TraceJob.JobId " +
                                   "ON dbo.Recipe.id = dbo.Job.RecipeId INNER JOIN dbo.Station ON dbo.TraceData.StationId = dbo.Station.Id " +
                                   "ON dbo.Setup.id = dbo.Job.SetupId INNER JOIN dbo.vOrder5 ON dbo.Job.OrderId = dbo.vOrder5.id " +
                                   "ON dbo.PCBBarcode.Id = dbo.TraceData.PCBBarcodeId " +
                                  "WHERE(dbo.PCBBarcode.Barcode = N'" + pallet + "') AND(SUBSTRING(dbo.Station.Name, 15, LEN(dbo.Station.Name)) LIKE 'Sipl%') ";

                        traceDB.cmd.CommandText = traceDB.sql;
                        traceDB.rd = traceDB.cmd.ExecuteReader();
                        palletInTraceDB.Load(traceDB.rd);
                        if (palletInTraceDB.Rows.Count == 0)
                        {
                            _mainForm.ErrorOut("Assembly ps side not found on board :  " + pallet + " => " + line);
                            _mainForm.EmergencyStopMethod(line, null, null, " ", "Assembly ps side not found on board:  " + pallet + " => " + line, true, "", "", board);

                        }
                        if (palletInTraceDB.Rows.Count > 0)
                        {

                            if ((!isPsExists(palletInTraceDB)) && (!isPalletIxistsInSideValidationDb(pallet)))
                            {
                                _mainForm.ErrorOut("Assembly ps side not found on board:  " + pallet + " => " + line);
                                _mainForm.EmergencyStopMethod(line, null, null, " ", "Assembly ps side not found on board:  " + pallet + " => " + line, true, "", "", board);

                            }
                        }

                    }


                }
            }




        }

        private static bool isPsExists(DataTable tbl)
        {
            bool x = false;
            for (int i = 0; i < tbl.Rows.Count; i++)
            {
                if (tbl.Rows[i][1].ToString().Contains("ps"))
                {

                    x = true;
                }

            }
            return x;

        }
        private static bool isPalletIxistsInSideValidationDb(string pallet)
        {
            string connectionString = @"Data Source=migsqlclu4\smt;Initial Catalog=Traceability;Persist Security Info=True;User ID=aoi;Password=$Flex2016";
            string qry = string.Format("IF EXISTS (SELECT TOP (1000) [Pallet] FROM [Traceability].[dbo].[SideValidation] WHERE Pallet = @plt ) SELECT 1 ELSE SELECT 0");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(qry, connection))
                {
                    command.Parameters.AddWithValue("@plt", pallet);
                    connection.Open();
                    int result = (int)command.ExecuteScalar();
                    if (result == 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }

                }
            }

        }

        private string GetLineSide(string line, string Convayer)
        {
            if (line.Contains("Line-R"))// Line R Convayer
            {
                if (Convayer == "Right")
                    return "Line-R1";
                else if (Convayer == "Left")
                    return "Line-R2";
            }
            if (line.Contains("Line-S"))// Line S Convayer
            {
                if (Convayer == "Right")
                    return "Line-S1";
                else if (Convayer == "Left")
                    return "Line-S2";
            }
            if (line.Contains("Line-Q"))// Line S Convayer
            {
                if (Convayer == "Right")
                    return "Line-Q1";
                else if (Convayer == "Left")
                    return "Line-Q2";
            }
            return "";
        }

        private void GetActiveLines()
        {
            var sql = new SqlClass("trace");
            var query = @"SELECT * FROM Lines";

            _mainForm.DTActiveLines = sql.SelectDb(query, out var result);
            if (result != null)
                _mainForm.ErrorOut(result);
        }

        private void ShowActivity(TraceabilityData trcData, string line, string station, string pallet, string board)
        {

            var time = DateTime.Now.ToString("HH:mm:ss");
            _mainForm.MessageOut(time + "\tProgram:  " + board + "\tStation:  " + station + "\tPallet:  " + pallet);

            if (board == MainWindow.RecipeDictionary[line]) return;
            _mainForm.FillRecipeDt();

            var d = GetRecipe(board, line);
            _mainForm.SetFirstLastInLine(d, line);
        }

        private void TurnOnLightLine(string line)
        {
            var index = MainWindow.LineCollection.FindIndex(a => a.Name == line);
            MainWindow._buttons[index].Background = Brushes.Green;
        }

        private void ClearTraceLine(string line, string pallet)
        {
            var sql = new SqlClass("trace");
            var query = string.Format("DELETE FROM TraceList WHERE line='{0}' and pallet = '{1}'", line, pallet);

            sql.Update(query);
        }

        private void CompareResults(string line, string pallet, string board, string setup, bool b, string recipe, bool StartdelayFlag, int delay, string Lane, string boardSide)
        {
            var st = new Stopwatch();
            st.Start();

            if (line == "Line-C" || line == "Line-N" || line == "Line-G")
                line = line;
            var d1 = GetDtFromDbRecipe(line);
            var d2 = GetDtFromDbTrace(line, pallet);

            if (d1.Rows.Count == 0)
            {
                _mainForm.FillOneRecipe(new string[] { setup, recipe, line });
                d1 = GetDtFromDbRecipe(line);
                if (d1.Rows.Count == 0)
                {
                    _mainForm.ErrorOut(line + " Recipe is empty.");
                    return;
                }
            }
            if (d2.Rows.Count == 0)
            {
                _mainForm.ErrorOut(line + " Trace is empty.");
                return;
            }
            DataTable d = null;
            var thisLock = new Object();
            lock (thisLock)
            {
                d = GetDifferentRecords(d1, d2);
            }
            var s = "";
            DataTable dRet = null;
            DataTable dDiff = null;

            var diff = d.Rows.Count;
            var last_ch = false;

            if (diff > 0)
            {
                List<DataRow> toDelete = new List<DataRow>();

                foreach (DataRow item in d.Rows)
                {
                    if (Array.IndexOf(MainWindow.PartsException, item[0].ToString().Trim()) != -1)
                    {
                        toDelete.Add(item);
                    }
                }

                foreach (DataRow dr in toDelete)
                {
                    d.Rows.Remove(dr);
                }

                if (d.Rows.Count == 0) return;


                if (!LastChance(pallet, recipe, board, line, d1, StartdelayFlag, out dRet, out dDiff))
                {
                    _mainForm.FillOneRecipe(new string[] { setup, recipe, line }); // this is for restarting the program
                    d1 = GetDtFromDbRecipe(line);

                    if (!StartdelayFlag)
                    {
                        var ti = DateTime.Now.ToString("HH:mm:ss");

                        StartDelay(line, pallet, board, setup, b, recipe, delay, Lane, boardSide);

                        _mainForm.MessageOut(ti
                    + "   " + line + "\tDiff:\t0"
                    + "\t" + pallet
                    + "\tRecipe:\t" + d1.Rows.Count.ToString()
                    + "\tTrace:\t" + d2.Rows.Count.ToString()
                    + "\tTime:\t" + st.ElapsedMilliseconds + "\t"
                    + "Delayed");
                        _mainForm.ErrorOut("Pallet " + pallet + " delayed, " + "Line: " + line);
                        return;
                    }

                    //var task2 = Task.Run(() => PrintToFile(d, pallet, line, d1, dRet));

                    var lt = new List<string>();
                    var ms = "Pallet: " + pallet;

                    foreach (DataRow dr in d.Rows)
                        ms = ms + Environment.NewLine + dr[0].ToString().Trim() + "\t" + dr[1].ToString().Trim() + "\t" + dr[2].ToString().Trim() + "\tLocation: " + dr[3].ToString().Trim() + "\tFeeder: " + dr[4].ToString().Trim() + "\tTrack: " + dr[5].ToString().Trim();

                    lt.Add(ms);
                    s = GetMissedStations(d);
                    var MissedList = GetMissedArray(line, pallet, d);
                    var check2 = (bool)_mainForm.DTActiveLines.Select("Line = '" + line + "'")[0]["Active"];
                    _mainForm.EmergencyStopMethod(line, lt, MissedList, recipe, "Missed components at stations:" + Environment.NewLine + s, check2, Lane, boardSide, board);
                }
                else
                {
                    d2 = dRet;
                    diff = 0;
                    last_ch = true;
                }
            }

            st.Stop();

            var time = DateTime.Now.ToString("HH:mm:ss");
            s = last_ch ? "Rechecked" : (s == "" ? "" : ("  Missed stations: " + s));

            _mainForm.MessageOut(time + "   " + line + "\tDiff: " + diff.ToString()
                + "\t" + pallet
                + "\tRecipe:\t" + d1.Rows.Count.ToString()
                + "\tTrace:\t" + d2.Rows.Count.ToString()
                + "\tTime:\t" + st.ElapsedMilliseconds + "\t"
                + s);

            ClearTraceLine(line, pallet);
            _mainForm.CheckIfDBEmpty(line);
        }


        private void StartDelay(string line, string pallet, string board, string setup, bool b, string recipe, int delay, string Lane, string boardSide)
        {
            try
            {
                var barInvoker = new BackgroundWorker();
                barInvoker.DoWork += delegate
                {
                    //Thread.Sleep(TimeSpan.FromSeconds(delay));
                    Thread.Sleep(TimeSpan.FromSeconds(360));

                    var task = Task.Run(() => CompareResults(line, pallet, board, setup, b, recipe, true, delay, Lane, boardSide));
                };
                barInvoker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                _mainForm.ErrorOut(ex.Message);
            }
        }

        private bool LastChance(string pallet, string recipe, string board, string line, DataTable d1, bool StartdelayFlag, out DataTable d2, out DataTable dDiff)
        {
            var sql = new SqlClass("setup_trace");

            var query = string.Format(@"SELECT TOP (100) PERCENT dbo.PackagingUnit.ComponentBarcode AS PN, dbo.RefDesignator.Name AS RefDes, dbo.PackagingUnit.PackagingUniqueId AS PUID, 
					                   '0' as Stam1,'0' as Stam2, '0' as Stam3
                                        FROM dbo.Placement INNER JOIN
					                    dbo.TracePlacement ON dbo.Placement.PlacementGroupId = dbo.TracePlacement.PlacementGroupId FULL OUTER JOIN
					                    dbo.Recipe INNER JOIN
					                    dbo.Job INNER JOIN
					                    dbo.TraceData INNER JOIN
					                    dbo.TraceJob ON dbo.TraceData.Id = dbo.TraceJob.TraceDataId ON dbo.Job.Id = dbo.TraceJob.JobId ON dbo.Recipe.id = dbo.Job.RecipeId FULL OUTER JOIN
					                    dbo.PCBBarcode ON dbo.TraceData.PCBBarcodeId = dbo.PCBBarcode.Id ON dbo.TracePlacement.TraceDataId = dbo.TraceData.Id FULL OUTER JOIN
					                    dbo.RefDesignator ON dbo.Placement.RefDesignatorId = dbo.RefDesignator.Id FULL OUTER JOIN
					                    dbo.Charge ON dbo.Placement.ChargeId = dbo.Charge.Id FULL OUTER JOIN
					                    dbo.PackagingUnit ON dbo.Charge.PackagingUnitId = dbo.PackagingUnit.Id
                                        WHERE (dbo.PCBBarcode.Barcode = N'{0}') and (dbo.Recipe.Name like N'%{1}')", pallet, recipe);

            d2 = sql.SelectDb(query, out var Result);
            if (Result != null)
                _mainForm.ErrorOut(Result);

            dDiff = null;

            if (d2.Rows.Count == 0 || d1.Rows.Count == 0)
                return false;

            var thisLock = new Object();
            lock (thisLock)
            {
                dDiff = GetDifferentRecords(d1, d2);
            }
            if (dDiff.Rows.Count > 0)
            {
                var ms = Environment.NewLine + "Pallet: " + pallet + ", recipe: " + recipe;

                foreach (DataRow dr in dDiff.Rows)
                    ms = ms + Environment.NewLine + dr[0].ToString().Trim() + "\t" + dr[1].ToString().Trim() + "\t" + dr[2].ToString().Trim() + "\tLocation: " + dr[3].ToString().Trim() + "\tFeeder: " + dr[4].ToString().Trim() + "\tTrack: " + dr[5].ToString().Trim();

                if (StartdelayFlag)
                    LogWriter.WriteLogTest("LastChance Not OK, " + ms, "c:\\tmp\\Traceability\\Traceability_LastChance.txt");
                return false;
            }
            else
            {
                if (StartdelayFlag)
                    LogWriter.WriteLogTest("LastChance OK, Pallet: " + pallet + ", Line: " + line + ", recipe: " + recipe, "c:\\tmp\\Traceability\\Traceability_LastChance.txt");
                return true;
            }
        }

        private List<string[]> GetMissedArray(string line, string pallet, DataTable d)
        {
            try
            {
                var list = new List<string[]>();
                var result = from r in d.AsEnumerable()
                             group r by new { placeCol = r[0], station = r[2] } into groupby
                             select new
                             {
                                 Value = groupby.Key,
                                 ColumnValues = groupby
                             };

                foreach (var item in result)
                {
                    var comp = item.Value.placeCol.ToString().Trim().Replace(" ", "");
                    var station = item.Value.station.ToString().Trim().Replace(" ", "");
                    var str = new string[] { pallet, "", comp, station, "", "", "", "", "" };
                    var ms = "Pallet: " + pallet + "; " + "PN: " + comp;
                    list.Add(str);
                }
                return list;
            }
            catch (Exception ex)
            {
                LogWriter.WriteLog(ex.Message);
                return null;
            }
        }

        private static void PrintToFile(DataTable d, string pallet, string line, DataTable d1, DataTable d2)
        {
            const string dir = @"C:\Tmp\Traceability\Logs\";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var fil = Path.Combine(dir, pallet.Replace("/", "_")) + "(" + line + ")" + ".txt";
            using (var sw = new StreamWriter(fil))
            {
                foreach (DataRow dr in d.Rows)
                {
                    sw.WriteLine(dr[0].ToString().Trim() + "\t" + dr[1].ToString().Trim() + "\t" + dr[2].ToString().Trim());
                }
            }
        }

        private string GetMissedStations(DataTable d)
        {
            var s = "";
            try
            {
                var groupedData = from b in d.AsEnumerable()
                                  group b by b.Field<string>("Station") into g
                                  select new
                                  {
                                      station = g.Key,
                                      List = g.ToList(),
                                  };

                s = groupedData.Aggregate(s, (current, a) => current + a.station.Trim() + " ");
            }
            catch (Exception ex) { _mainForm.ErrorOut(ex.Message); }
            return s;
        }

        private DataTable GetDifferentRecords(DataTable FirstDataTable, DataTable SecondDataTable)
        {
            //Create Empty Table  
            var ResultDataTable = new DataTable("ResultDataTable");

            //use a Dataset to make use of a DataRelation object  
            using (var ds = new DataSet())
            {
                //Add tables  
                ds.Tables.AddRange(new DataTable[] { FirstDataTable.Copy(), SecondDataTable.Copy() });

                //Get Columns for DataRelation  
                var firstColumns = new DataColumn[2];
                firstColumns[0] = ds.Tables[0].Columns[0];
                firstColumns[1] = ds.Tables[0].Columns[1];

                var secondColumns = new DataColumn[2];
                secondColumns[0] = ds.Tables[1].Columns[0];
                secondColumns[1] = ds.Tables[1].Columns[1];

                //Create DataRelation  
                var r1 = new DataRelation(string.Empty, firstColumns, secondColumns, false);
                ds.Relations.Add(r1);

                var r2 = new DataRelation(string.Empty, secondColumns, firstColumns, false);
                ds.Relations.Add(r2);

                //Create columns for return table  
                for (var i = 0; i < SecondDataTable.Columns.Count; i++)
                {
                    ResultDataTable.Columns.Add(SecondDataTable.Columns[i].ColumnName, SecondDataTable.Columns[i].DataType);
                }

                //If FirstDataTable Row not in SecondDataTable, Add to ResultDataTable.  
                ResultDataTable.BeginLoadData();
                foreach (DataRow parentrow in ds.Tables[0].Rows)
                {
                    var childrows = parentrow.GetChildRows(r1);
                    if (childrows == null || childrows.Length == 0)
                        ResultDataTable.LoadDataRow(parentrow.ItemArray, true);
                }

                //If SecondDataTable Row not in FirstDataTable, Add to ResultDataTable.  
                foreach (DataRow parentrow in ds.Tables[1].Rows)
                {
                    var childrows = parentrow.GetChildRows(r2);
                    if (childrows == null || childrows.Length == 0)
                        ResultDataTable.LoadDataRow(parentrow.ItemArray, true);
                }
                ResultDataTable.EndLoadData();
            }
            return ResultDataTable;
        }

        private DataTable GetDtFromDbTrace(string line, string pallet)
        {
            var query = string.Format("SELECT [pn], [rf], [station], [loc], [track], [div], [unitID] FROM TraceList  WHERE line='{0}' and pallet = '{1}'", line, pallet);

            var sql = new SqlClass("trace");
            var d = sql.SelectDb(query, out var result);

            if (result != null)
                _mainForm.ErrorOut(result);
            return d;
        }

        private DataTable GetDtFromDbRecipe(string line)
        {
            var query = string.Format("SELECT [pn], [rf], [station], [loc], [track], [div] FROM RecipeList WHERE line='{0}'", line);
            var sql = new SqlClass("trace");
            var d = sql.SelectDb(query, out var result);

            if (result != null)
                _mainForm.ErrorOut(result);
            return d;
        }

        private void WriteTraceToDbLines(string line, TraceabilityData trcData, string station)
        {
            if (trcData == null || trcData.BoardID == null) return;

            var pallet = trcData.BoardID.Trim().Length > 10 ? trcData.BoardID.Trim() : "";
            var recipe = trcData.Jobs[0].BoardName;

            var dicComp = FillCompDictionary(trcData, out var wrongList, line, station, out var specialstop);
            var sql = new SqlClass("trace");

            if (trcData.Panels != null)
            {
                foreach (var p in trcData.Panels)
                {
                    if (p.Packagings == null) continue;
                    foreach (var c in p.Packagings)
                    {
                        if (c.ReferenceDesignators == null) continue;

                        var pp = c.ReferenceDesignators;
                        var cm = dicComp[c.PackagingRefID];
                        foreach (var t in pp)
                        {
                            var rf = t.Name;
                            var query = string.Format(@"INSERT INTO TraceList ([line],[station],[pn],[rf],[loc],[track],[div],[tower],[lvl],[pallet],[unitID],[batch]) 
                                                      VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}')",
                                                      line, station, cm.Pn, rf, cm.Location.ToString(), cm.Track, cm.Division.ToString(), cm.Tower.ToString(), cm.Level.ToString(), pallet, cm.UnitId.Trim(), cm.Batch.Trim());
                            sql.Insert(query);
                        }
                    }
                }
            }

            if (wrongList != null && wrongList.Count > 0)
                CallEmergencyStop(wrongList, station, line, pallet, recipe);
        }

        private void CallEmergencyStop(List<Comp2> wrongList, string station, string line, string pallet, string recipe)
        {
            var list = new List<string[]>();
            var lt = new List<string>();

            foreach (var item in wrongList)
            {
                var str = new string[] {
                    pallet,
                    item.Batch,
                    item.Pn,
                    station,
                    item.Location.ToString(),
                    item.Division.ToString(),
                    item.Tower.ToString(),
                    item.Level.ToString(),
                    item.Track.ToString()
                };
                list.Add(str);

                var ms = line + "; Station: " + station + "; Pallet: " + pallet + "; PN: " + item.Pn + "; UnitID: " + item.UnitId + "; Batch: " + item.Batch;
                lt.Add(ms);
            }

            var check2 = (bool)_mainForm.DTActiveLines.Select("Line = '" + line + "'")[0]["Active"];

            new LogWriter("adam has stoped in line:" + line + "line Activation" + check2.ToString(), "error");
            _mainForm.EmergencyStopMethod(line, lt, list, recipe, "Part does not have Unique ID", true, "", "", "");
        }

        private Dictionary<string, Comp2> FillCompDictionary(TraceabilityData trcData, out List<Comp2> wrongList, string line, string station, out bool specialflag)
        {
            var dic = new Dictionary<string, Comp2>();
            wrongList = new List<Comp2>();
            specialflag = true;

            if (trcData.Locations != null)
            {
                foreach (var location in trcData.Locations)
                {
                    var loc = location.Loc;

                    if (location.Positions != null)
                    {
                        foreach (var position in location.Positions)
                        {
                            for (var i = 0; i < position.PackagingUnits.Length; i++)
                            {
                                var track = position.Track;
                                var div = position.Div;
                                var tower = position.Tower;
                                var level = position.Level;
                                var pn = position.PackagingUnits[i].ComponentBarcode;
                                var key = position.PackagingUnits[i].Id;
                                var pID = position.PackagingUnits[i].PackagingId;
                                var pBatch = position.PackagingUnits[i].BatchId;
                                var c = new Comp2(pn, loc, track, div, tower, level, i, pID, string.IsNullOrEmpty(pBatch) ? "" : pBatch);
                                dic.Add(key, c);
                                var is_skid = true;
                                new LogWriter(line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level, "");
                                if (is_skid)
                                {
                                    if (tower > 0)
                                    {
                                        if (pID.Length != 10)
                                        {
                                            if (Array.IndexOf(MainWindow.PartsException, pn) == -1)
                                            {
                                                new LogWriter("**********ERROR (IS_SKID is TRUE tower > 0!)************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level, "error");
                                                wrongList.Add(c);
                                            }
                                        }

                                        //if (pBatch == null)
                                        //{
                                        //    if (Array.IndexOf(MainWindow.PartsException, pn) == -1)
                                        //    {
                                        //        new LogWriter("**********ERROR (IS_SKID is TRUE tower > 0!)************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level, "error");
                                        //        wrongList.Add(c);
                                        //    }
                                        //}

                                        //else if (!Regex.IsMatch(pBatch, MainWindow._patBatch) && !pBatch.Contains("_"))
                                        //{
                                        //    MainWindow._mWindow.ErrorOut("error:  ( SKID IS TRUE!)" + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level);
                                        //    new LogWriter("**********ERROR (IS_SKID is TRUE tower > 0!)************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level, "error");
                                        //    wrongList.Add(c);
                                        //}
                                    }
                                    else
                                    {
                                        if (!Regex.IsMatch(pID, MainWindow._patBatch))
                                        {
                                            if (Array.IndexOf(MainWindow.PartsException, pn) == -1)
                                            {
                                                new LogWriter("**********ERROR (IS_SKID is TRUE tower <= 0!)************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level, "error");
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
                                                new LogWriter("**********ERROR (IS_SKID is FALSE!) tower > 0 !************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level, "error");
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
                                                new LogWriter("**********ERROR (IS_SKID is FALSE!) tower <= 0************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level, "error");
                                                MainWindow._mWindow.ErrorOut("error:  ( SKID IS FALSE!)" + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level);
                                                wrongList.Add(c);
                                            }
                                        }
                                    }
                            } // end for
                        }
                    }
                }
            }
            return dic;
        }

        private DataTable GetRecipe(string board, string line)
        {
            line = line.Contains("S") ? "Line-S" : line.Contains("R") ? "Line-R" : line.Contains("Q") ? "Line-Q" : line;
            var query = string.Format(@"SELECT TOP (100) PERCENT AliasName_3.ObjectName AS Setup, dbo.CFolder.bstrDisplayName AS Line, dbo.AliasName.ObjectName AS RecipeName, 
					  dbo.CComponentPlacement.bstrRefDesignator AS RefDes, AliasName_2.ObjectName AS PN, AliasName_1.ObjectName AS Station, dbo.CHeadSchedule.lHeadIndex AS Location, 
					  dbo.CPickupLink.lTrack AS Track, dbo.CPickupLink.lReserve AS Division, dbo.CPickupLink.lTower AS Tower, dbo.CPickupLink.lLevel AS [Level]
                      FROM dbo.CFolder INNER JOIN
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
                      WHERE (dbo.AliasName.ObjectName = N'{0}') AND (dbo.CFolder.bstrDisplayName = N'{1}')
                      ORDER BY PN", board, line);

            var sql = new SqlClass();
            var d = sql.SelectDb(query, out var result);
            if (result != null)
                _mainForm.ErrorOut(result);
            return d;
        }
    }
}