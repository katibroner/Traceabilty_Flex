using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using System.Windows;
using www.siplace.com.OIB._2012._03.Traceability.Contracts.Service;

namespace Traceabilty_Flex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        #region Error Messages and Messages

        /// <summary>
        /// Print out the Messages in the form.
        /// </summary>
        /// <param name="message">The message.</param>
        internal void MessageOut(string message)
        {
            if (message == null || message == "") return;

            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                LstMsgBox.Items.Add(message);
                LstMsgBox.SelectedIndex = LstMsgBox.Items.Count - 1;
                LstMsgBox.ScrollIntoView(LstMsgBox.SelectedItem);
            }));

            m_MessageCount++;

            if (m_MessageCount >= m_MaxMessageCount)
            {
                Dispatcher.BeginInvoke(new Action(delegate ()
                {
                   // LstMsgBox.Items.RemoveAt(0);
                    ClearListBox(LstMsgBox);
                }));

             //   m_MessageCount--;
                m_MessageCount = 0;
            }
            //lstMsgBox.SelectedIndex = lstMsgBox.Items.Count - 1;
        }

        /// <summary>
        /// Output function for error messages
        /// </summary>
        /// <param name="message">The message.</param>
        internal void ErrorOut(string message)
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

            WriteLog(message);
        }

        public static void WriteLog(string logLine)
        {
            //   string path = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + ".log"; //System.AppDomain.CurrentDomain.BaseDirectory
            string path = "c:\\tmp\\errorlog.txt";
            using (StreamWriter sw = new StreamWriter(path, true))     // Append, Create if not exist
            {
                sw.WriteLine("{0:dd/MM/yyyy HH:mm} > {1}", DateTime.Now, logLine);
            }
        }

        /// <summary>
        /// Traceabilities the fault out.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="fault">The fault.</param>
        /// <param name="showMessageBox">if set to <c>true</c> [show message box].</param>
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

        /// <summary>
        /// Communications the exception out.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="communicationException">The communication exception.</param>
        /// <param name="showMessageBox">if set to <c>true</c> [show message box].</param>
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

        /// <summary>
        /// Exceptions the out.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="showMessageBox">if set to <c>true</c> [show message box].</param>
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

        /// <summary>
        /// Handles the Click event of the btnClearMessages control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void BtnClearMessages_Click(object sender, EventArgs e)
        {
            //lstMsgBox.Items.Clear();
            m_MessageCount = 0;
        }

        public static string GetAllFootprints(Exception x)
        {
            var st = new StackTrace(x, true);
            var frames = st.GetFrames();
            var traceString = new StringBuilder();

            foreach (var frame in frames)
            {
                if (frame.GetFileLineNumber() < 1)
                    continue;

                traceString.Append("File: " + frame.GetFileName());
                traceString.Append(", Method:" + frame.GetMethod().Name);
                traceString.Append(", LineNumber: " + frame.GetFileLineNumber());
                traceString.Append("  -->  ");
            }

            return traceString.ToString();
        }

        #endregion
    }
}