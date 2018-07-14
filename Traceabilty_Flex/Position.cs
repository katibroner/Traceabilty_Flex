using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TraceabilityTestGui
{
    public class Comp
    {
        public int Location { get; set; }
        public int Track { get; set; }
        public int Division { get; set; }
        public int Tower { get; set; }
        public int Level { get; set; }
        public string PN { get; set; }
        public string BC { get; set; }
        public int Number { get; set; }
        public string UnitID { get; set; }
        public string Batch { get; set; }
        public string UnitIDAlt { get; set; }

        public Comp(string pn, int loc, int tr, int div, int tow, int lvl, int n, string u, string b)
        {
            PN = pn;
            Location = loc;
            Track = tr;
            Division = div;
            Tower = tow;
            Level = lvl;
            Number = n;
            UnitID = u;
            Batch = b;
        }
    }
}
