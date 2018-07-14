using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Advantech.Adam;
using System.Net.Sockets;
using System.Configuration;

namespace Traceabilty_Flex
{
    class Adam60xx
    {
        /// <summary>
        /// Send2Adam - Turn single digital output on or off at line
        /// </summary>
        /// <param name="Line"></param>
        /// <param name="ip"></param>
        /// <param name="iDO"></param>
        /// <param name="iOnOff"></param>
        /// <returns>Null or error</returns>
        ///  9/03/2014  Zohar   Get PLC IP from DB

        
        public static string Send2Adam(string Line, string IP, int iDO, int iOnOff )
        {
            string rtn = null;
            bool done = false;
            int retry = 3;
            AdamSocket adamModbus;
            //Adam6000Type m_Adam6000Type;
            object m_szIP;
            int m_iPort;
            //int m_iDoTotal, m_iDiTotal, m_iCount;

            if (string.IsNullOrEmpty(IP)) return rtn;                         // No PLC at line

            m_szIP = IP;                // modbus TCP IP
            m_iPort = 502;				// modbus TCP port is 502
            adamModbus = new AdamSocket();
            adamModbus.SetTimeout(1600, 1600, 1600); // set timeout for TCP

            //m_Adam6000Type = Adam6000Type.Adam6066;
            for (int i = 0; i < retry && !done; i++) {
                if (adamModbus.Connect(m_szIP.ToString(), ProtocolType.Tcp, m_iPort))
                    done = true;
                else
                    System.Threading.Thread.Sleep(1000);
            }
            if(!done)
                rtn = "Connect to " + Line + " Address: " + m_szIP.ToString() + " failed";
            else
            {
                done = false;
                int iStart = 17 + iDO;
                for (int i = 0; i < retry && !done; i++){
                    if (adamModbus.Modbus().ForceSingleCoil(iStart, iOnOff)) 
                        done = true;
                    else
                        System.Threading.Thread.Sleep(1000);
                }
                if (!done)
                    rtn = IP + " Set digital output failed!";
            }
            adamModbus.Disconnect(); // disconnect slave
            return rtn;
        }
    }
}
