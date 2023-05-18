using Asm.As.Oib.Common.Utilities;
using Asm.As.Oib.Monitoring.Proxy.Architecture.Objects;
using Asm.As.Oib.Monitoring.Proxy.Business.EventArgs;
using Asm.As.Oib.WS.Eventing.Contracts.Data;
using Asm.As.Oib.WS.Eventing.Contracts.Messages;
using System;
using System.Collections.Generic;
using System.Data;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Windows;

namespace Traceabilty_Flex
{
    public partial class MainWindow : Window
    {
        #region Subscription
        private string ServiceLocatorEndpoint
        {
            get
            {
                var subscriptionManager = new Uri(SubscriptionMonitorManagerEndpoint);
                var serviceLocator = new UriBuilder("http://smtoib:1405/Asm.As.oib.servicelocator")
                {
                    Host = subscriptionManager.Host
                };
                return serviceLocator.ToString();
            }
        }
        private DateTime NewExpiryDate => DateTime.UtcNow + TimeSpan.FromDays(365);

        private void ConnectToCore()
        {
            try
            {
                var endpointAddress = SubscriptionMonitorManagerEndpoint;
                _subscriber = new Subscriber(new EndpointAddress(endpointAddress), EndpointHelper.CreateBindingFromEndpointString(endpointAddress, false, false));

                // See if there is still a subscription with this ID. If yes, then renew it...
                var search = new SubscriptionDescriptor();
                var foundSubscriptions = _subscriber.GetSubscriptions(search);
                if (foundSubscriptions != null && foundSubscriptions.GetLength(0) > 0)
                {
                    // Check that the subscription that we found is using the same callback
                    var foundCallbackUri = foundSubscriptions[0].Delivery.EndpointSerialiazable.ToEndpointAddress().Uri;
                    _currentSubscription = foundSubscriptions[0];
                }

                StartReceiver(); // Starting Receiver Eventing

                if (_currentSubscription != null)
                {
                    RenewSubscription();
                    _timer.Enabled = true;
                }

                else
                {
                    CreateSubscription();
                }
            }
            catch (Exception ex)
            {
                ErrorOut("Error happened when connecting to core: " + ex);
                Environment.Exit(2);
            }
        }

        private void CreateSubscription()
        {
            if (_currentSubscription == null)
            {
                SubscribeResult result;
                try
                {
                    result = _subscriber.Subscribe(_receiver.CallbackEndpointString, NewExpiryDate, DeliveryModeType.PushWithAck);
                    var search = new SubscriptionDescriptor()
                    {
                        Id = result.SubscriptionManager.Identifier.ToString()
                    };

                    var foundSubscriptions = _subscriber.GetSubscriptions(search);
                    if (foundSubscriptions != null && foundSubscriptions.GetLength(0) > 0)
                    {
                        _currentSubscription = foundSubscriptions[0];
                    }
                }
                catch (Exception ex)
                {
                    ErrorOut(ex.Message);
                    Environment.Exit(4);
                }
            }
        }

        private void StopReceiver()
        {
            if (_receiver == null) return;
            // Unsubscribe from events
            _receiver.Dispose();
            _receiver = null;
        }

        private void DeleteSubscription()
        {
            if (_currentSubscription == null) return;
            try
            {
                _subscriber.Unsubscribe(_currentSubscription.Manager.Identifier, _subscriber.DefaultTopic);
            }
            catch (Exception ex)
            {
                // Maybe the subscription was manually deleted?
                ErrorOut("Unsubscribe got exception: " + ex.Message);
            }
            _currentSubscription = null;
        }

        private void StartReceiver()
        {
            if (_receiver != null) return;
            try
            {
                _receiver = new ReliableReceiver(MonitorCallbackEndpoint, false);

                // Events
                _receiver.RecipeChangeEventReceived += RecipeChangeEventReceived;
                _receiver.RecipeDownloadEventReceived += RecipeDownloadEventReceived;
            }
            catch (Exception ex)
            {
                ErrorOut(ex.Message);
                Environment.Exit(3);
            }
        }

        private void RenewSubscription()
        {
            if (_currentSubscription != null)
            {
                // We should renew the subscription (make it expire 365 day from now) 
                var renew = new RenewRequest(_currentSubscription.Manager.Identifier, new Renew(new Expires(NewExpiryDate)));
                _subscriber.Renew(renew);
                _currentSubscription.Expires = NewExpiryDate;
            }
        }
        #endregion

        #region Receiver Events
        private void RecipeChangeEventReceived(object sender, RecipeChangeEventArgs args)
        {
            try
            {
                var recipe = args.RecipeChange.Recipe.RecipeName;
                var WO = args.RecipeChange.Recipe.OrderId;
                var setup = args.RecipeChange.Recipe.SetupName;
                var line = args.RecipeChange.Recipe.LineName;
                var station = args.RecipeChange.Recipe.StationName;
                var time = args.RecipeChange.StationTime;
                string Conveyor = args.RecipeChange.Recipe.Conveyor.ToString();
                if (line.Contains("Line-R") || line.Contains("Line-S")|| line.Contains("Line-Q"))
                {
                    line = GetLineSide(line, Conveyor);
                    recipe = args.RecipeChange.Recipe.BoardName;
                }


                AddMessage(time.ToString("HH:mm:ss") + "  RecipeChanged: " + " Recipe: " + recipe + " Setup: " + setup + " Line: " + line + " Station: " + station);
                new LogWriter(time.ToString("HH:mm:ss") + "  RecipeChanged: " + " Recipe: " + recipe + " Setup: " + setup + " Line: " + line + " Station: " + station, "RecipeEvents");

                if (_mainservice)
                {
                    FillOneRecipe(new string[] { setup, recipe, line, WO });
                }
                FillRecipeDt();
            }
            catch (Exception ex)
            {
                ErrorOut("At RecipeChangeEventReceived: " + ex.Message);
            }
        }

        private void RecipeDownloadEventReceived(object sender, DownloadRecipeEventArgs args)
        {
            try
            {
                if (args.DownloadRecipe.Recipe.StationName.Contains(@"Sipl1"))
                {
                    var recipeName = args.DownloadRecipe.Recipe.RecipeName;
                    var WO = args.DownloadRecipe.Recipe.OrderId;
                    var setupName = args.DownloadRecipe.Recipe.SetupName;
                    var lineName = args.DownloadRecipe.Recipe.LineName;
                    var station = args.DownloadRecipe.Recipe.StationName;
                    var time = args.DownloadRecipe.DownloadTime;
                    string Conveyor = args.DownloadRecipe.Recipe.Conveyor.ToString();
                    if (lineName.Contains("Line-R") || lineName.Contains("Line-S")|| lineName.Contains("Line-Q"))
                    {
                        lineName = GetLineSide(lineName, Conveyor);
                        recipeName = args.DownloadRecipe.Recipe.BoardName;
                    }

                    AddMessage(time.ToString("HH:mm:ss") + "  RecipeDownloaded: " + " Recipe: " + recipeName + " Setup: " + setupName + " Line: " + lineName);
                    new LogWriter(time.ToString("HH:mm:ss") + "  RecipeDownloaded: " + " Recipe: " + recipeName + " Setup: " + setupName + " Line: " + lineName, "RecipeEvents");

                    FillOneRecipe(new string[] { setupName, recipeName, lineName, WO });
                    FillRecipeDt();
                }
            }
            catch (Exception ex)
            {
                ErrorOut("At RecipeDownloadEventReceived: " + ex.Message);
            }
        }
        #endregion

        #region Functions
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
            if (line.Contains("Line-Q"))// Line Q Convayer
            {
                if (Convayer == "Right")
                    return "Line-Q1";
                else if (Convayer == "Left")
                    return "Line-Q2";
            }
            return "";
        }

        internal void EmergencyStopMethod(string line, List<string> list, List<string[]> str, string recipe, string cause, bool active, string Lane, string boardSide, string board)
        {
            var s = "";
            if (list != null)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    s = s + list[i] + ", ";
                }
            }

            var message = cause + ": " + s;
            ErrorOut(message + " adam check is - " + active.ToString());

            try
            {
                if (active)
                {
                    StopAdamLine(line, message);
                    if (str != null)
                        WriteToDb(line, str, recipe, cause);
                }

                string sendMsg = message.Length > 999 ? message.Substring(0, 999) : message;
                LogWriter.SendMail("", "", "Traceability Monitor Error", line + ": " + sendMsg);
                LogWriter.WriteLog(message);
            }
            catch (Exception ex)
            {
                ErrorOut(ex.Message);
            }
        }

        private void WriteToDb(string line, List<string[]> list, string recipe, string cause)
        {
            var query = "";
            var sql = new SqlClass("LtsMonitor");
            for (var i = 0; i < list.Count; i++)
            {
                try
                {
                    if (cause.Length > 200)
                        cause = cause.Substring(0, 199);

                    query = string.Format("INSERT INTO [TrcErrors].[dbo].[QMSLog] (Line, Board, Barcode, Part, dtCreated, Message) VALUES('{0}', '{1}','{2}', '{3}', '{4}', '{5}')",
                        line, recipe, list[i][0], list[i][2], DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), cause);

                    sql.Insert(query);
                }
                catch (Exception ex)
                {
                    ErrorOut("At WriteToDB: " + ex.Message + ' ' + query);
                }
            }
        }

        private bool CheckSetup(string client, string setup, string line, out List<string> list, out List<string[]> strList)
        {
            list = new List<string>();
            strList = new List<string[]>();

            if (line == "Line-S1")
                line = "Line-S1";

            if (CustomerList.Length > 0)//if client is in CustomerList return true.
            {
                var cust = false;
                for (var i = 0; i < CustomerList.Length; i++)
                {
                    if (client == CustomerList[i])
                    {
                        cust = true;
                        break;
                    }
                }
                if (!cust)
                    return true;
            }

            var makat = "";

            var query = string.Format(@"SELECT TOP(100) PERCENT dbo.PackagingUnit.Batch AS Batch, dbo.PackagingUnit.PackagingUnitId AS Unic, dbo.PackagingUnit.SiplaceProComponent as Comp,
                         dbo.TableInUse.Line, dbo.TableInUse.Station, dbo.TableInUse.Location, dbo.FeederInUse.Track, dbo.PackagingUnitInUse.Division, dbo.PackagingUnitInUse.[Level] AS Lvl, 
                         dbo.PackagingUnitInUse.Tower
                         FROM dbo.TableInUse INNER JOIN
                         dbo.FeederInUse ON dbo.TableInUse.TableInventoryRef = dbo.FeederInUse.TableInventoryRef INNER JOIN
                         dbo.PackagingUnitInUse ON dbo.FeederInUse.FeederInventoryRef = dbo.PackagingUnitInUse.FeederInventoryRef INNER JOIN
                         dbo.PackagingUnit ON dbo.PackagingUnitInUse.PackagingUnitRef = dbo.PackagingUnit.PackagingUnitId
                         WHERE (LEN(dbo.TableInUse.Line) = 13) AND(dbo.TableInUse.Setup LIKE N'%{0}')", setup);

            var sql = new SqlClass("setup");

            var dt = sql.SelectDb(query, out var result);
            if (result != null)
                ErrorOut("At CheckSetup: " + result);

            foreach (DataRow dr in dt.Rows)
            {
                var u = dr["Unic"].ToString().Trim();
                var b = dr["Batch"].ToString().Trim();
                var comp = dr["Comp"].ToString();
                var station = dr["Station"].ToString();
                var loc = dr["Location"].ToString();
                var div = dr["Division"].ToString();
                var tower = dr["Tower"].ToString();
                var lvl = dr["Lvl"].ToString();
                var track = dr["Track"].ToString();

                var isSkid = true;

                if (isSkid == false && !Regex.IsMatch(u, _patUnitID))
                {
                    if (tower == "0")
                    {
                        if (!CheckIfExistInRecipe(comp, line))
                            continue;
                        makat = "Unit ID: " + u + "; "
                            + "Batch: " + b + "; "
                            + "PN: " + comp + "; "
                            + "Station: " + station + "; "
                            + "Location: " + loc + "; "
                            + "Track: " + track + "; "
                            + "Div: " + div + "; "
                            + "Tower: " + tower + "; "
                            + "Level: " + lvl
                            ;
                        list.Add(makat);

                        var star = new string[] { u, b, comp, station, loc, div, tower, lvl, track };
                        strList.Add(star);

                        LogWriter.WriteLog("wrong unique id(unique tower == 0!) - " + makat);
                    }
                    else
                    {
                        if (!Regex.IsMatch(b, _patBatch) && !Regex.IsMatch(b, _patUnitID))
                        {

                            if (!CheckIfExistInRecipe(comp, line))
                                continue;
                            makat = "Unit ID: " + u + "; "
                                + "Batch: " + b + "; "
                                + "PN: " + comp + "; "
                                + "Station: " + station + "; "
                                + "Location: " + loc + "; "
                                + "Track: " + track + "; "
                                + "Div: " + div + "; "
                                + "Tower: " + tower + "; "
                                + "Level: " + lvl
                                ;
                            list.Add(makat);

                            var star = new string[] { u, b, comp, station, loc, div, tower, lvl, track };
                            strList.Add(star);

                            LogWriter.WriteLog("wrong unique id(unique tower == 0!) - " + makat);
                        }
                    }
                }
                else if (isSkid == true && !Regex.IsMatch(u, _patBatch))
                    if (tower == "0")
                    {
                        if (!CheckIfExistInRecipe(comp, line))
                            continue;
                        makat = "Unit ID: " + u + "; "
                            + "Batch: " + b + "; "
                            + "PN: " + comp + "; "
                            + "Station: " + station + "; "
                            + "Location: " + loc + "; "
                            + "Track: " + track + "; "
                            + "Div: " + div + "; "
                            + "Tower: " + tower + "; "
                            + "Level: " + lvl
                            ;
                        list.Add(makat);

                        var star = new string[] { u, b, comp, station, loc, div, tower, lvl, track };
                        strList.Add(star);
                        LogWriter.WriteLog("wrong unique id(unique tower == 0!) - " + makat);
                    }
                    else
                    {
                        if (!Regex.IsMatch(b, _patBatch) && !b.Contains("_"))
                        {

                            if (!CheckIfExistInRecipe(comp, line))
                                continue;
                            makat = "Unit ID: " + u + "; "
                                + "Batch: " + b + "; "
                                + "PN: " + comp + "; "
                                + "Station: " + station + "; "
                                + "Location: " + loc + "; "
                                + "Track: " + track + "; "
                                + "Div: " + div + "; "
                                + "Tower: " + tower + "; "
                                + "Level: " + lvl
                                ;
                            list.Add(makat);

                            var star = new string[] { u, b, comp, station, loc, div, tower, lvl, track };
                            strList.Add(star);
                            LogWriter.WriteLog("wrong unique id(skid is true, tower > 0!) - " + makat);
                        }
                    }
            }
            return list.Count == 0;
        }

        private bool CheckIfExistInRecipe(string comp, string line)
        {
            var sql = new SqlClass("trace");
            var query = string.Format("SELECT * FROM RecipeList WHERE line='{0}' and [pn] = '{1}'", line, comp);

            var d = sql.SelectDb(query, out var result);
            if (result != null)
                ErrorOut(result);
            if (d.Rows.Count > 0)
                return true;

            return false;
        }

        private string GetClient(string recipe)
        {
            var query = string.Format(@"SELECT DISTINCT dbo.CFolder.bstrDisplayName AS Client
                FROM dbo.AliasName INNER JOIN dbo.CFolder ON dbo.AliasName.FolderID = dbo.CFolder.OID
                WHERE(dbo.AliasName.ObjectName LIKE N'%{0}%') AND (NOT(dbo.CFolder.bstrDisplayName LIKE N'%line-%'))
				AND(NOT(dbo.CFolder.bstrDisplayName LIKE N'%(%'))", recipe);

            var sql = new SqlClass();

            var d = sql.SelectDb(query, out var result);
            if (result != null)
                ErrorOut("At GetClient: " + result);
            if (d.Rows.Count > 0)
                return d.Rows[0]["Client"].ToString();
            ErrorOut("Client for recipe: " + recipe + " no founded.");
            return "";
        }

        #endregion
    }
}