namespace TraceabilityTestGui
{
    public class Comp2
    {
        public int Location { get; set; }
        public int Track { get; set; }
        public int Division { get; set; }
        public int Tower { get; set; }
        public int Level { get; set; }
        public string Pn { get; set; }
        public string Bc { get; set; }
        public int Number { get; set; }
        public string UnitId { get; set; }
        public string Batch { get; set; }
        public string UnitIdAlt { get; set; }

        public Comp2(string pn, int loc, int tr, int div, int tow, int lvl, int n, string u, string b)
        {
            Pn = pn;
            Location = loc;
            Track = tr;
            Division = div;
            Tower = tow;
            Level = lvl;
            Number = n;
            UnitId = u;
            Batch = b;
        }
    }
}
