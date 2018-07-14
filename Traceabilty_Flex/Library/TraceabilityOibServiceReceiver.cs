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
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;


#endregion

namespace TraceabilityTestGui
{
    /// <summary>
    /// Receiver class for the Setup Center events
    /// </summary>
    [ServiceBehavior(InstanceContextMode=InstanceContextMode.Single)]
    public class TraceabilityOibServiceReceiver : ITraceabilityDataDuplex
    {
        #region Fields
        private readonly Form1 m_Form;
        enum InLine { First, Middle, Last};
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="TraceabilityOibServiceReceiver"/> class.
        /// </summary>
        /// <param name="form">The form.</param>
        public TraceabilityOibServiceReceiver(Form1 form)
        {
            m_Form = form;
        }
        #endregion // Constructor

        #region ITraceabilityDataDuplex Members

        public NewTraceabilityDataResponse NewTraceabilityData(TraceabilityDataRequest request)
        {
            NewTraceabilityDataResponse response = new NewTraceabilityDataResponse();
            TraceabilityData trcData = request.TraceabilityData;
            string board = "";
            string line = "";
            string station = "";
            string pallet = "";
            string setup = "";
            try
            {
                if (m_Form != null)
                {
                    if (trcData != null)
                    {
                        if (trcData.Jobs != null)
                        {
                            line = GetLeaf(trcData.Line);

                            pallet = trcData.BoardID;

                            foreach (Job job in trcData.Jobs)
                            {
                                string fb = job.BoardName;

                                if(Form1.CustomerList.Length > 0)
                                {
                                    bool cust = false;
                                    for (int i = 0; i < Form1.CustomerList.Length; i++)
                                    {
                                        if (fb.Contains(Form1.CustomerList[i]))
                                        {
                                            cust = true;
                                            break;
                                        }
                                    }

                                    if(!cust)
                                        return response;
                                }
                                board = GetLeaf(job.BoardName);
                                setup = GetLeaf(job.Setup);

                            }
                        }

                        if (trcData.Station != null)
                        {
                            station = GetLeaf(trcData.Station);
                        }

                        if ((trcData.ErrorCodes != null) && (trcData.ErrorCodes.ErrorCodesList != null))
                        {
                            m_Form.MessageOut("ErrorCodes : ");
                            foreach (ErrorStruct error in trcData.ErrorCodes.ErrorCodesList)
                            {
                                m_Form.MessageOut(" ErrorLevel  = " + error.ErrorLevel);
                                if (error.ErrorReasons != null)
                                {
                                    foreach (ReasonStruct reason in error.ErrorReasons)
                                    {
                                        m_Form.MessageOut("  ErrorReason = " + reason.Reason + " Source = " + reason.Source);
                                    }
                                }
                            }
                        }
                        //if (trcData.Consumptions != null)
                        //{
                        //    m_Form.MessageOut("Consumptions  : ");
                        //    foreach (Consumption consumption in trcData.Consumptions)
                        //    {
                        //        string messages = " " + consumption.PackagingUID;
                        //        messages += " " + consumption.AccessTotal;
                        //        messages += "," + consumption.RejectIdent;
                        //        messages += "," + consumption.RejectVacuum;
                        //        messages += "," + consumption.TrackEmpty;
                        //        m_Form.MessageOut("   " + messages);
                        //    }
                        //}
                    }
                }
            }
            catch (Exception dumpRequestException)
            {
                m_Form.ExceptionOut("Exception during printing information in NewTraceabilityData", dumpRequestException, false);
            }
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


            InLine loc = GetStationLocation(line, station, m_Form.LineCollection);

            WriteTraceToDBLines(line, trcData, station);
            string makat = "";

            //bool bSetup = CheckSetup(setup, out makat);

            //if(makat != "")
            //    System.Windows.Forms.MessageBox.Show(makat);

            if (loc == InLine.First)
            {
                if (!CheckReceipes(board, line))
                {
                    DataTable d = GetReceipe(board, line);
                    //bool bSetup = CheckSetup(setup);

                    RegisterReceipe(board, line);
                    CleanReceipeLine(line);
                    WriteReceipeToDBLine(d, line);
                }
            }
            else if (loc == InLine.Last)
            {
                Task task = Task.Run(() => CompareResults(line, pallet));
            }

            return response;
        }

        private bool CheckSetup(string setup, out string  makat)
        {
            makat = "";

            string query = string.Format(@"SELECT TOP(100) PERCENT dbo.PackagingUnit.Batch AS Batch, dbo.PackagingUnit.PackagingUnitId AS Unic, dbo.PackagingUnit.SiplaceProComponent as Comp
        FROM         dbo.TableInUse INNER JOIN
                         dbo.FeederInUse ON dbo.TableInUse.TableInventoryRef = dbo.FeederInUse.TableInventoryRef INNER JOIN
                         dbo.PackagingUnitInUse ON dbo.FeederInUse.FeederInventoryRef = dbo.PackagingUnitInUse.FeederInventoryRef INNER JOIN
                         dbo.PackagingUnit ON dbo.PackagingUnitInUse.PackagingUnitRef = dbo.PackagingUnit.PackagingUnitId
        WHERE     (LEN(dbo.TableInUse.Line) = 13) AND(dbo.TableInUse.Setup LIKE N'%{0}')", setup);

            SQLClass sql = new SQLClass("setup");
            string result = "";

            DataTable dt = sql.SelectDB(query, out result);

            foreach(DataRow dr in dt.Rows)
            {
                string u = dr["Unic"].ToString().Trim();
                string b = dr["Batch"].ToString().Trim();
                string pat = @"\d{10}\/\d{4}";
                if (u.StartsWith("DMY"))
                    u = b;
                if (!Regex.IsMatch(u, pat))
                {

                    makat = u + " : " + b;
                    return false;
                }
            }

            return true;
        }

        void ThreadMethod(string line, string pallet)
        {
            CompareResults(line, pallet);
        }

        private void ClearTraceLine(string line, string pallet)
        {
            SQLClass sql = new SQLClass("trace");

            string query = string.Format("delete from [Traceability].[dbo].[{0}] where pallet = '{1}'", line.Replace("Line-", "Trace_"), pallet);
            sql.Update(query);
        }

        private void CompareResults(string line, string pallet)
        {
            Stopwatch st = new Stopwatch();
            st.Start();
            DataTable d1 = GetDTFromDBReceipe(line); // Load with data
            DataTable d2 = GetDTFromDBTrace(line, pallet); // Load with data (same schema)

            m_Form.Invoke((Action)(() => m_Form.FillDGV(m_Form.dataGridView1, d1)));
            m_Form.Invoke((Action)(() => m_Form.FillDGV(m_Form.dataGridView2, d2)));

            if (d1.Columns.Count == 0 || d2.Columns.Count == 0) return;

            DataTable d = getDifferentRecords(d1, d2);
            string s = "";
            if (d.Rows.Count > 0)
            {
                s = GetMissedStations(d);
               Task task = Task.Run(()=> PrintToFile(d, pallet, line));
            }

            st.Stop();
            m_Form.Invoke((Action)(() => m_Form.FillDGV(m_Form.dataGridView3, d)));
            
            m_Form.Invoke((Action)(() => m_Form.MessageOut("Difference: " + d.Rows.Count.ToString() + "  " + line + "  " + pallet + "     Recipe : " + d1.Rows.Count.ToString() + "  Trace : " + d2.Rows.Count.ToString() + " Time:  " + st.ElapsedMilliseconds + (s == "" ? "" : ("  Missed stations: " + s)))));
            ClearTraceLine(line, pallet);
        }

        private void PrintToFile(DataTable d, string pallet, string line)
        {
            string dir = @"C:\Tmp\Logs";
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

        private string GetMissedStations(DataTable d)
        {
            string s = "";

            var groupedData = from b in d.AsEnumerable()
                              group b by b.Field<string>("Station") into g
                              select new
                              {
                                  station = g.Key,
                                  List = g.ToList(),
                              };

            foreach(var a in groupedData)
            {
                s = s + a.station.Trim() + " ";
            }

            return s;
        }

        #region Compare two DataTables and return a DataTable with DifferentRecords  
        /// <summary>  
        /// Compare two DataTables and return a DataTable with DifferentRecords  
        /// </summary>  
        /// <param name="FirstDataTable">FirstDataTable</param>  
        /// <param name="SecondDataTable">SecondDataTable</param>  
        /// <returns>DifferentRecords</returns>  
        public DataTable getDifferentRecords(DataTable FirstDataTable, DataTable SecondDataTable)
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
	                for (int i = 0; i<FirstDataTable.Columns.Count; i++)  
	                {  
	                    ResultDataTable.Columns.Add(FirstDataTable.Columns[i].ColumnName, FirstDataTable.Columns[i].DataType);  
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
	 
        private DataTable GetDTFromDBTrace(string line, string pallet)
        {
            string nl = line.Replace("Line-","Trace_");
            DataTable d = null;

            string query = string.Format("SELECT [pn], [rf], [station]" +
                " FROM [Traceability].[dbo].[{0}]" +
                " WHERE [pallet] = '{1}'", nl, pallet);

            SQLClass sql = new SQLClass("trace");
            string result = "";

            d = sql.SelectDB(query, out result);

            return d;
        }

        private DataTable GetDTFromDBReceipe(string line)
        {
            string nl = line.Replace("Line-", "Receipe_");
            DataTable d = null;

            string query = string.Format("SELECT [pn], [rf], [station]" + //
                " FROM [Traceability].[dbo].[{0}]", nl);

            SQLClass sql = new SQLClass("trace");
            string result = "";

            d = sql.SelectDB(query, out result);

            return d;
        }

        private void CleanReceipeLine(string line)
        {
            SQLClass sql = new SQLClass("trace");

            string query = string.Format("truncate table [Traceability].[dbo].[{0}]", line.Replace("Line-", "Receipe_"));

            sql.Update(query);
        }

        private void WriteReceipeToDBLine(DataTable d, string line)
        {
            string lr = line.Replace("Line-", "Receipe_");

            SQLClass sql = new SQLClass("trace");

            foreach(DataRow r in d.Rows)
            {
                string station = r["Station"].ToString();
                string rf = r["RefDes"].ToString();
                string pn = r["PN"].ToString().Trim();
                string loc = r["Location"].ToString();
                string track = r["Track"].ToString();
                string div = r["Division"].ToString();
                string tower = r["Tower"].ToString();
                string level = r["Level"].ToString();

                bool skip = false;

                for (int i = 0; i < Form1.PartsException.Length; i++)
                {
                    if (pn == Form1.PartsException[i].Trim())
                    {
                        skip = true;
                        break;
                    }
                }

                if (!skip)
                {
                    string query = string.Format("INSERT INTO [Traceability].[dbo].[{0}] "
                         + "  ([station],[pn],[rf],[loc],[track],[div],[tower],[lvl]) "
                         + "VALUES "
                         + "  (" + "'" + station + "'"
                         + "  ," + "'" + pn + "'"
                         + "  ," + "'" + rf + "'"
                         + "  ," + "'" + Form1.LocationDic[loc] + "'"
                         + "  ," + "'" + track + "'"
                         + "  ," + "'" + Form1.DivisionDic[div] + "'"
                         + "  ," + "'" + tower + "'"
                         + "  ," + "'" + level + "'"
                         + "  )", lr);

                    sql.Insert(query);
                }
            }
        }

        private void RegisterReceipe(string board, string line)
        {
            SQLClass sql = new SQLClass("trace");

            string query = 
                string.Format("IF NOT EXISTS(SELECT 1 from [Traceability].[dbo].[Current] where line = '{0}') Insert INTO [Traceability].[dbo].[Current] (line, receipe, tm) VALUES('{0}', '{1}', '{2}') else UPDATE [Traceability].[dbo].[Current] SET receipe = '{1}', tm = '{2}' where line = '{0}'", 
                line, board, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            sql.Insert(query);
        }

        private bool CheckReceipes(string board, string line)
        {
            SQLClass sql = new SQLClass("trace");

            string query = string.Format("SELECT * FROM [Traceability].[dbo].[Current] WHERE receipe = '{0}' and line = '{1}'", board, line);
            string result = "";

            DataTable d = sql.SelectDB(query, out result);

            if (d.Rows.Count > 0)
                return true;

            return false;
        }

        private void WriteTraceToDBLines(string line, TraceabilityData trcData, string station)
        {
            if(trcData != null)
            {
                string pallet = trcData.BoardID.Trim();
                
                Dictionary<string, Comp> dicComp = FillCompDictionary(trcData);
                int count = 0;
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

                                    if (cm.Number == 1) continue;

                                    for (int j = 0; j < pp.Length; j++)
                                    {
                                        string rf = pp[j].Name;

                                        string query = string.Format("INSERT INTO [Traceability].[dbo].[{0}] "
                                            + "  ([station],[pn],[rf],[loc],[track],[div],[tower],[lvl],[pallet]) "
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
                                            + "  )", line.Replace("Line-", "Trace_"));

                                        sql.Insert(query);
                                    }
                                    count += c.ReferenceDesignators.Length;
                                }
                            }
                        }
                    }
                }
                //m_Form.Invoke((Action)(() => m_Form.MessageOut("Count: " + count.ToString())));
            }
        }

        private Dictionary<string, Comp> FillCompDictionary(TraceabilityData trcData)
        {
            Dictionary<string, Comp> dic = new Dictionary<string, Comp>();

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

                                Comp c = new Comp(pn, loc, track, div, tower, level, i);
                                dic.Add(key, c);

                            }
                        }
                    }
                }
            }
            return dic;
        }

        private string RefsToString(PanelRefDes[] pp)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < pp.Length; i++)
            {
                sb.Append(pp[i].Name).Append(",");
            }
            return sb.ToString();
        }

        private InLine GetStationLocation(string line, string station, List<FlexLine> lineCollection)
        {
            foreach(FlexLine f in lineCollection)
            {
                try
                {
                    if (f.Name == line)
                    {
                        int n = f.StationDictionary[station];

                        if (n == 1)
                            return InLine.First;
                        else if (n == f.StationDictionary.Count)
                            return InLine.Last;
                    }
                }
                catch (Exception) { }
            }
            return InLine.Middle;
        }

        private DataTable GetReceipe(string board, string line)
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
            string result = "";

            DataTable d = sql.SelectDB(query, out result);

            return d;
        }
        #endregion

        static string GetLeaf(object strWithBackSlash)
        {
            if (strWithBackSlash == null) return "";

            string[] arr = strWithBackSlash.ToString().Split('\\');
            return arr[arr.Length - 1];
        }

        //           SETUP query 172.20.20.4
        //        SELECT TOP(100) PERCENT dbo.TableInUse.Line, dbo.TableInUse.Station, dbo.TableInUse.Location, dbo.FeederInUse.Track, dbo.PackagingUnitInUse.Division, dbo.PackagingUnitInUse.[Level],
        //                 dbo.PackagingUnitInUse.Tower, dbo.PackagingUnit.SiplaceProComponent, dbo.TableInUse.Setup, dbo.PackagingUnit.Batch, dbo.PackagingUnit.PackagingUnitId, dbo.TableInUse.LastModifiedDate,
        //                 dbo.PackagingUnit.OriginalQuantity, dbo.PackagingUnit.InitialQuantity, dbo.PackagingUnit.Quantity
        //FROM         dbo.TableInUse INNER JOIN
        //                 dbo.FeederInUse ON dbo.TableInUse.TableInventoryRef = dbo.FeederInUse.TableInventoryRef INNER JOIN
        //                 dbo.PackagingUnitInUse ON dbo.FeederInUse.FeederInventoryRef = dbo.PackagingUnitInUse.FeederInventoryRef INNER JOIN
        //                 dbo.PackagingUnit ON dbo.PackagingUnitInUse.PackagingUnitRef = dbo.PackagingUnit.PackagingUnitId
        //WHERE     (LEN(dbo.TableInUse.Line) = 13) AND(dbo.TableInUse.Setup LIKE N'%(98976)MLX-xxx002063-(4591)-T68')
        //ORDER BY dbo.TableInUse.Line, dbo.TableInUse.Setup, dbo.TableInUse.Station, dbo.FeederInUse.Track, dbo.PackagingUnitInUse.Division, dbo.PackagingUnitInUse.Tower, dbo.PackagingUnitInUse.[Level]


        

        //for (int i = 0; i < groupedData.Count(); i++)
        //{
        //    List<DataRow> list = groupedData.ElementAt(i).List;
        //    var comb = list.GroupBy(x => x.ItemArray[2]).ToList();
        //}

        //string query = string.Format("INSERT INTO [Traceability].[dbo].[{0}] "
        //      + "  ([station],[pn],[rf],[loc],[track],[div],[tower],[lvl]) "
        //      + "VALUES "
        //      + "  (" + ((station == null) ? "null" : "'" + station.Replace("'", "''") + "'")
        //      + "  ," + ((pn == null) ? "null" : "'" + pn.Replace("'", "''") + "'")
        //      + "  ," + ((rf == null) ? "null" : "'" + rf.Replace("'", "''") + "'")
        //      + "  ," + ((loc == null) ? "null" : "'" + loc.Replace("'", "''") + "'")
        //      + "  ," + ((track == null) ? "''" : "'" + track.Replace("'", "''") + "'")
        //      + "  ," + ((div == null) ? "null" : "'" + div.Replace("'", "''") + "'")
        //      + "  ," + ((tower == null) ? "null" : "'" + tower.Replace("'", "''") + "'")
        //      + "  ," + ((level == null) ? "null" : "'" + level.Replace("'", "''") + "'")
        //      + "  )", lr);


    }
}