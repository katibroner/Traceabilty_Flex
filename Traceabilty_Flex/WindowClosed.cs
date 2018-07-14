using Asm.As.Oib.SiplacePro.Proxy.Architecture.Objects;
using schemas.xmlsoap.org.ws._2004._08.eventing;
using System;
using System.Data;
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
        private void Window_Closed(object sender, EventArgs e)
        {
            // When ever the form is closed
            // Unsubscribe from the events.
            if(_mainservice && HasPermissionsToClose())
                ChangeStatus(0);

            ClearAllBoxes();

            try
            {
                if (_client != null)
                {
                    // We should ALWAYS call Dispose when we are done with a Disposable object!
                    _client.Dispose();
                }
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch
            // ReSharper restore EmptyGeneralCatchClause
            {
            }

            #region SIPLACE_OIB_TRACEABILITY_UNSUBSCRIBE

            try
            {
                // Monitoring
                DeleteSubscription();
                _timer.Enabled = false;
                StopReceiver();

            }
            catch (CommunicationException communicationExcpetion)
            {
                CommunicationExceptionOut("During Unsubscribe", communicationExcpetion, true);
            }
            catch (Exception exPing)
            {
                ExceptionOut("During Unsubscribe", exPing, true);
            }

            #endregion // SIPLACE_OIB_SAMPLE_UNSUBSCRIBE
        }

        private bool HasPermissionsToClose()
        {
            SQLClass sql = new SQLClass("trace");
            string query = "SELECT * FROM [Traceability].[dbo].[Status]";

            DataTable d = sql.SelectDB(query, out string result);

            if (Environment.MachineName == d.Rows[0]["host"].ToString().Trim() || User == d.Rows[0]["user"].ToString().Trim())
                return true;

            return false;
        }
    }
}