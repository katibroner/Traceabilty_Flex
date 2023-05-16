using Asm.As.Oib.SiplacePro.Proxy.Architecture.Objects;
using schemas.xmlsoap.org.ws._2004._08.eventing;
using System;
using System.Data;
using System.ServiceModel;
using System.Windows;
using TraceabilityTestGui;

namespace Traceabilty_Flex
{
    public partial class MainWindow : Window
    {
        private void Window_Closed(object sender, EventArgs e)
        {
            if(_mainservice && HasPermissionsToClose())
                ChangeStatus(0);

            ClearAllBoxes();

            try
            {
                if (_client != null)
                {
                    _client.Dispose();
                }
            }
            catch
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
            SqlClass sql = new SqlClass("trace");
            string query = "SELECT * FROM Status";

            DataTable d = sql.SelectDb(query, out string result);

            if (Environment.MachineName == d.Rows[0]["host"].ToString().Trim() || User == d.Rows[0]["user"].ToString().Trim())
                return true;

            return false;
        }
    }
}