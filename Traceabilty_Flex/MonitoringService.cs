using Asm.As.Oib.Common.Utilities;
using Asm.As.Oib.DisplayService.Contracts.Data;
using Asm.As.Oib.DisplayService.Contracts.Data.Types;
using Asm.As.Oib.Monitoring.Proxy.Architecture.Objects;
using Asm.As.Oib.Monitoring.Proxy.Business.EventArgs;
using Asm.As.Oib.SiplacePro.Proxy.Business.Objects;
using Asm.As.Oib.Monitoring.Proxy.Business.Types;
using Asm.As.Oib.WS.Eventing.Contracts.Data;
using Asm.As.Oib.WS.Eventing.Contracts.Messages;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using TraceabilityTestGui;
using www.siplace.com.OIB._2008._05.ServiceLocator.Contracts.Data;
using www.siplace.com.OIB._2008._05.ServiceLocator.Contracts.Service;
using System.Windows.Controls;
using System.Text;
using System.Threading.Tasks;
using Asm.As.Oib.SiplacePro.Optimizer.Proxy.Business.Objects;
using Asm.As.Oib.SiplacePro.Optimizer.Contracts;
using Asm.As.Oib.SiplacePro.Optimizer.Contracts.Data;
using System.Reflection;

namespace Traceabilty_Flex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region ServiceLocatorEndpoint
        private string ServiceLocatorEndpoint
        {
            get
            {
                Uri subscriptionmanager = new Uri(SubscriptionMonitorManagerEndpoint);
                UriBuilder serviceLocator = new UriBuilder("http://mignt048:1405/Asm.As.oib.servicelocator")
                {
                    Host = subscriptionmanager.Host
                };
          //      serviceLocator.Host = subscriptionmanager.Host;
                return serviceLocator.ToString();
            }
        }
        #endregion

        #region NewExpiryDate
        private DateTime NewExpiryDate
        {
            get { return DateTime.UtcNow + TimeSpan.FromDays(365); }
        }
        #endregion

        #region ConnectToCore
        private void ConnectToCore()
        {
            try
            {
                #region DOC_MONITORING_CREATE_SUBSCRIBER

             //   if (_optimizer == null) _optimizer = new Optimizer();
                    
                string spSM = SubscriptionMonitorManagerEndpoint;
                _subscriber = new Subscriber(new EndpointAddress(spSM), EndpointHelper.CreateBindingFromEndpointString(spSM, false, false));

                #endregion

                #region DOC_MONITORING_LAST_SUBSCRIPTION_ID


                // See if there is still a subscription with this ID.
                // If yes, then renew it...
                SubscriptionDescriptor search = new SubscriptionDescriptor();
                //                    search.Id = lastSubsciptionId;
                Subscription[] foundSubscriptions = _subscriber.GetSubscriptions(search);
                if (foundSubscriptions != null && foundSubscriptions.GetLength(0) > 0)
                {
                    // Check that the subscription that we found is using the same callback
                    Uri foundCallbackUri = foundSubscriptions[0].Delivery.EndpointSerialiazable.ToEndpointAddress().Uri;
                    //if (foundCallbackUri.ToString() != MonitorCallbackEndpoint)
                    //{
                    //    //ErrorOut(string.Format("A subscription with Identifier:{0} was found in Eventing which uses a different callback than specified! Please manually delete the old subscription!.", lastSubsciptionId));
                    //}
                    //else
                    //{
                        _currentSubscription = foundSubscriptions[0];
                    //}
                }
                #endregion
                #region DOC_MONITORING_LAST_SUBSCRIPTION_CALLBACK
                //else
                //{
                //    // Alternative 2, use the callback URI and the topic to get the existing subscription
                //    SubscriptionDescriptor search = new SubscriptionDescriptor();
                //    search.Topic = _subscriber.DefaultTopic;
                //    search.Endpoint = EndpointAddressAugust2004.FromEndpointAddress(new EndpointAddress(MonitorCallbackEndpoint));
                //    Subscription[] foundSubscriptions = _subscriber.GetSubscriptions(search);
                //    if (foundSubscriptions != null && foundSubscriptions.GetLength(0) > 0)
                //    {
                //        _currentSubscription = foundSubscriptions[0];
                //    }
                //}
                #endregion

                //int lastPortNumber = LastPortNumber;
                //if (lastPortNumber != -1)
                //{
                //    _numericUpDownPort.Value = lastPortNumber;
                //}

                StartReceiver();

                #region DOC_MONITORING_SUBSCRIPTION_FOUND
                if (_currentSubscription != null)
                {
                    RenewSubscription();
                    _timer.Enabled = true;
                }

                #endregion
                else
                {
                    CreateSubscription();
                }
                //foreach (string monitoringAdapterEP in GetMonitoringAdapters())
                //{
                //    AddMessage(monitoringAdapterEP);
                //}
            }
            catch (Exception ex)
            {
                ErrorOut("Error happened when connecting to core: " + ex);
                Environment.Exit(2);
            }

        }
        #endregion

        #region DOC_MONITORING_CREATE_SUBSCRIPTION
        private void CreateSubscription()
        {
            if (_currentSubscription == null)
            {
                SubscribeResult result;
                // We need to create a new subscription
                //if (_checkBoxFilterLine.Checked)
                //{
                //    XPathFilterAdapter filterAdapter = new XPathFilterAdapter(XPathFilterDataType.LineFullPath, _textBoxLineFullPath.Text);
                //    result = _subscriber.Subscribe(filterAdapter, _receiver.CallbackEndpointString, NewExpiryDate, DeliveryModeType.PushWithAck);
                //}
                //else
                try
                {
                    result = _subscriber.Subscribe(_receiver.CallbackEndpointString, NewExpiryDate, DeliveryModeType.PushWithAck);

                    //LastSubscriptionId = result.SubscriptionManager.Identifier.ToString();
                    SubscriptionDescriptor search = new SubscriptionDescriptor()
                    {
                        Id = result.SubscriptionManager.Identifier.ToString()
                    };
               //     search.Id = result.SubscriptionManager.Identifier.ToString();
                    Subscription[] foundSubscriptions = _subscriber.GetSubscriptions(search);
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
        #endregion

        #region DOC_MONITORING_STOP_RECEIVER
        private void StopReceiver()
        {
            if (_receiver != null)
            {
                // Unsubscribe from events
                _receiver.RecipeChangeEventReceived -= ReceiverOnRecipeChangeEventReceived;
                _receiver.RecipeDownloadEventReceived -= ReceiverOnRecipeDownloadEventReceived;
                //_receiver.StationEventReceived -= _receiver_StationEventReceived;
                _receiver.BoardProcessedEventReceived -= _receiver_BoardProcessedEventReceived;
                _receiver.Dispose();
                _receiver = null;
            }
        }
        #endregion

        #region TimerOnElapsed
        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            try
            {
                _timer.Enabled = false;
                // Renew the subscription
                RenewSubscription();
            }
            catch (Exception ex)
            {
                AddMessage("Got exception when renwing the subscription: " + ex);
            }
            finally
            {
                _timer.Enabled = true;
            }
        }

        private void DeleteSubscription()
        {
            if (_currentSubscription != null)
            {
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
        }
        #endregion

        #region GetMonitoringAdapters
        private List<string> GetMonitoringAdapters()
        {
            List<string> ret = new List<string>();
            try
            {
                ServiceLocatorClient slClient = new ServiceLocatorClient(EndpointHelper.CreateBindingFromEndpointString(ServiceLocatorEndpoint, false, false), new EndpointAddress(ServiceLocatorEndpoint));

                ServiceMatchCriteria criteria = new ServiceMatchCriteria { ServiceName = "SIPLACE.Monitoring" };
                ServiceDescription[] monitoringDescriptions = slClient.FindServices(criteria);

                foreach (ServiceDescription description in monitoringDescriptions)
                {
                    // This is a quick and dirty shortcut since we only use the computer name here and then 
                    // use a hard-coded endpoint pattern for the reliable tcp asm endpoint
                    if (description.MetadataEndpoints.Length > 0)
                    {
                        string s = description.MetadataEndpoints[0].ToString();
                        string line = s.Substring(7, 5);
                        string ip = LineDic[line];
                        string ns = "http://" + ip + ":1405/Siemens.Siplace.Oib.Monitoring.Services.Architecture.Services/SiplaceMonitoringService/mexSubscribe";
                        Uri nUri = new Uri(ns, UriKind.Absolute);

                        Uri metadataEndpoint = nUri;// description.MetadataEndpoints[4];
                        UriBuilder adapter = new UriBuilder("http://mignt048:1405/Asm.As.Oib.Monitoring.Services.Architecture.Services/SiplaceMonitoringService/Reliable")
                        {
                            Host = metadataEndpoint.Host
                        };
                 //       adapter.Host = metadataEndpoint.Host;
                        ret.Add(adapter.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOut("ERROR: While getting registered MonitoringAdapters from Service Locator: " + ex.Message);
            }
            return ret;
        }
        #endregion

        #region DOC_MONITORING_START_RECEIVER
        private void StartReceiver()
        {
            if (_receiver == null)
            {
                try
                {
                    _receiver = new ReliableReceiver(MonitorCallbackEndpoint, false);
                    _receiver.RecipeChangeEventReceived += ReceiverOnRecipeChangeEventReceived;
                    _receiver.RecipeDownloadEventReceived += ReceiverOnRecipeDownloadEventReceived;
                    _receiver.StationEventReceived += _receiver_StationEventReceived;
                    _receiver.BoardProcessedEventReceived += _receiver_BoardProcessedEventReceived;
                }
                catch (Exception ex)
                {
                    ErrorOut(ex.Message);
                    Environment.Exit(3);
                }
            }
        }

        private void _receiver_BoardProcessedEventReceived(object sender, BoardProcessedEventArgs args)
        {
            try
            {
                if(_placement)
                {
                    AddMessage("BoardProcessed: " +
                        args.BoardProcessedData.ProcessedBoards[0].Barcode + " " +
                        args.BoardProcessedData.ProcessedBoards[0].BoardName
                        , listBoxPallets);
                }

                int cnt = 0;
                int placed_total = 0;
                if (args == null || 
                    args.BoardProcessedData == null || 
                    args.BoardProcessedData.UsedDetails == null ||
                    args.BoardProcessedData == null ||
                    args.BoardProcessedData.ProcessedBoards == null ||
                    args.BoardProcessedData.ProcessedBoards.Count == 0) return;
                string CurrentLine = args.BoardProcessedData.Recipe.LineName;
                CurrentLine = GetLeaf(CurrentLine);
                CurrentLine = CurrentLine.Remove(0, 5);
                foreach (var detail in args.BoardProcessedData.UsedDetails)
                {
                    if (detail == null || detail.TrackEntry == null) continue;

                    int consumed = detail.AccessTotal - detail.TrackEmpty;
                    int placed = consumed - detail.RejectIdent - detail.RejectVacuum;
                    placed_total = placed_total + placed;
                    int missed = consumed - placed;
                    string componentName = detail.TrackEntry.ComponentName ?? string.Empty;
                    string station = args.BoardProcessedData.Station.Name ?? string.Empty;
                    string track = station + "_" + detail.TrackEntry.TableLocation + "_" + detail.TrackEntry.Track;

                    DateTime timestamp = args.BoardProcessedData.ProcessedBoards[0].StationTime;

                    short empty = detail.TrackEmpty;
                    short vacuum = detail.RejectVacuum;
                    short ident = detail.RejectIdent;
                    Comp cp = new Comp()
                    {
                        Date = timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                        Id = args.BoardProcessedData.Station.MachineId,
                        Barcode = args.BoardProcessedData.ProcessedBoards[0].Barcode ?? args.BoardProcessedData.ProcessedBoards[0].BoardNumber.ToString(), // board id!!!
                        Program = args.BoardProcessedData.ProcessedBoards[0].BoardName ?? string.Empty, // board 
                        CompName = componentName,
                        Station = station,
                        Line = args.BoardProcessedData.Recipe.LineName,
                        Recipe = args.BoardProcessedData.Recipe.RecipeName ?? string.Empty,
                        TableLocation = detail.TrackEntry.TableLocation,
                        Track = detail.TrackEntry.Track,
                        Division = detail.TrackEntry.Division,
                        Level = detail.TrackEntry.Level,
                        Tower = detail.TrackEntry.Tower,
                        AccessTotal = detail.AccessTotal,
                        Consumed = consumed,
                        Placed = placed,
                        Missed = missed,
                        Empty = empty,
                        Ident = ident,
                        Vacuum = vacuum,
                        TrackID = 0,
                        Shape = detail.TrackEntry.ComponentShapeName ?? string.Empty
                    };
                    listComp.Add(cp);

                    if (CurrentLine == "E"  || CurrentLine == "F" || CurrentLine == "D" )
                    {
                        listComPilot.Add(cp);
                        //  Task task = Task.Run(() => SendToService(listComPilot));// send to db by thread


                        //    Object thisLock = new Object();

                        //       lock (thisLock)
                        //         {
                        if (_mainservice)
                            SendToService(listComPilot);
                        //       }

                             listComPilot.Clear();
                    }
                    //open only one of them
                    //  Task task = Task.Run(() => SendToDB(cp));// send to db by thread
                    // SendToDB(cp);// send to db

                    if (_placement)
                    {
                        AddMessage("consumed: " + consumed.ToString() + " , placed: " + placed.ToString()
                            + ", component: " + componentName + ", track: " + track, listBoxPlacements);
                        //AddMessage(timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"), listBoxPlacements);

                        //               Errors
                        if (empty != 0 || vacuum != 0 || ident != 0)
                        {
                            AddMessage("error: " + " empty = " + empty.ToString() + ", vacuum = " + vacuum.ToString() + ", ident = " + ident.ToString() + " " + ", component: " + componentName
                                + ", track: " + track + " : " + args.BoardProcessedData.ProcessedBoards[0].Barcode + " : " + args.BoardProcessedData.ProcessedBoards[0].BoardName, listBoxErrors);
                            //                AddMessage(timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"), listBoxErrors);
                        }

                        AddMessage(textBoxPLaceTime, cp.Date);
                        AddMessage(textBoxPlacePN, cp.CompName);
                        AddMessage(textBoxPlaceStation, cp.Station);
                        AddMessage(textBoxPlaceLine, cp.Line.Substring(7));
                        AddMessage(textBoxPlaceRecipe, cp.Recipe);
                    }

                    //          if (listComPilot.Count >= 10 && _mainservice)
                    //{
                    //    SendToService(listComPilot);
                    //    listComPilot.Clear();

                    //}
                    if (listComp.Count >= LIM) // how much messages to send, it collects 50 messages, when it comes to 50 it sends as a package
                    {
                        //if (checkBoxPlacement.Checked)
                        if (_mainservice)
                        {
                            //if (!DEBUG)
                            {
                                try
                                {
                                    //open only one of them
                                    //Task task = Task.Run(() => SendToService(listComp)); // send to qms by thread
                                    //SendToService(listComp);//send to qms



                                    AddMessage(textBoxTime, DateTime.Now.ToString());
                                    //AddMessage(textBoxSent, listComp.Count.ToString());
                                }
                                catch (Exception ex)
                                {
                                    ErrorOut("Inside of SentToService(Placements).\t" + ex.Message);
                                }
                            }
                        }
                     listComp.Clear();
                        
                    }

                    if (cnt == 0)
                    {
                        string stN = args.BoardProcessedData.Station.Name;

                        if (stN.StartsWith("Sipl1"))
                        {
                            if (_placement)
                            {
                                AddMessage(textBoxBCRecipe, cp.Recipe);
                                AddMessage(textBoxBCStation, cp.Station);
                                AddMessage(textBoxBCTime, cp.Date);
                                AddMessage(textBoxBCLine, cp.Line.Substring(7));
                                AddMessage(textBoxBC, cp.Barcode);
                            }
                            //if (checkBoxSend.Checked)
                            //if(_mainservice)
                            //{
                            //    try
                            //    {
                            //        //open only one of them
                            //        // Task task = Task.Run(() => SendToService2(cp)); // send to qms by thread
                            //      //  SendToService2(cp); // send to qms



                            //    }
                            //    catch (Exception ex)
                            //    {
                            //        ErrorOut("Inside of SentToService(Barcode).\t" + ex.Message);
                            //    }
                            //}
                        }
                    }
                    cnt++;
                }
                string Barcode;
                bool b = string.IsNullOrEmpty(args.BoardProcessedData.ProcessedBoards[0].Barcode);
                if (!b)
                    Barcode = args.BoardProcessedData.ProcessedBoards[0].Barcode;
                else
                    Barcode = args.BoardProcessedData.ProcessedBoards[0].BoardNumber.ToString();
                string stationid = args.BoardProcessedData.Station.MachineId;
                //             string pallet = args.BoardProcessedData.ProcessedBoards[0].Barcode.ToString();
                stationid = stationid.Trim('0');
                int panelcount = args.BoardProcessedData.ProcessedBoards[0].PanelCount;
                string program = args.BoardProcessedData.ProcessedBoards[0].BoardName;
                string line = args.BoardProcessedData.Recipe.LineName;

                if (placed_total < 0)
                    placed_total = 0;
                if (_mainservice)
                    SendStationToQMS(line.Substring(7), stationid, Barcode, placed_total, program, panelcount);



            }
            catch (Exception ex)
            {
                string errorline = GetAllFootprints(ex);
                ErrorOut("_receiver_BoardProcessedEventReceived\t" + errorline + ": " + ex.Message);
            }
        }

        private void SendToService(List<Comp> list)
        {
    //        string address = "http://webdemo1.migux105/qms3/web_services/ws_json.php";
            string address = "http://10.229.8.35/qms3/web_services/ws_json.php";
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(address);
            httpWebRequest.ContentType = "text/json";
            httpWebRequest.Method = "POST";
            string json = "";

            string s = string.Empty;

            int n = list.Count;

            foreach (Comp cp in list)
            {
                n--;
                string sTemp = "{\"lId\":\"" + cp.Id.ToString().Trim('0') + "\"," +
                                   "\"dtTime\":\"" + cp.Date + "\"," +
                                   "\"sAccessTotal\":\"" + cp.AccessTotal.ToString() + "\"," +
                                   "\"sRejectIdent\":\"" + cp.Ident.ToString() + "\"," +
                                   "\"sRejectVacuum\":\"" + cp.Vacuum.ToString() + "\"," +
                                   "\"ucTable\":\"" + cp.TableLocation.ToString() + "\"," +
                                   "\"sTrack\":\"" + cp.Track.ToString() + "\"," +
                                   "\"ucTower\":\"" + cp.Tower.ToString() + "\"," +
                                   "\"sLevel\":\"" + cp.Level.ToString() + "\"," +
                                   "\"sReceptacle\":\"" + cp.Division.ToString() + "\"," +
                                   "\"lBoardNumber\":\"" + cp.Barcode + "\"," +
                                   "\"strPartNumber\":\"" + cp.CompName + "\"," +
                                   "\"lIdTrack\":\"" + cp.TrackID + "\"," +
                                   "\"strLine\":\"" + cp.Line.Substring(7) + "\"," +
                                   "\"strStation\":\"" + cp.Station + "\"," +
                                   "\"strRecipe\":\"" + cp.Recipe + "\"," +
                                   "\"strComponentShape\":\"" + cp.Shape + "\"}";
                if (n != 0)
                    s = s + sTemp + ",";
                else
                    s = s + sTemp;
            }

            json = @"{""data"": {""rows"":"
                                + "[" + s + "]"
                                + @"},""base"": {""flex_user_code"":""A014"",""password"":""$Flex2099"",""customer_code"":""0000"",""function_name"":""lms3_oib_insert_reject""}}";

            //Newtonsoft.Json.Linq.JObject jobject = Newtonsoft.Json.Linq.JObject.Parse(json);
            //            using (StreamWriter streamWriter = new StreamWriter(@"C:\Tmp\qmstest2.txt", true))

          using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                //    new LogWriter("the following message recieved :");
                streamWriter.Write(json);
                }

            try
            {
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var text = streamReader.ReadToEnd();
                    new LogWriter(json + "\n" + text,"QMS");

                    if (text.Contains("error"))
                    {
                        int ix = text.IndexOf("error");
                        string error = text.Substring(ix);
                        error = error.Replace("{", " ");
                        error = error.Replace("}", " ");
                        error = error.Remove(error.Length - 18);
                        //   error=error.Replace("/"/", " ");
                        Utils.SendMail("OIB insert/ reject  error! QMS sending data", DateTime.Now.ToLongTimeString() + " : OIB insert/ reject error! QMS sending data,  " + error);
                    }



                    //if (text.IndexOf("OK") == -1)
                    //{
                    //    new LogWriter(json + "\n" + text);
                    //  //  Utils.SendMail("error occurs", "error occurs while sending to qms\n" + DateTime.Now.ToLongTimeString());
                    //}
                }
            }
            catch (Exception ex)
            {
                string lineerror = GetAllFootprints(ex);
                WriteLog("SendToService\t" + lineerror + " " + ex.Message);
                //MessageBox.Show(ex.Message + "Inside of HTTP Response.");
            }

            _count = _count + list.Count;

      //      _time = true;

            AddMessage( textBoxSent , _count.ToString());
        }

        private void SendToService2(Comp cp)
        {
            //string address = "http://webdemo1.migux105/qms3/web_services/ws_json.php";
            string address = "http://10.229.8.35/qms3/web_services/ws_json.php";
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(address);
            httpWebRequest.ContentType = "text/json";
            httpWebRequest.Method = "POST";
            string json = "";
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                json = @"{""data"": {""result"":""OK"", ""rows"":" +
                                "[{" +
                                "\"dtTime\":\"" + cp.Date + "\"," +
                                "\"pallet_id\":\"" + cp.Barcode + "\"," +
                                "\"strLine\":\"" + cp.Line.Substring(7) + "\"," +
                                "\"strStation\":\"" + cp.Station + "\"," +
                                "\"strRecipe\":\"" + cp.Recipe + "\"," +
                                "\"lId\":\"" + cp.Id.ToString() +
                                "\"}]"
                                +
                                 @"},""base"": {""flex_user_code"":""A014"",""password"":""$Flex2099"",""customer_code"":""0143"",""function_name"":""oib_pallet_pass_in_station"",""success"":true}}";

                //Newtonsoft.Json.Linq.JObject jobject = Newtonsoft.Json.Linq.JObject.Parse(json);

                streamWriter.Write(json);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var text = streamReader.ReadToEnd();
                if (text.IndexOf("OK") == -1)
                {
                    new LogWriter(json + "\n" + text,"QMS");
                }
            }
        }
        #endregion

        private void _receiver_StationEventReceived(object sender, StationEventArgs args)
        {
            try
            {
                string stn = args.StationEventComposite.Station.Name;
                string stpa = args.StationEventComposite.ProcessingArea.ToString();

                StationEventType stationEvent = args.StationEventComposite.StationEventType;
               // StationStateType stationEvent2 = args.StationEventComposite.StationStateType;

                if (stationEvent == StationEventType.PCBBegin || stationEvent == StationEventType.PCBEnd)
                {
                    if (_placement)
                    {
                        string sa = stationEvent.ToString();
                        AddMessage("Station: " + stn + " Area: " + stpa + " Event: " + sa + " : " + args.StationEventComposite.BoardNumber, listBoxEvents);
                    }
                }
                //if ( stationEvent == StationEventType.WaitPCBIn || stationEvent == StationEventType.WaitPCBOut)
                //{
                //    string sa = stationEvent.ToString();
                //    AddMessage("Station: " + stn + " Area: " + stpa + " Event: " + sa + " : " + args.StationEventComposite.BoardNumber, listBoxEvents);
                //}
            }
            catch(Exception ex)
            {
                ErrorOut("At StationEventReceived: " + ex.Message);
            }
        }

       #region RenewSubscription
        private void RenewSubscription()
        {
            if (_currentSubscription != null)
            {
                // We should renew the subscription (make it expire 365 day from now)
 //               DateTime expiresDate = NewExpiryDate;
                RenewRequest renew = new RenewRequest(_currentSubscription.Manager.Identifier, new Renew(new Expires(NewExpiryDate)));
                _subscriber.Renew(renew);
                _currentSubscription.Expires = NewExpiryDate;
                //LastSubscriptionId = _currentSubscription.Manager.Identifier.ToString();
            }
        }

        private void SendToDB(Comp cp)
        {
            string sTemp = "{\"lId\":\"" + cp.Id.ToString() + "\"," +
                                   "\"dtTime\":\"" + cp.Date + "\"," +
                                   "\"sAccessTotal\":\"" + cp.AccessTotal.ToString() + "\"," +
                                   "\"sRejectIdent\":\"" + cp.Ident.ToString() + "\"," +
                                   "\"sRejectVacuum\":\"" + cp.Vacuum.ToString() + "\"," +
                                   "\"ucTable\":\"" + cp.TableLocation.ToString() + "\"," +
                                   "\"sTrack\":\"" + cp.Track.ToString() + "\"," +
                                   "\"ucTower\":\"" + cp.Tower.ToString() + "\"," +
                                   "\"sLevel\":\"" + cp.Level.ToString() + "\"," +
                                   "\"sReceptacle\":\"" + cp.Division.ToString() + "\"," +
                                   "\"lBoardNumber\":\"" + cp.Barcode + "\"," +
                                   "\"strPartNumber\":\"" + cp.CompName + "\"," +
                                   "\"lIdTrack\":\"" + cp.TrackID + "\"," +
                                   "\"strLine\":\"" + cp.Line.Substring(7) + "\"," +
                                   "\"strStation\":\"" + cp.Station + "\"," +
                                   "\"strRecipe\":\"" + cp.Recipe + "\"," +
                                   "\"strComponentShape\":\"" + cp.Shape + "\"}";

            SQLClass sql = new SQLClass();

            string query = string.Format("INSERT INTO dbo.history (row, time) VALUES('{0}','{1}')", sTemp, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            sql.Insert(query);
        }

        #endregion

        #region ReceiverOnRecipeXXXEventReceived
        private void ReceiverOnRecipeDownloadEventReceived(object sender, DownloadRecipeEventArgs args)
        {
            try
            {
                if (args.DownloadRecipe.Recipe.StationName.Contains("Sipl1"))
                {
                    string recipe = args.DownloadRecipe.Recipe.RecipeName;
                    string setup = args.DownloadRecipe.Recipe.SetupName;
                    string line = args.DownloadRecipe.Recipe.LineName;
                    TimeSpan cycletime = args.DownloadRecipe.Recipe.CycleTime;
                    TimeSpan cycletime2 = args.DownloadRecipe.Recipe.StationCycleTime;
                    string test2 = test.PCBCycle.ToString();
                    //LocationPcbCycle test = new LocationPcbCycle();   
                        DataTable d =null;
                    AddMessage(DateTime.Now.ToString("HH:mm:ss") + "  RecipeDownloaded: " + " Recipe: " + recipe + " Setup: " + setup + " Line: " + line);

                    if (_mainservice)
                    {
                       DataTable dRecipe = FillOneRecipe(new string[] { setup, recipe, line });

                        //open only one of them
                        //Task task = Task.Run(() => SendToQMSProgram(GetLeaf(line), recipe, setup, dRecipe));
                        SendToQMSProgramNew(GetLeaf(line), recipe, setup, dRecipe); // Open!!!
                    }
                    else
                    {

                        d = GetRecipe(GetLeaf(recipe), GetLeaf(line));
                        SetFirstLastInLine(d, GetLeaf(line));
                    //    SendToQMSProgramNew(GetLeaf(line), recipe, setup, d); // trusted

                    }

                    FillRecipeDT();
                }
            }
            catch(Exception ex)
            {
                ErrorOut("At ReceiverOnRecipeDownloadEventReceived: " + ex.Message);
            }
        }

        public struct QMS
        {
            public string line;
            public string recipe;
            public string setup;

        }

        private void SendToQMSProgram(string line, string recipe, string setup, DataTable dRecipe)
        {
            //string lineletter = line.Remove(0, 5);
            //if (lineletter != "E")
            //    return;
            ////       st = new Station("System\\" + line + "\\" + recipe,)
            //if (_optimizer == null) _optimizer = new Optimizer();

            // //       RecipeDataPCBCycle test = new RecipeDataPCBCycle();
            ////         test.Performance();
            //Dictionary<string, string[]> stationlist = new Dictionary<string, string[]>();
            //RecipeDataRecipe RecipeData = _optimizer.GetOptimizerResultsForRecipe("System\\" + line + "\\" + recipe);
            //SQLClass sql2 = null;
            //DataRow[] dr2 = DTActiveLines.Select("Line = '" + line + "'");
            //if (dr2 != null && dr2.Length > 0)
            //{
            //    string path = dr2[0]["DBPath"].ToString();
            //    sql2 = new SQLClass("line")
            //    {
            //        Server = path
            //    };
            //    sql2.ConnectionString = sql2.ConnectionString.Replace("Data Source=", "Data Source =" + path);

            //}
            ////    var allProps = cb.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderBy(pi => pi.Name).ToList();

            //try
            //{
            //    foreach (RecipeDataStation Stations in RecipeData.Setup.Line.Stations)
            //    {
            //        int cyclemax = 0;
            //        foreach (RecipeDataPlacementArea placemnts in Stations.PlacementAreas)
            //        {
            //            if (placemnts.CycleTime > cyclemax)
            //                cyclemax = placemnts.CycleTime;

            //        }
            //        string stationname = GetLeaf(Stations.Name);
            //        string query2 = string.Format(@"SELECT [strMid] FROM[SiplaceSIS].[dbo].[RECIPE] where strStation = '{0}'", stationname);
            //        DataTable dr3 = sql2.SelectDB(query2, out string Result);
            //        string stationid = dr3.Rows[0]["strMid"].ToString().TrimStart('0');

            //        stationlist.Add(stationname, new string[] { stationid.ToString(), cyclemax.ToString() });
            //    }
            //}
            //catch (Exception ex)
            //{
            //    WriteLog("SendToService\t" + " " + ex.Message);

            //}
            //line = line.Remove(0, 5);

            //string s1 = "{\"" + line + "\":" +
            //               "{\"line\":\"" + line + "\"," +
            //               "\"recipe\":\"" + recipe + "\"," +
            //               "\"setup\":\"" + setup + "\"," +
            //               "\"machines\":{";

            //StringBuilder s2 = new StringBuilder();


            //foreach (KeyValuePair<string, string[]> entry in stationlist)
            //{
            //    string machineid = entry.Value[0];
            //    int cycle = Convert.ToInt32(entry.Value[1]);
            //    string station = entry.Key;

            //    int placements = dRecipe == null ? 0 : dRecipe.Select("Station = '" + station + "'").Length;
            //    int max_boards = cycle == 0 ? 0 : (int)(3600 / (cycle / 1000)); // can be calculated
            //                                                                    //double max_pph = cycle == 0 ? 0 : (int)((placements * 3600) / (cycle / 1000));
            //    double max_pph = placements == 0 ? 0 : (int)(3600 / ((cycle / 1000) / placements));

            //    string s = "\"" + machineid + "\":{\"placement_positions\":\"" + placements + "\"," +
            //                    "\"max_pph\":\"" + max_pph + "\"," +
            //                    "\"max_boards_qty_in_hour\":\"" + max_boards + "\"," +
            //                    "\"placement_time\":\"" + cycle + "\"},";
            //    s2.Append(s);


            //}

            //string s3 = s2.ToString();
            //string str = s1 + s3.Substring(0, s2.Length - 1) + "}";

            //string address = "http://10.229.8.35/qms3/web_services/ws_json.php";
            //var httpWebRequest = (HttpWebRequest)WebRequest.Create(address);
            //httpWebRequest.ContentType = "text/json";
            //httpWebRequest.Method = "POST";
            //string json = "";
            //// using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            //using (StreamWriter streamWriter = new StreamWriter(@"C:\Tmp\qmstest5.txt", true))
            //{
            //    json = @"{""data"":"
            //            + str +
            //            @"}},""base"": {""flex_user_code"":""A014"",""password"":""$Flex2099"",""customer_code"":""0000"",""function_name"":""lms3_oib_new_batch""}}";

            //    streamWriter.Write(json);
            //}

            //try
            //{
            //    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            //    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            //    {
            //        var text = streamReader.ReadToEnd();
            //        //   if (text.IndexOf("OK") == -1)
            //        new LogWriter(json + "\n" + text);

            //        if (text.Contains("error"))
            //        {
            //            int ix = text.IndexOf("error");
            //            string error = text.Substring(ix);
            //            error = error.Replace("{", " ");
            //            error = error.Replace("}", " ");
            //            error = error.Remove(error.Length - 18);
            //            //   error=error.Replace("/"/", " ");
            //            Utils.SendMail("new batch error while sending QMS data", DateTime.Now.ToLongTimeString() + " : new batch error in recipe:  " + recipe + "  :   " + error);
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{

            //    string lineerror = GetAllFootprints(ex);
            //    WriteLog("SendToService\t" + lineerror + " " + ex.Message);
            //}

            string lineletter = line.Remove(0, 5);
            if (lineletter != "E" || lineletter != "F")
                return;
            // setup = setup.Substring(setup.IndexOf("(") + 1, setup.LastIndexOf(")"));
            DataRow[] dr = DTActiveLines.Select("Line = '" + line + "'");
            if (dr != null && dr.Length > 0)
            {
                string path = dr[0]["DBPath"].ToString();
                SQLClass sql = new SQLClass("line")
                {
                    Server = path
                };
                sql.ConnectionString = sql.ConnectionString.Replace("Data Source=", "Data Source =" + path);

                string query = @"select siplaceSIS.dbo.RECIPE.strStation as station, dbo.recipe.lStationCycleTime as cycle_time, dbo.recipe.strMid as id from SiplaceSIS.dbo.RECIPE INNER JOIN SiplaceOIS.dbo.STATION 
                                ON SiplaceSIS.dbo.RECIPE.strStation = SiplaceOIS.dbo.STATION.strStation 
                                where dtEnd > getdate()
                                order by station";


                DataTable d = sql.SelectDB(query, out string Result);

                if (d.Rows.Count > 0)
                {
                    line = line.Remove(0, 5);

                    string s1 = "{\"" + line + "\":" +
                                   "{\"line\":\"" + line + "\"," +
                                   "\"recipe\":\"" + recipe + "\"," +
                                   "\"setup\":\"" + setup + "\"," +
                                   "\"machines\":{";

                    StringBuilder s2 = new StringBuilder();

                    for (int i = 0; i < d.Rows.Count; i++)
                    {
                        string id = d.Rows[i]["id"].ToString();
                        id = id.TrimStart('0');

                        double cycle = Convert.ToInt32(d.Rows[i]["cycle_time"]);
                        string station = d.Rows[i]["station"].ToString().Trim();
                        int placements = dRecipe == null ? 0 : dRecipe.Select("Station = '" + station + "'").Length;
                        int max_boards = cycle == 0 ? 0 : (int)(3600 / (cycle / 1000)); // can be calculated
                        //double max_pph = cycle == 0 ? 0 : (int)((placements * 3600) / (cycle / 1000));
                        double max_pph = cycle == 0 ? 0 : (int)(3600 / ((cycle / 1000) / placements));

                        string s = "\"" + id + "\":{\"placement_positions\":\"" + placements + "\"," +
                                        "\"max_pph\":\"" + max_pph + "\"," +
                                        "\"max_boards_qty_in_hour\":\"" + max_boards + "\"," +
                                        "\"placement_time\":\"" + cycle + "\"},";
                        s2.Append(s);
                    }



                    string s3 = s2.ToString();
                    string str = s1 + s3.Substring(0, s2.Length - 1) + "}";

                    string address = "http://10.229.8.35/qms3/web_services/ws_json.php";
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(address);
                    httpWebRequest.ContentType = "text/json";
                    httpWebRequest.Method = "POST";
                    string json = "";
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
  //                  using (StreamWriter streamWriter = new StreamWriter(@"C:\Tmp\qmstest5555.txt", true))
                    {
                        json = @"{""data"":"
                                + str +
                                @"}},""base"": {""flex_user_code"":""A014"",""password"":""$Flex2099"",""customer_code"":""0000"",""function_name"":""lms3_oib_new_batch""}}";


                        streamWriter.Write(json);
                    }

                    try
                    {
                        var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            var text = streamReader.ReadToEnd();
                            //   if (text.IndexOf("OK") == -1)
                            new LogWriter(json + "\n" + text,"QMS");

                            if (text.Contains("error"))
                            {
                                int ix = text.IndexOf("error");
                                string error = text.Substring(ix);
                                error = error.Replace("{", " ");
                                error = error.Replace("}", " ");
                                error = error.Remove(error.Length - 18);
                                //   error=error.Replace("/"/", " ");
                                Utils.SendMail("new batch error while sending QMS data", DateTime.Now.ToLongTimeString() + " : new batch error in recipe: \n\n  " + recipe + "  :   " + error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                        string lineerror = GetAllFootprints(ex);
                        WriteLog("SendToService\t" + lineerror + " " + ex.Message);
                    }
                }
            }
        }

        private void SendToQMSProgramNew(string line, string recipe, string setup, DataTable dRecipe)
        {

            string lineletter = line.Remove(0, 5);
            if (lineletter != "E" && lineletter != "F" && lineletter != "D" )
                return;
            //       st = new Station("System\\" + line + "\\" + recipe,)
            if (_optimizer == null )
                _optimizer = new Optimizer();
           
            //       RecipeDataPCBCycle test = new RecipeDataPCBCycle();
            //         test.Performance();
            Dictionary<string, string[]> stationlist = new Dictionary<string, string[]>();
            RecipeDataRecipe RecipeData = _optimizer.GetOptimizerResultsForRecipe("System\\" + line + "\\" + recipe);
            SQLClass sql2 = null;
            if (DTActiveLines == null)
                GetActiveLines();

            DataRow[] dr2 = DTActiveLines.Select("Line = '" + line + "'"); // get IP of the relevant Line's SQL server
            try
            {
                if (dr2 != null)
                {
                    string path = dr2[0]["DBPath"].ToString();
                    sql2 = new SQLClass("line")
                    {
                        Server = path
                    };
                    sql2.ConnectionString = sql2.ConnectionString.Replace("Data Source=", "Data Source =" + path);

                }
            }
            catch (Exception ex)
            {
                WriteLog("SendToService\t" + " cannot connect to Line's SQL Server" + ex.Message);

            }

            //    var allProps = cb.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderBy(pi => pi.Name).ToList();

            try
            {
                foreach (RecipeDataStation Stations in RecipeData.Setup.Line.Stations)
                {
                    int cyclemax = 0;
                    foreach (RecipeDataPlacementArea placemnts in Stations.PlacementAreas)
                    {
                        if (placemnts.CycleTime > cyclemax)
                            cyclemax = placemnts.CycleTime;

                    }
                    string stationname = GetLeaf(Stations.Name);
                    string query2 = string.Format(@"SELECT top(1) [strMid] FROM [SiplaceSIS].[dbo].[RECIPE] where strStation = '{0}' and dtStart > '2018-01-01' and dtEnd > GetDate()", stationname);
                    DataTable dr3 = sql2.SelectDB(query2, out string Result);
                    string stationid = dr3.Rows[0]["strMid"].ToString().TrimStart('0');

                    stationlist.Add(stationname, new string[] { stationid.ToString(), cyclemax.ToString() });
                }
            }
            catch (Exception ex)
            {
                WriteLog("SendToService\t" + " " + ex.Message);

            }
            line = line.Remove(0, 5);

            string s1 = "{\"" + line + "\":" +
                           "{\"line\":\"" + line + "\"," +
                           "\"recipe\":\"" + recipe + "\"," +
                           "\"setup\":\"" + setup + "\"," +
                           "\"machines\":{";

            StringBuilder s2 = new StringBuilder();


            foreach (KeyValuePair<string, string[]> entry in stationlist)
            {
                string machineid = entry.Value[0];
                int cycle = Convert.ToInt32(entry.Value[1]);
                string station = entry.Key;

                int placements = dRecipe == null ? 0 : dRecipe.Select("Station = '" + station + "'").Length;
                int max_boards = cycle == 0 ? 0 : (int)(3600 / (cycle / 1000)); // can be calculated
                double max_pph = cycle == 0 ? 0 : (int)((placements * 3600) / (cycle / 1000));
            //    double max_pph = placements == 0 ? 0 : (int)(3600 / ((cycle / 1000) / placements));

                string s = "\"" + machineid + "\":{\"placement_positions\":\"" + placements + "\"," +
                                "\"max_pph\":\"" + max_pph + "\"," +
                                "\"max_boards_qty_in_hour\":\"" + max_boards + "\"," +
                                "\"placement_time\":\"" + cycle + "\"},";
                s2.Append(s);


            }

            string s3 = s2.ToString();
            string str = s1 + s3.Substring(0, s2.Length - 1) + "}";

            string address = "http://10.229.8.35/qms3/web_services/ws_json.php";
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(address);
            httpWebRequest.ContentType = "text/json";
            httpWebRequest.Method = "POST";
            string json = "";
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
     //       using (StreamWriter streamWriter = new StreamWriter(@"C:\Tmp\qmstest5.txt", true))
            {
                json = @"{""data"":"
                        + str +
                        @"}},""base"": {""flex_user_code"":""A014"",""password"":""$Flex2099"",""customer_code"":""0000"",""function_name"":""lms3_oib_new_batch""}}";
                
                streamWriter.Write(json);
            }

            try
            {
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var text = streamReader.ReadToEnd();
                    //   if (text.IndexOf("OK") == -1)
                    new LogWriter(json + "\n" + text,"QMS");

                    if (text.Contains("error"))
                    {
                        int ix = text.IndexOf("error");
                        string error = text.Substring(ix);
                        error = error.Replace("{", " ");
                        error = error.Replace("}", " ");
                        error = error.Remove(error.Length - 18);
                        //   error=error.Replace("/"/", " ");
                        Utils.SendMail("new batch error while sending QMS data", DateTime.Now.ToLongTimeString() + " : new batch error in recipe:  " + recipe + "  :   " + error);
                    }
                }
            }
            catch (Exception ex)
            {

                string lineerror = GetAllFootprints(ex);
                WriteLog("SendToService\t" + lineerror + " " + ex.Message);
            }



        }
        private DataTable GetDTFromDBRecipe(string line)
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

        private void ReceiverOnRecipeChangeEventReceived(object sender, RecipeChangeEventArgs args)
        {
            try
            {
                string recipe = args.RecipeChange.Recipe.RecipeName;
                string setup = args.RecipeChange.Recipe.SetupName;
                string line = args.RecipeChange.Recipe.LineName;
          //      TimeSpan cycletime = args.RecipeChange.Recipe.CycleTime;
           //     TimeSpan cycletime2 = args.RecipeChange.Recipe.StationCycleTime;
                DataTable d=null;
                AddMessage(DateTime.Now.ToString("HH:mm:ss") + "  RecipeChanged: " + " Recipe: " + recipe + " Setup: " + setup + " Line: " + line);

                if (_mainservice)
                {
                    DataTable dRecipe = FillOneRecipe(new string[] { setup, recipe, line });

                    //open only one of them
                    //Task task = Task.Run(() => SendToQMSProgram(GetLeaf(line), recipe, setup, dRecipe));
                    SendToQMSProgramNew(GetLeaf(line), recipe, setup, dRecipe); // always open
                }
                else
                {
                    d = GetRecipe(GetLeaf(recipe), GetLeaf(line));
                    SetFirstLastInLine(d, GetLeaf(line));
                  //  SendToQMSProgramNew(GetLeaf(line), recipe, setup, d);

                }
             //   if ((GetLeaf(line) == "line-E"))
     

                FillRecipeDT();
            }
            catch (Exception ex)
            {
                ErrorOut("At ReceiverOnRecipeChangeEventReceived: " + ex.Message);
            }
        }
        #endregion

        #region EmergencyStopMethod
        internal void EmergencyStopMethod(string line, List<string> list, List<string[]> str, string recipe, string cause, bool db)
        {
            string s = "";
            if (list != null)
            {

                for (int i = 0; i < list.Count; i++)
                {
                    s = s + list[i] + ", ";
                }
            }

            string message = cause +": " + s;
            ErrorOut(message + " adam check is - " + db.ToString());
            if (db)
            {
                try
                {

                    StopAdamLine(line, message.Length > 999 ? message.Substring(0,999) : message);
                    //SendToDisplay(line, message);
                    if (str != null)
                        WriteToDB(line, str, recipe, cause);

                    Utils.SendMail(Utils.GetJoinedList("trace", "select eMail from [Users] where [Admin] = '20'", ';', out string Result)    
                       , ""
                       , "Traceability Monitor Error"
                       , line + ": " + message);

                    Utils.WriteLog(message);


                }
                catch(Exception ex)
                {
                    ErrorOut(ex.Message);
                }
            }
        }
        #endregion

        #region WriteToDB
        private void WriteToDB(string line, List<string[]> list, string recipe, string cause)
        {
            string query = "";
            //string query2 = "";
            //string complist = "";
            SQLClass sql = new SQLClass("LtsMonitor");

            //for (int i = 0; i < list.Count; i++) 
            //    complist = complist + list[i][2] + '|';

            //complist = complist.Trim().Replace(" ", "");
            //complist.Remove(ComboList.Length - 1);


            //try
            //{
            //    if (cause.Length > 200)
            //        cause = cause.Substring(0, 199);
            //    query2 = string.Format("INSERT INTO [TrcErrors].[dbo].[QMSLog] (Line, Board, Barcode, Part, dtCreated, Message) VALUES('{0}', '{1}','{2}', '{3}', '{4}', '{5}')",
            //                line, recipe, list[0][0], complist, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), cause);

            //    sql.Insert(query2);
            //}
            //catch (Exception ex)
            //{
            //    ErrorOut("At WriteToDB: " + ex.Message + ' ' + query2);
            //}
            for (int i = 0; i < list.Count; i++)
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
        #endregion

            #region SendToDisplay
            private void SendToDisplay(string line, string s)
        {
            line = "System\\" + line;
            List<ViewerRegistration> list = _client.GetAllViewerRegistrations();

            if (list.Count == 0)
            {
                ErrorOut(@"There are no viewers registered currently, cancelling the send!");
                return;
            }
            try
            {
                _client.SendMessageExplicit(list, s, "", AcknowledgementType.Originator, false, 0, SeverityLevel.Warning);
            }
            catch(Exception ex)
            {
                ErrorOut("At SendToDisplay" + ex.Message);
            }
        }
        #endregion

        #region CheckSetup
        private bool CheckSetup(string client, string setup, string line, out List<string> list, out List<string[]> strList)
        {
            list = new List<string>();
            strList = new List<string[]>();

            if (CustomerList.Length > 0)
            {
                bool cust = false;
                for (int i = 0; i < CustomerList.Length; i++)
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

         string  makat = "";

            string query = string.Format(@"SELECT TOP(100) PERCENT dbo.PackagingUnit.Batch AS Batch, dbo.PackagingUnit.PackagingUnitId AS Unic, dbo.PackagingUnit.SiplaceProComponent as Comp,
                         dbo.TableInUse.Line, dbo.TableInUse.Station, dbo.TableInUse.Location, dbo.FeederInUse.Track, dbo.PackagingUnitInUse.Division, dbo.PackagingUnitInUse.[Level] AS Lvl, 
                         dbo.PackagingUnitInUse.Tower
                         FROM dbo.TableInUse INNER JOIN
                         dbo.FeederInUse ON dbo.TableInUse.TableInventoryRef = dbo.FeederInUse.TableInventoryRef INNER JOIN
                         dbo.PackagingUnitInUse ON dbo.FeederInUse.FeederInventoryRef = dbo.PackagingUnitInUse.FeederInventoryRef INNER JOIN
                         dbo.PackagingUnit ON dbo.PackagingUnitInUse.PackagingUnitRef = dbo.PackagingUnit.PackagingUnitId
                         WHERE (LEN(dbo.TableInUse.Line) = 13) AND(dbo.TableInUse.Setup LIKE N'%{0}')", setup);

            SQLClass sql = new SQLClass("setup");

            DataTable dt = sql.SelectDB(query, out string result);
            if (result != null)
                ErrorOut("At CheckSetup: " + result);

            foreach (DataRow dr in dt.Rows)
            {
                string u = dr["Unic"].ToString().Trim();
                string b = dr["Batch"].ToString().Trim();
                string comp = dr["Comp"].ToString();
                string station = dr["Station"].ToString();
                string loc = dr["Location"].ToString();
                string div = dr["Division"].ToString();
                string tower = dr["Tower"].ToString();
                string lvl = dr["Lvl"].ToString();
                string track = dr["Track"].ToString();

                //if (u.StartsWith("DMY"))
                //    u = b;


                bool is_skid = (bool)DTActiveLines.Select("Line = '" + line + "'")[0]["skid"];

                if (is_skid == false && !Regex.IsMatch(u, _patUnitID))
                {
                    if (tower == "0")
                    //if (!Regex.IsMatch(b, _patBatch) && !Regex.IsMatch(b, _patUnitID))
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

                        string[] star = new string[] { u, b, comp, station, loc, div, tower, lvl, track };
                        strList.Add(star);

                        WriteLog("wrong unique id(unique tower == 0!) - " + makat);
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

                            string[] star = new string[] { u, b, comp, station, loc, div, tower, lvl, track };
                            strList.Add(star);

                            WriteLog("wrong unique id(unique tower == 0!) - " + makat);



                        }

                    }
                }
                else if (is_skid == true && !Regex.IsMatch(u, _patBatch))
                    if (tower == "0")
                    //if (!Regex.IsMatch(b, _patBatch) && !Regex.IsMatch(b, _patUnitID))
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

                        string[] star = new string[] { u, b, comp, station, loc, div, tower, lvl, track };
                        strList.Add(star);

                        WriteLog("wrong unique id(unique tower == 0!) - " + makat);


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

                            string[] star = new string[] { u, b, comp, station, loc, div, tower, lvl, track };
                            strList.Add(star);

                            WriteLog("wrong unique id(skid is true, tower > 0!) - " + makat);


                        }

                    }
            }

                return list.Count == 0;
        }
        #endregion

        #region CheckIfExistInRecipe
        private bool CheckIfExistInRecipe(string comp, string line)
        {
            SQLClass sql = new SQLClass("trace");

            string query = string.Format("SELECT * FROM [Traceability].[dbo].[{0}] where [pn] = '{1}'", line.Replace("Line-","Receipe_"), comp);

            DataTable d = sql.SelectDB(query, out string result);
            if (result != null)
                ErrorOut(result);
            if (d.Rows.Count > 0)
                return true;

            return false;
        }
        #endregion

        #region GetClient
        private string GetClient(string recipe)
        {
            string query = string.Format(@"SELECT DISTINCT dbo.CFolder.bstrDisplayName AS Client
                FROM dbo.AliasName INNER JOIN dbo.CFolder ON dbo.AliasName.FolderID = dbo.CFolder.OID
                WHERE(dbo.AliasName.ObjectName LIKE N'%{0}%') AND(NOT(dbo.CFolder.bstrDisplayName LIKE N'%line-%'))", recipe);

            SQLClass sql = new SQLClass();

            DataTable d = sql.SelectDB(query, out string result);
            if (result != null)
                ErrorOut("At GetClient: " + result);
            if(d.Rows.Count > 0)
                return d.Rows[0]["Client"].ToString();
            ErrorOut("Client for recipe: " + recipe + " no founded.");
            return "";
        }
        #endregion


        private void SendStationToQMS(string line, string stationID, string pallet, int comp_place, string boardname, int panel_count)
        {
           // if (panel_count == 0)
           // panel_count = GetCardsQty(boardname);
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            int board_qty = 1;
            int sub_event = 2;
            line = line.Remove(0, 5);
            if (line != "E" && line != "F" && line != "D")
                return;
            string s = "{\"line\":\"" + line + "\"," +
                                    "\"machine\":" + Convert.ToInt32(stationID) + "," +
                                    "\"sub_event\":" + Convert.ToInt32(sub_event) + "," +
                                    "\"pass_time\":\"" + time + "\"," +
                                    "\"boards_qty\":" + Convert.ToInt32(board_qty) + "," +
                                    "\"cards_qty\":" + panel_count + "," +
                                    "\"comp_place\":" + Convert.ToInt32(comp_place) + "," +
                                    "\"pallet_id\":\"" + pallet + "\"}";

            string json = json = @"{""data"": {""rows"":"
               + "[" + s + "]"
               + @"},""base"": {""flex_user_code"":""A014"",""password"":""$Flex2099"",""customer_code"":""0000"",""function_name"":""lms3_oib_insert_qty""}}";

            string address = "http://10.229.8.35/qms3/web_services/ws_json.php";
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(address);
            httpWebRequest.ContentType = "text/json";
            httpWebRequest.Method = "POST";

                  using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            //using (StreamWriter streamWriter = new StreamWriter(@"C:\Tmp\qmstest6.txt", true))
            {
                streamWriter.Write(json);
            }

            try
            {
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var text = streamReader.ReadToEnd();
                    new LogWriter(json + "\n" + text,"QMS");

                    if (text.Contains("error"))
                    {
                        int ix = text.IndexOf("error");
                        string error = text.Substring(ix);
                        error = error.Replace("{", " ");
                        error = error.Replace("}", " ");
                        error = error.Remove(error.Length - 18);
                        //   error=error.Replace("/"/", " ");
                       // Utils.SendMail("OIB insert qunatity error! QMS sending data", DateTime.Now.ToLongTimeString() + " : OIB insert qunatity error! QMS sending data,  " + error);
                    }



                }
            }
            catch (Exception ex)
            {
                string lineeror = GetAllFootprints(ex);
                MainWindow.WriteLog("SendToService\t" + lineeror + " " + ex.Message);
            }
        }

        public int GetCardsQty(string boardname)
        {

            SQLClass sql = new SQLClass();

            string query = string.Format(@"SELECT TOP (100) PERCENT dbo.AliasName.ObjectName AS boardname, dbo.CPanelNameCol.CPNameCollection_CComBSTR as cards_qty
FROM            dbo.CBoardSide INNER JOIN
                        dbo.CPanelNameCol INNER JOIN
                        dbo.CPanelMatrix ON dbo.CPanelNameCol.PID = dbo.CPanelMatrix.OID ON dbo.CBoardSide.OID = dbo.CPanelMatrix.PID RIGHT OUTER JOIN
                        dbo.CBoard INNER JOIN
                        dbo.AliasName ON dbo.CBoard.OID = dbo.AliasName.PID ON dbo.CBoardSide.PID = dbo.CBoard.OID
WHERE        (dbo.AliasName.ObjectName like '{0}')
GROUP BY dbo.AliasName.ObjectName, dbo.CPanelNameCol.CPNameCollection_CComBSTR, dbo.CPanelMatrix.CPanelCollection_CComBSTR", boardname);
            DataTable d = sql.SelectDB(query, out string Result);
            int minAccountLevel = int.MaxValue;
            int maxAccountLevel = int.MinValue;
            if (d.Rows.Count > 0)
            {
                foreach (DataRow dr in d.Rows)
                {
                    string accountLevel = dr.Field<string>("cards_qty");
              //      maxAccountLevel = Math.Max(maxAccountLevel, accountLevel);
                }


            }
            return maxAccountLevel;
        }


    }
}