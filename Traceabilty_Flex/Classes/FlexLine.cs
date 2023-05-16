using System;
using System.Collections.Generic;

namespace Traceabilty_Flex
{
    public class FlexLine
    {
        public string Name { get; set; }
        public List<string> Stations { get; set; }
        public Dictionary<string, int> StationDictionary { get; set; }
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
            if (Stations.Exists(x => x == s)) return;
            Stations.Add(s);
            AddStationToDictionary(s);
        }

        private void AddStationToDictionary(string s)
        {
            StationDictionary.Add(s, Convert.ToInt32(s[4]));
        }
    }
}
