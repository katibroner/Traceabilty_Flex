using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilityTestGui
{
    public class FlexLine
    {
        public string Name { get; set; }
        public List<string> Stations { get; set; }
        public Dictionary<string,int> StationDictionary { get; set; }
        public string First { get; set; }
        public string Last { get; set; }
        public Dictionary<string, int> PalletDictionary { get; set; }
        public string[] Used { get; set; }

        public FlexLine(string n)
        {
            Name = n;
            Stations = new List<string>();
            StationDictionary = new Dictionary<string, int>();
            PalletDictionary = new Dictionary<string, int>();
            Used = new string[StationDictionary.Count];
        }

        public void AddStation(string s)
        {
            for (int i = 0; i < Stations.Count; i++)
            {
                if (Stations[i] == s)
                    return;
            }
            Stations.Add(s);

            AddStationToDictionary(s);
        }

        private void AddStationToDictionary(string s)
        {
            char ch = s[4];
            int d = Convert.ToInt32(ch.ToString());
            StationDictionary.Add(s, d);
        }
    }
}
