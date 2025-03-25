using System;
using System.ServiceModel;
using System.Windows;
using www.siplace.com.OIB._2012._03.Traceability.Contracts.Service;

namespace Traceabilty_Flex
{
    public partial class MainWindow : Window
    {
        internal void MessageOut(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                LstMsgBox.Items.Add(message);
            }));

            m_MessageCount++;
            if (m_MessageCount < m_MaxMessageCount) return;

            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                ClearListBox(LstMsgBox);
            }));
            m_MessageCount = 0;
        }

        internal  void ErrorOut(string message)
        {
            if (message == null || message == "") return;

            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                if (ListErrors.Items.Count > 200)
                {
                    ClearListBox(ListErrors);
                }
                ListErrors.Items.Add(DateTime.Now.ToString("HH:mm:ss") + " " + message);
            }));

            LogWriter.WriteLog(message);
        }

        internal void TraceabilityFaultOut(string message, FaultException<TraceabilityFault> fault,
                                        bool showMessageBox)
        {
            ErrorOut("TRACEABILITY FAULT: " + message);
            ErrorOut(" Traceability Fault message: " + fault.Detail.ExtendedMessage);
            ErrorOut(" Traceability Fault code: " + fault.Detail.ErrorCode);
            ErrorOut(" Fault message: " + fault.Message);
            ErrorOut(" Stacktrace: " + fault.StackTrace);

            if (showMessageBox)
            {
                MessageBox.Show(
                    @" Traceability Fault message: " + fault.Detail.ExtendedMessage + @"\n" +
                    @" Traceability Fault code: " + fault.Detail.ErrorCode,
                    @"TRACEABILITY FAULT ");
            }
        }

        internal void CommunicationExceptionOut(string message, CommunicationException communicationException,
                                              bool showMessageBox)
        {
            ErrorOut("COMMUNICATION ERROR: " + message);
            ErrorOut(" Exception: " + communicationException.Message);
            ErrorOut(" Stacktrace: " + communicationException.StackTrace);
            if (showMessageBox)
            {
                MessageBox.Show(
                    @" Exception: " + communicationException.Message + @"\n" +
                    @" Stacktrace: " + communicationException.StackTrace,
                    @"COMMUNICATION ERROR ");
            }
        }

        internal void ExceptionOut(string message, Exception exception, bool showMessageBox)
        {
            ErrorOut("EXCEPTION: " + message);
            ErrorOut(" Exception: " + exception.Message);
            ErrorOut(" Stacktrace: " + exception.StackTrace);
            if (showMessageBox)
            {
                MessageBox.Show(
                    @" Exception: " + exception.Message + @"\n" +
                    @" Stacktrace: " + exception.StackTrace,
                    @"EXCEPTION ");
            }
        }
    }
}