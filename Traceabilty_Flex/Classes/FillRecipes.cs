using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using TraceabilityTestGui;

namespace Traceabilty_Flex
{
    public partial class MainWindow : Window
    {
        private void FillRecipes(List<string[]> recipeSetupList)
        {
            foreach (var s in recipeSetupList)
            {
                if (s == null) continue;
                try
                {
                    FillOneRecipe(s);
                }
                catch (Exception ex)
                {
                    ErrorOut("At FillRecipes: " + ex.Message);
                }
            }
        }

        internal DataTable FillOneRecipe(string[] s)
        {
            var setup = GetLeaf(s[0]);
            var recipe = GetLeaf(s[1]);
            var line = GetLeaf(s[2]);

            RegisterRecipe(recipe, line);   // to current
                                            // RecipeDictionary[line] = recipe;
            DataTable d = null;
            var client = recipe.Length < 10 ? "Unknown" : GetClient(recipe.Substring(0, 10));
            var thisLock = new Object();
            lock (thisLock)
            {
                d = GetRecipe(recipe, line); // get recipe from sipplace
                if (line.StartsWith("Line-R") || line.StartsWith("Line-S") || line.StartsWith("Line-Q"))
                    d = CompareRecipeDualLine(d, line, recipe);
                CleanRecipeLine(line); // clean from recipe db
                WriteRecipeToDbLine(d, line); // write to recipe db
            }
            SetFirstLastInLine(d, line);

            if (RegisterLineStatus(line, client, recipe))  // check client if belongs to customers and register to StatusDictionary[]
            {
                if (!CheckSetup(client, setup, line, out var list, out var listar))
                    EmergencyStopMethod(line, list, null, recipe, "Wrong Unique ID in Setup:\n", true, "", "", "");
            }
            FillRecipeDt();
            return d;
        }

        internal void SetFirstLastInLine(DataTable d, string line)
        {
            try
            {
                var groupedData = from b in d.AsEnumerable()
                                  group b by b.Field<string>("Station") into g
                                  select new
                                  {
                                      station = g.Key,
                                      List = g.ToList()
                                  };

                var f = FindInCollection(line);

                f.Used = new string[groupedData.Count()];

                var max = 1;
                var min = 1;
                var i = 0;

                foreach (var a in groupedData)
                {
                    var st = a.station;

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
                ErrorOut("At SetFirstLastInLine: " + ex.Message);
            }
        }

        private DataTable GetRecipe(string recipe, string line)
        {
            line = line.Contains("S") ? "Line-S" : line.Contains("R") ? "Line-R" : line.Contains("Q") ? "Line-Q" : line;
            var query =
                $@"SELECT TOP (100) PERCENT AliasName_3.ObjectName AS Setup, dbo.AliasName.ObjectName AS RecipeName, AliasName_4.ObjectName AS Line, dbo.CComponentPlacement.bstrRefDesignator AS RefDes, 
                      AliasName_2.ObjectName AS PN, AliasName_1.ObjectName AS Station, dbo.CHeadSchedule.lHeadIndex AS Location, dbo.CPickupLink.lTrack AS Track, dbo.CPickupLink.lReserve AS Division, 
                      dbo.CPickupLink.lTower AS Tower, dbo.CPickupLink.lLevel AS [Level], dbo.CComponent.nValidationMode
                      FROM dbo.CRecipe INNER JOIN
                      dbo.AliasName ON dbo.CRecipe.OID = dbo.AliasName.PID INNER JOIN
                      dbo.CHeadSchedule ON dbo.CRecipe.OID = dbo.CHeadSchedule.PID INNER JOIN
                      dbo.AliasName AS AliasName_1 ON dbo.CHeadSchedule.spStation = AliasName_1.PID INNER JOIN
                      dbo.CHeadStep ON dbo.CHeadSchedule.OID = dbo.CHeadStep.PID INNER JOIN
                      dbo.CPickupLink ON dbo.CRecipe.OID = dbo.CPickupLink.PID AND dbo.CHeadStep.lPickupLink = dbo.CPickupLink.lIndex INNER JOIN
                      dbo.CPlacementLink ON dbo.CRecipe.OID = dbo.CPlacementLink.PID AND dbo.CHeadStep.lPlacementLink = dbo.CPlacementLink.lIndex INNER JOIN
                      dbo.CComponentPlacement ON dbo.CPlacementLink.spComponentPlacement = dbo.CComponentPlacement.OID INNER JOIN
                      dbo.AliasName AS AliasName_3 ON dbo.CRecipe.spSetupRef = AliasName_3.PID INNER JOIN
                      dbo.CSetup ON dbo.CRecipe.spSetupRef = dbo.CSetup.OID INNER JOIN
                      dbo.AliasName AS AliasName_4 ON dbo.CSetup.spLineRef = AliasName_4.PID INNER JOIN
                      dbo.CComponent ON dbo.CPickupLink.spComponentRef = dbo.CComponent.OID INNER JOIN
                      dbo.AliasName AS AliasName_2 ON dbo.CComponent.OID = AliasName_2.PID
                      WHERE (dbo.AliasName.ObjectName = N'{recipe}') AND (AliasName_4.ObjectName = N'{line}') AND (dbo.CComponent.nValidationMode = 3)
                      ORDER BY PN";

            var sql = new SqlClass();
            var d = sql.SelectDb(query, out var result);

            if (result != null)
                ErrorOut("At GetRecipe: " + result);

            return d;
        }

        private DataTable CompareRecipeDualLine(DataTable FullDT, string line, string recipe)
        {
            var query = $@"SELECT DISTINCT Board_a.ObjectName AS Board,  CComponentPlacement_1.bstrRefDesignator AS RefDes, AliasName_2.ObjectName AS Part
                         FROM dbo.AliasName AliasName_2 INNER JOIN
                         dbo.CComponentPlacement CComponentPlacement_1 ON AliasName_2.PID = CComponentPlacement_1.spComponentRef INNER JOIN
                         dbo.AliasName Board_a INNER JOIN
                         dbo.CBoard ON Board_a.PID = dbo.CBoard.OID INNER JOIN
                         dbo.CBoardSide ON dbo.CBoard.OID = dbo.CBoardSide.PID ON CComponentPlacement_1.PID = dbo.CBoardSide.spPlacementListRef
                         WHERE (Board_a.ObjectName LIKE '%{recipe}%') AND (CComponentPlacement_1.bOmit = 0) 
                         UNION
                         SELECT DISTINCT Board_a.ObjectName AS Board, dbo.CComponentPlacement.bstrRefDesignator AS Symbol, AliasName_1.ObjectName AS Part
                         FROM dbo.AliasName Board_a INNER JOIN
                         dbo.CBoard ON Board_a.PID = dbo.CBoard.OID INNER JOIN
                         dbo.CBoardSide ON dbo.CBoard.OID = dbo.CBoardSide.PID INNER JOIN
                         dbo.CPanelMatrix ON dbo.CBoardSide.OID = dbo.CPanelMatrix.PID INNER JOIN
                         dbo.CPanel ON dbo.CPanelMatrix.OID = dbo.CPanel.PID INNER JOIN
                         dbo.CComponentPlacement ON dbo.CPanel.spPlacementListRef = dbo.CComponentPlacement.PID INNER JOIN
                         dbo.AliasName AliasName_1 ON dbo.CComponentPlacement.spComponentRef = AliasName_1.PID
                         WHERE (Board_a.ObjectName LIKE '%{recipe}%') AND (dbo.CComponentPlacement.bOmit = 0)";

            var sql = new SqlClass();
            var d = sql.SelectDb(query, out var result);

            if (result != null)
                ErrorOut("At GetRecipe: " + result);

            foreach (DataRow dr in FullDT.Rows)//if DefRes in d table, delete it from FullDT table
            {
                var pn = dr["RefDes"].ToString().Trim();
                bool contains = d.AsEnumerable().Any(row => pn == row.Field<String>("RefDes"));
                if (contains == false)
                {
                    dr.Delete();
                }
            }
            FullDT.AcceptChanges();
            return FullDT;
        }

        private void RegisterRecipe(string board, string line)
        {
            try
            {
                var sql = new SqlClass("trace");
                string query = string.Format(@"SELECT * FROM [Current] where line = '{0}'", line);
                DataTable DT = sql.SelectDb(query, out var res);

                query = string.Format(@"SELECT top 1 Wo FROM Insert_Qty where Line='{0}' order by PassTime desc", line);
                var sql2 = new SqlClass("LMS_DATA");
                DataTable DT2 = sql2.SelectDb(query, out var res2);
                string WO_LMS_DATA = "";
                if (DT2.Rows.Count > 0)
                    WO_LMS_DATA = DT2.Rows[0].IsNull("Wo") ? "" : DT2.Rows[0]["Wo"].ToString().Trim();

                if (DT.Rows.Count > 0)
                {
                    string WO_Current = DT.Rows[0].IsNull("wo") ? "" : DT.Rows[0]["wo"].ToString().Trim();

                    if (board != DT.Rows[0]["receipe"].ToString().Trim() || WO_LMS_DATA != WO_Current)
                    {
                        query = string.Format(@"UPDATE [Current] SET receipe = '{1}', tm = '{2}', Change_Manual=0, wo='{3}' where line = '{0}'", line, board, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), WO_LMS_DATA);
                        sql.Insert(query);
                    }
                }
                else
                {
                    query = string.Format(@"Insert INTO [Current] (line, receipe, tm, Boards, Finish_Boards, Change_Manual,wo) VALUES('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}')",
                                            line, board, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 0, 0, 0, WO_LMS_DATA);
                    sql.Insert(query);
                }
            }
            catch (SqlException ex)
            {
                ErrorOut("At RegisterReceipe: " + ex.Message);
            }
        }

        private void CleanRecipeLine(string line)
        {
            var sql = new SqlClass("trace");
            var query = string.Format("DELETE FROM RecipeList WHERE line='{0}'", line);
            try
            {
                sql.Update(query);
            }
            catch (SqlException ex)
            {
                ErrorOut("At CleanReceipeLine: " + ex.Message);
            }
        }

        private void WriteRecipeToDbLine(DataTable d, string line)
        {
            var sql = new SqlClass("trace");

            foreach (DataRow r in d.Rows)
            {
                var pn = r["PN"].ToString().Trim();
                var station = r["Station"].ToString();
                var rf = r["RefDes"].ToString();
                var loc = r["Location"].ToString();
                var track = r["Track"].ToString();
                var div = r["Division"].ToString();
                var tower = r["Tower"].ToString();
                var level = r["Level"].ToString();

                var query = string.Format(@"INSERT INTO RecipeList ([line],[station],[pn],[rf],[loc],[track],[div],[tower],[lvl]) 
                         VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')", line, station, pn, rf, LocationDic[loc], track, DivisionDic[div], tower, level);
                try
                {
                    sql.Insert(query);
                }
                catch (SqlException ex)
                {
                    ErrorOut("At WriteReceipeToDBLine: " + ex.Message);
                }
            }
        }
    }
}