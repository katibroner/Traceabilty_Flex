using Asm.As.Oib.SiplacePro.Proxy.Architecture.Objects;
using schemas.xmlsoap.org.ws._2004._08.eventing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using TraceabilityTestGui;

namespace Traceabilty_Flex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region FillRecipes
        private void FillRecipes(List<string[]> recipeSetupList)
        {
           foreach(string[] s in recipeSetupList)
            {
                if (s != null)
                    try
                    {
                        FillOneRecipe(s);
                    }
                    catch(Exception ex)
                    {
                        ErrorOut ("At FillRecipes: " + ex.Message);
                    }
            }
        }
        #endregion

        #region FillOneRecipe
        internal DataTable FillOneRecipe(string[] s)
        {
            string setup = GetLeaf(s[0]);
            string recipe = GetLeaf(s[1]);
            string line = GetLeaf(s[2]);

            RegisterRecipe(recipe, line); // to current
                                          //RecipeDictionary[line] = recipe;
            DataTable d = null; 

            string client = recipe.Length < 10 ? "Unknown" : GetClient(recipe.Substring(0, 10));

            Object thisLock = new Object();
            lock (thisLock)
            {
               d = GetRecipe(recipe, line); // get recipe from sipplace
                CleanRecipeLine(line); // clean from recipe db
                WriteRecipeToDBLine(d, line); // write to recipe db
            }
                SetFirstLastInLine(d, line);

                if (RegisterLineStatus(line, client))  // check client if belongs to customers and register to StatusDictionary[]
                {
                if (!CheckSetup(client, setup, line, out List<string> list, out List<string[]> listar))
                    //TraceAdam(AdamSetup) // (bool)DTActiveLines.Select("Line = '" + line + "'")[0]["Active"]
                    EmergencyStopMethod(line, list, null, recipe, "Wrong Unique ID in Setup:\n",false );

                
                }

                FillRecipeDT();
                return d;
            
        }
        #endregion

        #region LoadNewRecipe
        public void LoadNewRecipe(string line, string recipe)
        {
            RegisterRecipe(recipe, line);
            CleanRecipeLine(line);
            //RecipeDictionary[line] = recipe;
            FillRecipeDT();
            DataTable d = GetRecipe(recipe, line);
            WriteRecipeToDBLine(d, line);

            SetFirstLastInLine(d, line);
        }
        #endregion

        #region SetFirstLastInLine
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

                FlexLine f = FindInCollection(line);

                f.Used = new string[groupedData.Count()];

                int max = 1;
                int min = 1;
                int i = 0;

                foreach (var a in groupedData)
                {
                    string st = a.station;

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
            catch(Exception ex)
            {
                ErrorOut("At SetFirstLastInLine: " + ex.Message);
            }
        }
        #endregion

        #region GetRecipe
        private DataTable GetRecipe(string recipe, string line)
        {
            string query = string.Format(@"SELECT     TOP (100) PERCENT AliasName_3.ObjectName AS Setup, dbo.AliasName.ObjectName AS RecipeName, AliasName_4.ObjectName AS Line, dbo.CComponentPlacement.bstrRefDesignator AS RefDes, 
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
                      WHERE (dbo.AliasName.ObjectName = N'{0}') AND (AliasName_4.ObjectName = N'{1}') AND (dbo.CComponent.nValidationMode = 3)
                      ORDER BY PN", recipe, line);

            SQLClass sql = new SQLClass();

            DataTable d = sql.SelectDB(query, out string result);

            if (result != null)
                    ErrorOut("At GetRecipe: " + result);

            return d;
        }
        #endregion

        #region CheckReceipes
        private bool CheckReceipes(string board, string line)
        {
            SQLClass sql = new SQLClass("trace");

            string query = string.Format("SELECT * FROM [Traceability].[dbo].[Current] WHERE receipe = '{0}' and line = '{1}'", board, line);

            DataTable d = sql.SelectDB(query, out string result);

            if(result != null)
                ErrorOut("At CheckReceipes: " + result);
 
            if (d.Rows.Count > 0)
                return true;

            return false;
        }
        #endregion

        #region RegisterRecipe
        private void RegisterRecipe(string board, string line)
        {
            SQLClass sql = new SQLClass("trace");

            string query =
                string.Format("IF NOT EXISTS(SELECT 1 from [Traceability].[dbo].[Current] where line = '{0}') Insert INTO [Traceability].[dbo].[Current] (line, receipe, tm) VALUES('{0}', '{1}', '{2}') else UPDATE [Traceability].[dbo].[Current] SET receipe = '{1}', tm = '{2}' where line = '{0}'",
                line, board, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            try
            {
                sql.Insert(query);
            }
            catch(SqlException ex)
            {
                ErrorOut("At RegisterReceipe: " + ex.Message);
            }
        }
        #endregion

        #region CleanRecipeLine
        private void CleanRecipeLine(string line)
        {
            SQLClass sql = new SQLClass("trace");

            string query = string.Format("truncate table [Traceability].[dbo].[{0}]", line.Replace("Line-", "Receipe_"));
            try
            {
                sql.Update(query);
            }
            catch(SqlException ex)
            {
                ErrorOut("At CleanReceipeLine: " + ex.Message);
            }
        }
        #endregion

        #region WriteRecipeToDBLine
        private void WriteRecipeToDBLine(DataTable d, string line)
        {
            string lr = line.Replace("Line-", "Receipe_");

            SQLClass sql = new SQLClass("trace");

            foreach (DataRow r in d.Rows)
            {
                string pn = r["PN"].ToString().Trim();

                //if (Array.IndexOf(PartsException, pn.ToUpper()) != -1)
                //    continue;

                string station = r["Station"].ToString();
                string rf = r["RefDes"].ToString();
                string loc = r["Location"].ToString();
                string track = r["Track"].ToString();
                string div = r["Division"].ToString();
                string tower = r["Tower"].ToString();
                string level = r["Level"].ToString();

                string query = string.Format("INSERT INTO [Traceability].[dbo].[{0}] "
                         + "  ([station],[pn],[rf],[loc],[track],[div],[tower],[lvl]) "
                         + "VALUES "
                         + "  (" + "'" + station + "'"
                         + "  ," + "'" + pn + "'"
                         + "  ," + "'" + rf + "'"
                         + "  ," + "'" + LocationDic[loc] + "'"
                         + "  ," + "'" + track + "'"
                         + "  ," + "'" + DivisionDic[div] + "'"
                         + "  ," + "'" + tower + "'"
                         + "  ," + "'" + level + "'"
                         + "  )", lr);
                    try
                    {
                        sql.Insert(query);
                    }
                    catch(SqlException ex)
                    {
                        ErrorOut("At WriteReceipeToDBLine: " + ex.Message);
                    }
            }
        }
        #endregion
    }
}