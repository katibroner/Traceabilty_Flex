using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Advantech.Adam;
using System.Net.Sockets;

namespace LTSMonitor
{
    class Adam6000
    {
        private static AdamSocket adamModbus;
        private static Adam6000Type m_Adam6000Type;
        private static string m_szIP;
        private static int m_iPort;
        private static int m_iCount;
        private static int m_iAiTotal, m_iDoTotal;
        private static bool[] m_bChEnabled;
        private static byte[] m_byRange;
        private static void onTimer()
        {
            if (adamModbus.Connect(m_szIP, ProtocolType.Tcp, m_iPort))
            {
                //RefreshDO();
                RefreshChannelValue();
                adamModbus.Disconnect();
            }
        }

        private static void Initialize()
        {
            //m_szIP = GetSetting("ElbitAdamIP");	// modbus slave IP address "10.229.141.235"
            //m_iPort = 502;				            // modbus TCP port is 502
            //adamModbus = new AdamSocket();
            //adamModbus.SetTimeout(1000, 1000, 1000); // set timeout for TCP

            //m_Adam6000Type = Adam6000Type.Adam6017; // the sample is for ADAM-6017

            //m_iAiTotal = AnalogInput.GetChannelTotal(m_Adam6000Type);
            //m_iDoTotal = DigitalOutput.GetChannelTotal(m_Adam6000Type);
            //m_bChEnabled = new bool[m_iAiTotal];
            //m_byRange = new byte[m_iAiTotal];

            //ScanPorts();
        }
        private static void RefreshDO()
        {
            int iStart = 17;
            bool[] bData = new bool[m_iDoTotal];

            if (m_iDoTotal == 0)
            {
            }
            else
            {
                if (adamModbus.Modbus().ReadCoilStatus(iStart, m_iDoTotal, out bData))
                {

                    //if (m_iDoTotal > 0)
                    //    txtCh0.Text = bData[0].ToString();
                    //if (m_iDoTotal > 1)
                    //    txtCh1.Text = bData[1].ToString();
                    //if (m_iDoTotal > 2)
                    //    txtCh2.Text = bData[2].ToString();
                    //if (m_iDoTotal > 3)
                    //    txtCh3.Text = bData[3].ToString();
                    //if (m_iDoTotal > 4)
                    //    txtCh4.Text = bData[4].ToString();
                    //if (m_iDoTotal > 5)
                    //    txtCh5.Text = bData[5].ToString();
                    //if (m_iDoTotal > 6)
                    //    txtCh6.Text = bData[6].ToString();
                    //if (m_iDoTotal > 7)
                    //    txtCh7.Text = bData[7].ToString();

                }
                else
                {
                    //txtCh0.Text = "Fail";
                    //txtCh1.Text = "Fail";
                    //txtCh2.Text = "Fail";
                    //txtCh3.Text = "Fail";
                    //txtCh4.Text = "Fail";
                    //txtCh5.Text = "Fail";
                    //txtCh6.Text = "Fail";
                    //txtCh7.Text = "Fail";
                }
            }
        }
        private static void RefreshChannelValue()
        {
            int iStart = 1, iBurnStart = 121;
            int iIdx;
            int[] iData;
            float[] fValue = new float[m_iAiTotal];
            bool[] bBurn = new bool[m_iAiTotal];
            string sql, Result, _date;
            float _tmp, _hum;

            if (adamModbus.Modbus().ReadInputRegs(iStart, m_iAiTotal, out iData))
            {
                for (iIdx = 0; iIdx < m_iAiTotal; iIdx++)
                    fValue[iIdx] = AnalogInput.GetScaledValue(m_Adam6000Type, m_byRange[iIdx], iData[iIdx]);
                //
                _hum = (float)(fValue[0] * 7 - 26);
                _tmp = (float)(fValue[1] * 6.25 - 44);
                Console.WriteLine("Humid={0:N3}", _hum);
                Console.WriteLine("Temp ={0:N3}", _tmp);

                SaveReadings("HUM.txt", false, _hum);
                SaveReadings("TMPR.txt", false, _tmp);

                string year = DateTime.Today.ToString("yyyy");
                SaveReadings("HUM" + year + ".txt", true, _hum);
                SaveReadings("TMPR" + year + ".txt", true, _tmp);
                Console.WriteLine(RoundTimeByMinuth(DateTime.Now, 30));

                _date = RoundTimeByMinuth(DateTime.Now, 30).ToString("dd-MMM-yyyy HH:mm");
                sql = "MERGE INTO ENV_HUMIDITY USING DUAL ON (LOG_DATE = to_date('{0}','dd-mon-yyyy hh24:mi')) "
                        + "WHEN MATCHED THEN UPDATE SET {1}={2} "
                        + "WHEN NOT MATCHED THEN INSERT (LOG_DATE,{1}) VALUES (to_date('{0}','dd-mon-yyyy hh24:mi'),{2})";
                UpdateDB("SMT", string.Format(sql, _date, "TEMP_ELBIT", _tmp), out Result);
                if (Result != "OK")
                {
                    throw new Exception(string.Format("Oracle error while updating TEMP_ELBIT: {0}", Result));
                }
                UpdateDB("SMT", string.Format(sql, _date, "HUM_ELBIT", _hum), out Result);
                if (Result != "OK")
                {
                    throw new Exception(string.Format("Oracle error while updating HUM_ELBIT: {0}", Result));
                }
            }
        }
        //private static void RefreshSingleChannel(int i_iIndex, ref TextBox txtCh, float fValue)
        //{
        //    string szFormat;

        //    if (m_bChEnabled[i_iIndex])
        //    {
        //        szFormat = AnalogInput.GetFloatFormat(m_Adam6000Type, m_byRange[i_iIndex]);
        //        txtCh.Text = fValue.ToString(szFormat) + " " + AnalogInput.GetUnitName(m_Adam6000Type, m_byRange[i_iIndex]);
        //    }
        //}
    }
}
