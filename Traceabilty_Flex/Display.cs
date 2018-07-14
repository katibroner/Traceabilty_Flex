using Asm.As.Oib.DisplayService.Contracts.Messages;
using Asm.As.Oib.DisplayService.Proxy.Architecture.Objects;
using Asm.As.Oib.Monitoring.Proxy.Architecture.Objects;
using Asm.As.Oib.WS.Eventing.Contracts.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TraceabilityTestGui;

namespace Traceabilty_Flex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private void DisplayConnection()
        {
            try
            {
                if (_client != null)
                {
                    _client.Dispose();
                    _client = null;
                }
                    //either use the app config settings (if check box is checked) or use programmatically defined parameters
                _client = new DisplayServiceClient(DisplayHostName, 1406, DisplayClientName, DisplayPort, Guid.Empty, false, 0, true);

                //else
                //{
                //    Guid id = new Guid(_tbClientRegId.Text);
                //    //either use the app config settings (if check box is checked) or use programmatically defined parameters
                //    _client = !_cbAppConfig.Checked
                //                  ? new DisplayServiceClient("mignt048", 1406, "TestClient", 5555, id,
                //                                             false, 0, _cbUnregister.Checked)
                //                  : new DisplayServiceClient(id);
                //}
                InitializeDsClient();
            }
            catch (EndpointNotFoundException)
            {
                ErrorOut("Endpoint not found.");
            }
            catch (Exception ex)
            {
               ErrorOut("DisplayConnection exception " + ex.Message);
            }
        }

        private void InitializeDsClient()
        {
            _client.ServiceConnected += ClientServiceConnected;
            _client.ServiceUnreachable += ClientServiceUnreachable;
            _client.ConfirmationReceived += ClientConfirmationReceived;
        }

        /// <summary>
        /// Handles the display service connected event
        /// </summary>
        private void ClientServiceConnected(string strserviceendpoint, string strcomment)
        {

            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                LabelLineDisplay.Background = Brushes.Green;
            }));


        }

        /// <summary>
        /// Handles the display service unreachable event
        /// </summary>
        private void ClientServiceUnreachable(string strserviceendpoint, string strcomment)
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                LabelLineDisplay.Background = Brushes.Red;
            }));
        }


        #region DOC_CONF_REC
        /// <summary>
        /// Handles the confirmation received event
        /// </summary>
        private void ClientConfirmationReceived(ConfirmationReceivedRequest cRR)
        {
            if (cRR.ThisViewerDeleted)
            {
                ErrorOut(string.Format("The viewer just got unregistered: {0}, MessageId: {1}", cRR.Viewer.ComputerName, cRR.OriginalMessage.MessageGUID));
            }
            else
            {
                if (cRR.SubAnswer != null)
                {
                    MessageOut(
                       string.Format(
                            "Viewer: {0} sent Answer: {1}(AnswerID: {2}), SubAnswer: {3}(SubAnswerID:{4}), MessageId: {5}",
                            cRR.Viewer.ComputerName, cRR.Answer.AnswerText,
                            cRR.Answer.AnswerID, cRR.SubAnswer.SubAnswerText, cRR.SubAnswer.SubAnswerID,
                            cRR.OriginalMessage.MessageGUID));
                }
                else
                {
                    MessageOut(
                        string.Format(
                            "Viewer: {0} sent Answer: {1}(AnswerID: {2}), MessageId: {3}",
                            cRR.Viewer.ComputerName, cRR.Answer.AnswerText,
                            cRR.Answer.AnswerID, cRR.OriginalMessage.MessageGUID));
                }
            }
        }
        #endregion

    }
}