using Asm.As.Oib.SiplacePro.LineControl.Contracts.Faults;
using Asm.As.Oib.SiplacePro.LineControl.Proxy.Business.Objects;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Windows;
using LcObjects = Asm.As.Oib.SiplacePro.LineControl.Contracts.Data;
using Asm.As.Oib.SiplacePro.LineControl.Contracts;

namespace Traceabilty_Flex
{
    public partial class MainWindow : Window
    {
        private List<string[]> StartLineControl()
        {
            List<string[]> list = new List<string[]>();
            LineControl lineControlProxy = null;
            try
            {
                for (int i = 0; i < _LineControlEndpointAddress.Length; i++)
                {
                    lineControlProxy = InitializeLineControlPoxy(_LineControlEndpointAddress[i]);
                    if (lineControlProxy != null)
                    {
                        string[] result = ShowLineStatus(lineControlProxy, _lines[i], "right");
                        list.Add(result);

                        if (GetLeaf(_lines[i]) == "Line-R" || GetLeaf(_lines[i]) == "Line-S")
                        {
                            result = ShowLineStatus(lineControlProxy, _lines[i], "left");
                            list.Add(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage("ERROR: " + ex.Message);
            }
            finally
            {
                ReleaseLineControlProxy(lineControlProxy);
            }
            return list;
        }

        private LineControl InitializeLineControlPoxy(string s)
        {
            LineControl lineControlProxy = null;
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
                lineControlProxy = new LineControl(endpointAddress, binding);
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
                ErrorMessage("ERROR: " + ex.Message + s);
            }
            return lineControlProxy;

        }

        private string[] ShowLineStatus(LineControl lineControlProxy, string line, string side)
        {
            LcObjects.LineControlLineStatus lineStatus = lineControlProxy.GetLineStatus(line);

            foreach (LcObjects.LineControlStationStatus stationStatus in lineStatus.LineControlStationStati)
            {
                if (GetLeaf(line) == "Line-R")
                {
                    if (side == "left" && stationStatus.SetupName != "" && stationStatus.LeftConveyorStatus.BoardName != "")
                        return new string[] { stationStatus.SetupName, stationStatus.LeftConveyorStatus.BoardName, "Line-R2" };

                    else if (side == "right" && stationStatus.SetupName != "" && stationStatus.RightConveyorStatus.BoardName != "")
                        return new string[] { stationStatus.SetupName, stationStatus.RightConveyorStatus.BoardName, "Line-R1" };
                }
                if (GetLeaf(line) == "Line-S")
                {
                    if (side == "left" && stationStatus.SetupName != "" && stationStatus.LeftConveyorStatus.BoardName != "")
                        return new string[] { stationStatus.SetupName, stationStatus.LeftConveyorStatus.BoardName, "Line-S2" };

                    else if (side == "right" && stationStatus.SetupName != "" && stationStatus.RightConveyorStatus.BoardName != "")
                        return new string[] { stationStatus.SetupName, stationStatus.RightConveyorStatus.BoardName, "Line-S1" };
                }
                if (GetLeaf(line) == "Line-Q")
                {
                    if (side == "left" && stationStatus.SetupName != "" && stationStatus.LeftConveyorStatus.BoardName != "")
                        return new string[] { stationStatus.SetupName, stationStatus.LeftConveyorStatus.BoardName, "Line-Q2" };

                    else if (side == "right" && stationStatus.SetupName != "" && stationStatus.RightConveyorStatus.BoardName != "")
                        return new string[] { stationStatus.SetupName, stationStatus.RightConveyorStatus.BoardName, "Line-Q1" };
                }
                else
                {
                    if (stationStatus.SetupName != "" && stationStatus.RightConveyorStatus.RecipeName != "")
                    {
                        return new string[] { stationStatus.SetupName, stationStatus.RightConveyorStatus.RecipeName, line };
                    }
                }

            }
            return null;
        }

        private void ReleaseLineControlProxy(LineControl lineControlProxy)
        {
            try
            {
                lineControlProxy?.Dispose();
            }
            catch (Exception ex)
            {
                ErrorMessage("ERROR: failed to dispose of line control proxy: " + ex);
            }
            finally
            {
                lineControlProxy = null;
            }
        }

        public delegate void MsgOutDelegate(string message);
    }
}