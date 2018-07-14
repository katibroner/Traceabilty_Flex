using Asm.As.Oib.DisplayService.Contracts.Data;
using Asm.As.Oib.DisplayService.Contracts.Data.Types;
using Asm.As.Oib.DisplayService.Contracts.Messages;
using Asm.As.Oib.DisplayService.Proxy.Architecture.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Traceabilty_Flex
{
    /// <summary>
    /// Interaction logic for DisplayMessenger.xaml
    /// </summary>
    public partial class DisplayMessenger : Window
    {
        private DisplayServiceClient _client;
        private string _tbLine = "";
        private string _tbStation = "";
 //       private ViewerRegistration _receiver;

        #region Structs

        public struct SubAnswerItem
        {
            public string Text;
            public bool Editable;

            public override string ToString() { return Text; }
        }

        #endregion

        public DisplayMessenger(string line, string station, DisplayServiceClient client)
        {
            InitializeComponent();
            _tbLine = line;
            _tbStation = station;
            this._client = client;
            txtLine.Text = line;
            txtStation.Text = station;
        }

        #region GetDisplayServiceClient
        private ViewerRegistration GetDisplayServiceClient()
        {
            foreach (ViewerRegistration item in _client.GetAllViewerRegistrations())
            {
                if((bool)item.IsStationViewer)
                {
                    if (_tbStation == item.SIPLACEProStationPath)
                        return item;
                }
                else
                {
                    if (_tbLine == item.SIPLACEProLinePath)
                        return item;
                }
            }
            return null;
        }
        #endregion

        #region SendMessage_Click
        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _rbErrorMessage.Text = string.Empty;
                if (_client != null)
                {
                    var answers = new List<Answer>();

                    if ((bool)_cbUseAnswers.IsChecked)
                    {
                        foreach (string answer1 in _lbAnswers.Items)
                        {
                            var aw = new Answer(answer1, Guid.NewGuid());
                            answers.Add(aw);
                            if ((bool)_cbUseSubAnswers.IsChecked)
                            {
                                foreach (SubAnswerItem item in _lbSubAnswers.Items)
                                {
                                    var subanswer1 = new SubAnswer(item.Text, Guid.NewGuid()) { TextEditable = item.Editable };
                                    aw.SubAnswersNew.Add(subanswer1);
                                }
                            }
                        }
                    }

                    int priority = Convert.ToInt32(((ComboBoxItem)_nudPriority.SelectedItem).Content);
                    int defanswer = Convert.ToInt32(((ComboBoxItem)_nudDefaultAnswerIndex.SelectedItem).Content);
                    _rbConfirmation.Text = "";
                    #region DOC_ACK_TYPE
                    //set AcknowledgementType
                    AcknowledgementType ack;
                    if ((bool)_rbAckAllViewers.IsChecked)
                    {
                        ack = AcknowledgementType.AllReceivers;
                    }
                    else if ((bool)_rbAckOneViewer.IsChecked)
                    {
                        ack = AcknowledgementType.OneReceiver;
                    }
                    else
                    {
                        ack = AcknowledgementType.Originator;
                    }
                    #endregion
                    //set SeverityLevel
                    SeverityLevel level;
                    if ((bool)_rbSevError.IsChecked)
                    {
                        level = SeverityLevel.Error;
                    }
                    else if ((bool)_rbSevInfo.IsChecked)
                    {
                        level = SeverityLevel.Info;
                    }
                    else if ((bool)_rbSevNone.IsChecked)
                    {
                        level = SeverityLevel.None;
                    }
                    else
                    {
                        level = SeverityLevel.Warning;
                    }

                    SendMessageResponse messageResponse;
                    #region DOC_SENDMSG_EXPLICIT
                    // send the message explicit
                    if ((bool)_rbExplicit.IsChecked)
                    {

                        List<ViewerRegistration> list = _client.GetAllViewerRegistrations();

                        if (list.Count == 0)
                        {
                            MessageBox.Show(@"There are no viewers registered currently, cancelling the send!");
                            return;
                        }
                        messageResponse = _client.SendMessageExplicit(list, _rbMessage.Text,
                                                                                          _rbExtDescription.Text,
                                                                                          ack,
                                                                                          (bool)_cbCallbackRequested.IsChecked,
                                                                                          priority,
                                                                                          level, 
                                                                                          answers,
                                                                                          defanswer,
                                                                                          (bool)_cbExpandExtendedDescription.IsChecked);


                    }
                    #endregion  
                    // send the message by line path to all stations
                    else if ((bool)_rbLineAll.IsChecked)
                    {
                        if (string.IsNullOrEmpty(_tbLine))
                        {
                            MessageBox.Show(@"You need to specify a valid line path, cancelling the send!");
                            return;
                        }
                        messageResponse = _client.SendMessageByLinePathAllStations(_tbLine, _rbMessage.Text,
                                                                                          _rbExtDescription.Text,
                                                                                          ack,
                                                                                          (bool)_cbCallbackRequested.IsChecked,
                                                                                          priority,
                                                                                          level, answers, null,
                                                                                          defanswer,
                                                                                          (bool)_cbExpandExtendedDescription.IsChecked, (bool)_cbNonStationViewers.IsChecked);
                    }
                    // send the message by line path to the non-station viewer that is registered for that line
                    else if ((bool)_rbLineNo.IsChecked)
                    {
                        if (string.IsNullOrEmpty(_tbLine))
                        {
                            MessageBox.Show(@"You need to specify a valid line path, cancelling the send!");
                            return;
                        }
                        messageResponse = _client.SendMessageByLinePathNoStations(_tbLine, _rbMessage.Text,
                                                                                          _rbExtDescription.Text,
                                                                                          ack,
                                                                                          (bool)_cbCallbackRequested.IsChecked,
                                                                                          priority,
                                                                                          level, answers, null,
                                                                                          defanswer,
                                                                                          (bool)_cbExpandExtendedDescription.IsChecked);
                    }
                    #region DOC_SENDMSG_FIRSTSTATION
                    // send the message by line path to the first station
                    else if ((bool)_rbLineFirst.IsChecked)
                    {
                        if (string.IsNullOrEmpty(_tbLine))
                        {
                            MessageBox.Show(@"You need to specify a valid line path, cancelling the send!");
                            return;
                        }
                        messageResponse = _client.SendMessageByLinePathFirstStation(_tbLine, _rbMessage.Text,
                                                                                          _rbExtDescription.Text,
                                                                                          ack,
                                                                                          (bool)_cbCallbackRequested.IsChecked,
                                                                                          priority,
                                                                                          level, answers, null,
                                                                                          defanswer,
                                                                                          (bool)_cbExpandExtendedDescription.IsChecked, (bool)_cbNonStationViewers.IsChecked);
                    }
                    #endregion 
                    // send the message by station path
                    else
                    {
                        if (string.IsNullOrEmpty(_tbStation))
                        {
                            MessageBox.Show(@"You need to specify a valid station path, cancelling the send!");
                            return;
                        }
                        messageResponse = _client.SendMessageByStationPath(_tbStation, _rbMessage.Text,
                                                                                       _rbExtDescription.Text,
                                                                                       ack,
                                                                                       (bool)_cbCallbackRequested.IsChecked,
                                                                                       priority,
                                                                                       level, answers, null,
                                                                                       defanswer,
                                                                                       (bool)_cbExpandExtendedDescription.IsChecked);
                    }

                    int index = _cbMessages.Items.Add(messageResponse.Message.MessageGUID);
                    _cbMessages.SelectedIndex = index;

                    string errorMessage = string.Empty;
                    foreach (var detail in messageResponse.DeliveryDetails)
                    {
                        if (!detail.DeliveredSuccessfully)
                        {
                            errorMessage += string.Format("Viewer {0} could not be reached: {1}", detail.ViewerRegistration, detail.ExecptionMessage);
                        }
                    }
                    if (errorMessage != string.Empty)
                    {
                        _rbErrorMessage.Text = errorMessage;
                    }
                    else
                    {
                        _statusLabel.Content = @"Sent message " + messageResponse.Message.MessageGUID + @" successfully.";
                    }
                }
                else
                {
                    MessageBox.Show(this, @"No client registration available.");
                }
            }
            catch (Exception ex)
            {
                MainWindow._mWindow.ErrorOut("At b_SendMessage: " +  ex.Message);
            }
        }
        #endregion

        #region UpdateMessage_Click
        private void UpdateMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _rbErrorMessage.Text = string.Empty;
                if (_client != null && !string.IsNullOrEmpty(_cbMessages.Text))
                {
                    //set SeverityLevel
                    SeverityLevel level;
                    if ((bool)_rbSevError.IsChecked)
                    {
                        level = SeverityLevel.Error;
                    }
                    else if ((bool)_rbSevInfo.IsChecked)
                    {
                        level = SeverityLevel.Info;
                    }
                    else if ((bool)_rbSevNone.IsChecked)
                    {
                        level = SeverityLevel.None;
                    }
                    else
                    {
                        level = SeverityLevel.Warning;
                    }

                    #region DOC_UPDATE_MSG
                    string msg = _cbMessages.Text;
                    List<MessageDeliveryDetails> updateMessageResponse = _client.UpdateMessage(msg, _rbMessage.Text,
                        _rbExtDescription.Text, (bool)_cbExpandExtendedDescription.IsChecked, (int)_nudPriority.SelectedItem, level);
                    string errorMessage = string.Empty;
                    foreach (var detail in updateMessageResponse)
                    {
                        if (!detail.DeliveredSuccessfully)
                        {
                            errorMessage += string.Format("Viewer {0} could not be reached: {1}", detail.ViewerRegistration, detail.ExecptionMessage);
                        }
                    }
                    if (errorMessage != string.Empty)
                    {
                        _rbErrorMessage.Text = errorMessage;
                    }
                    else
                    {
                        _statusLabel.Content = @"Updated message " + msg + @" successfully.";
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                MainWindow._mWindow.ErrorOut("At b_UpdateMessage: " + ex.Message);
            }
        }
        #endregion

        #region RevokeMessage_Click
        private void RevokeMessage_Click(object sender, RoutedEventArgs e)
        {
               try
            {
                _rbErrorMessage.Text = string.Empty;
                if (_client != null && _cbMessages.SelectedItem != null)
                {
                    #region DOC_REVOKE_MSG
                    string msg = (string)_cbMessages.SelectedItem;
                    bool revoked = _client.RevokeMessage(msg);
                    if (!revoked)
                    {
                        _cbMessages.Items.Remove(msg);
                        if (_cbMessages.Items.Count > 0)
                        {
                            _cbMessages.SelectedIndex = 0;
                        }
                        else
                        {
                            _cbMessages.Text = "";
                        }
                        _rbErrorMessage.Text = @"Message could not be revoked";
                    }
                    else
                    {
                        _cbMessages.Items.Remove(msg);
                        if (_cbMessages.Items.Count > 0)
                        {
                            _cbMessages.SelectedIndex = 0;
                        }
                        else
                        {
                            _cbMessages.Text = "";
                        }
                        _statusLabel.Content = @"Revoked message " + msg + @" successfully.";
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                MainWindow._mWindow.ErrorOut("At b_RevokeMessage: " + ex.Message);
            }
        }
        #endregion

        #region RbViewerIdentificationCheckedChanged
        private void RbViewerIdentificationCheckedChanged(object sender, EventArgs e)
        {
            if ((bool)_rbExplicit.IsChecked)
            {
                _cbNonStationViewers.Visibility = Visibility.Hidden;
                _lbNonStation.Visibility = Visibility.Hidden;
                _cbNonStationViewers.IsChecked = false;
            }
            else if ((bool)_rbLineAll.IsChecked || (bool)_rbLineFirst.IsChecked || (bool)_rbLineNo.IsChecked)
            {
                _cbNonStationViewers.Visibility = Visibility.Visible;
                _lbNonStation.Visibility = Visibility.Visible;
                _cbNonStationViewers.IsChecked = true;
                if ((bool)_rbLineNo.IsChecked)
                {
                    _cbNonStationViewers.IsEnabled = false;
                    _cbNonStationViewers.IsChecked = true;
                }
                else
                {
                    _cbNonStationViewers.IsEnabled = true;
                }
            }
            else
            {
                _cbNonStationViewers.Visibility = Visibility.Hidden; 
                _lbNonStation.Visibility = Visibility.Hidden;
                _cbNonStationViewers.IsChecked = false;
            }
        }
        #endregion

        #region CheckBoxes_Check_Uncheck
        private void _cbUseAnswers_Checked(object sender, RoutedEventArgs e)
        {
            _btnAddAnswer.IsEnabled = true;
            _btnRemoveAnswer.IsEnabled = true;
        }

        private void _cbUseAnswers_Unchecked(object sender, RoutedEventArgs e)
        {
            _btnAddAnswer.IsEnabled = false;
            _btnRemoveAnswer.IsEnabled = false;
        }

        private void _cbUseSubAnswers_Unchecked(object sender, RoutedEventArgs e)
        {
            _btnAddSubAnswer.IsEnabled = false;
            _btnRemoveSubAnswer.IsEnabled = false;
        }

        private void _cbUseSubAnswers_Checked(object sender, RoutedEventArgs e)
        {
            _btnAddSubAnswer.IsEnabled = true;
            _btnRemoveSubAnswer.IsEnabled = true;
        }

        private void _btnRemoveAnswer_Click(object sender, RoutedEventArgs e)
        {
            int index = _lbAnswers.SelectedIndex;
            if (index != -1)
            {
                _lbAnswers.Items.RemoveAt(index);
            }
        }

        private void _btnAddAnswer_Click(object sender, RoutedEventArgs e)
        {
            _lbAnswers.Items.Add(_tbAnswer.Text);
        }

        private void _btnRemoveSubAnswer_Click(object sender, RoutedEventArgs e)
        {
            int index = _lbSubAnswers.SelectedIndex;
            if (index != -1)
            {
                _lbSubAnswers.Items.RemoveAt(index);
            }
        }

        private void _btnAddSubAnswer_Click(object sender, RoutedEventArgs e)
        {
            var item = new SubAnswerItem { Text = _tbSubAnswer.Text, Editable = false };
            _lbSubAnswers.Items.Add(item);
        }

        private void _cbxEditableSubAnswer_Checked(object sender, RoutedEventArgs e)
        {
            var i = _lbSubAnswers.SelectedIndex;
            if ((i >= 0) && (i < _lbSubAnswers.Items.Count))
            {
                var item = (SubAnswerItem)_lbSubAnswers.Items[i];
                item.Editable = true;
                _lbSubAnswers.Items[i] = item;
            }
        }

        private void _cbxEditableSubAnswer_Unchecked(object sender, RoutedEventArgs e)
        {
            var i = _lbSubAnswers.SelectedIndex;
            if ((i >= 0) && (i < _lbSubAnswers.Items.Count))
            {
                var item = (SubAnswerItem)_lbSubAnswers.Items[i];
                item.Editable = false;
                _lbSubAnswers.Items[i] = item;
            }
        }

        private void _lbSubAnswers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var i = _lbSubAnswers.SelectedIndex;
            if ((i >= 0) && (i < _lbSubAnswers.Items.Count))
            {
                var item = (SubAnswerItem)_lbSubAnswers.Items[i];
                _cbxEditableSubAnswer.IsChecked = item.Editable;
            }
        }
        #endregion

        private void _lbAnswers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
