using Asm.As.Oib.DisplayService.Contracts.Messages;
using Asm.As.Oib.DisplayService.Proxy.Architecture.Objects;
using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Media;

namespace Traceabilty_Flex
{
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
                _client = new DisplayServiceClient(DisplayHostName, 1406, DisplayClientName, DisplayPort, Guid.Empty, false, 0, true);
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


        private void ClientServiceConnected(string strserviceendpoint, string strcomment)
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                LabelLineDisplay.Background = Brushes.Green;
            }));
        }

        private void ClientServiceUnreachable(string strserviceendpoint, string strcomment)
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                LabelLineDisplay.Background = Brushes.Red;
            }));
        }


        private void ClientConfirmationReceived(ConfirmationReceivedRequest cRr)
        {
            if (cRr.ThisViewerDeleted)
            {
                ErrorOut(
                    $"The viewer just got unregistered: {cRr.Viewer.ComputerName}, MessageId: {cRr.OriginalMessage.MessageGUID}");
            }
            else
            {
                if (cRr.SubAnswer != null)
                {
                    MessageOut(
                        $"Viewer: {cRr.Viewer.ComputerName} sent Answer: {cRr.Answer.AnswerText}(AnswerID: {cRr.Answer.AnswerID}), SubAnswer: {cRr.SubAnswer.SubAnswerText}(SubAnswerID:{cRr.SubAnswer.SubAnswerID}), MessageId: {cRr.OriginalMessage.MessageGUID}");
                }
                else
                {
                    MessageOut(
                        $"Viewer: {cRr.Viewer.ComputerName} sent Answer: {cRr.Answer.AnswerText}(AnswerID: {cRr.Answer.AnswerID}), MessageId: {cRr.OriginalMessage.MessageGUID}");
                }
            }
        }
    }
}