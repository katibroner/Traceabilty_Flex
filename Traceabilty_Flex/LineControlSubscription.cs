using Asm.As.Oib.SiplacePro.LineControl.Contracts.Faults;
using Asm.As.Oib.SiplacePro.LineControl.Proxy.Business.Objects;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Windows;
using System.Windows.Media;
using LcObjects = Asm.As.Oib.SiplacePro.LineControl.Contracts.Data;

namespace Traceabilty_Flex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string[]> StartLineControl()
        {
            List<string[]> list = new List<string[]>();
            LineControl _LineControlProxy = null;

            try
            {
                for (int i = 0; i < _LineControlEndpointAddress.Length; i++)
                {
                    _LineControlProxy = InitializeLineControlPoxy(_LineControlEndpointAddress[i]);
                    if (_LineControlProxy != null)
                    {
                        string[] _result = ShowLineStatus(_LineControlProxy, _lines[i]);
                        list.Add(_result);
                        //ReleaseLineControlProxy(_LineControlProxy);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage("ERROR: " + ex.Message);
            }
            finally
            {
                 ReleaseLineControlProxy(_LineControlProxy);
            }
            return list;
        }

        private LineControl InitializeLineControlPoxy(string s)
        {
            LineControl _LineControlProxy = null;
            try
            {
                // Create a tcp binding and extend the timeouts
                NetTcpBinding binding = new NetTcpBinding();
                binding.Security.Mode = SecurityMode.None;
                binding.CloseTimeout = TimeSpan.FromMinutes(10);
                binding.OpenTimeout = TimeSpan.FromSeconds(20);
                binding.ReceiveTimeout = TimeSpan.FromSeconds(20);
                binding.ReliableSession.InactivityTimeout = binding.SendTimeout = TimeSpan.MaxValue;
                binding.ReliableSession.Enabled = true;
                binding.PortSharingEnabled = true;

                // Create the endpoint
                EndpointAddress endpointAddress = new EndpointAddress(s);

                // Create the proxy
                _LineControlProxy = new LineControl(endpointAddress, binding);
            }
            catch (FaultException<SiplaceProLineControlFault> ex)
            {
                ErrorMessage("LINECONTROL ERROR: " + ex.Message);
            }
            catch (CommunicationException ex)
            {
                ErrorMessage("COMMUNICATION ERROR: " + ex.Message);
            }
            catch (Exception ex)
            {
                ErrorMessage("ERROR: " + ex.Message);
            }
            return _LineControlProxy;

        }

        private string[] ShowLineStatus(LineControl _LineControlProxy, string s)
        {
            #region DOC_LINE_STATUS

            LcObjects.LineControlLineStatus lineStatus = _LineControlProxy.GetLineStatus(s);

            foreach (LcObjects.LineControlStationStatus stationStatus in lineStatus.LineControlStationStati)
            {
                if(stationStatus.SetupName != "" && stationStatus.RightConveyorStatus.RecipeName != "")
                {
                    return new string[] { stationStatus.SetupName, stationStatus.RightConveyorStatus.RecipeName, s };
                }
            }
            return null;
            #endregion
        }

        private void ReleaseLineControlProxy(LineControl _LineControlProxy)
        {
            try
            {
                if (_LineControlProxy != null)
                    _LineControlProxy.Dispose();
            }
            catch (Exception ex)
            {
                ErrorMessage("ERROR: failed to dispose of line control proxy: " + ex);
            }
            finally
            {
                _LineControlProxy = null;
            }
        }

        public delegate void MsgOutDelegate(string message);

     

    }
}