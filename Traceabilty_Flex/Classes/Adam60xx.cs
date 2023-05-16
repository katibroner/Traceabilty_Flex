using Advantech.Adam;
using System.Net.Sockets;

namespace Traceabilty_Flex
{
    class Adam60Xx
    {
        /// Send2Adam - Turn single digital output on or off at line

        public static string Send2Adam(string line, string ip, int iDo, int iOnOff )
        {
            string rtn = null;
            var done = false;
            const int retry = 3;

            if (string.IsNullOrEmpty(ip)) return rtn;   // No PLC at line

            object mSzIp = ip;
            var mIPort = 502;

            var adamModbus = new AdamSocket();
            adamModbus.SetTimeout(1600, 1600, 1600); // set timeout for TCP

            for (var i = 0; i < retry && !done; i++) {
                if (adamModbus.Connect(mSzIp.ToString(), ProtocolType.Tcp, mIPort))
                    done = true;
                else
                    System.Threading.Thread.Sleep(1000);
            }
            if(!done)
                rtn = "Connect to " + line + " Address: " + mSzIp.ToString() + " failed";
            else
            {
                done = false;
                var iStart = 17 + iDo;
                for (var i = 0; i < retry && !done; i++){
                    if (adamModbus.Modbus().ForceSingleCoil(iStart, iOnOff)) 
                        done = true;
                    else
                        System.Threading.Thread.Sleep(1000);
                }
                if (!done)
                    rtn = ip + " Set digital output failed!";
            }
            adamModbus.Disconnect(); // disconnect slave
            return rtn;
        }
    }
}
