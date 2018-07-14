using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Traceabilty_Flex
{
    public class Comp
    {
        public string Id { get; set; }
        public string Date { get; set; }
        public string Barcode { get; set; }
        public string Program { get; set; }
        public string CompName { get; set; }
        public string Station { get; set; }
        public byte TableLocation { get; set; }
        public short Track { get; set; }
        public short Division { get; set; }
        public short Level { get; set; }
        public int AccessTotal { get; set; }
        public int Consumed { get; set; }
        public int Placed { get; set; }
        public int Missed { get; set; }
        public short Empty { get; set; }
        public short Ident { get; set; }
        public short Vacuum { get; set; }
        public string Line { get; set; }
        public string Recipe { get; set; }
        public short Tower { get; set; }
        public string Shape { get; set; }
        public int TrackID { get; set; }

        public Comp()
        {
            Id = "0";
            Date = "";
            Barcode = "";
            Program = "";
            CompName = "";
            Station = "";
            TableLocation = 0;
            Track = 0;
            Division = 0;
            Level = 0;
            AccessTotal = 0;
            Consumed = 0;
            Placed = 0;
            Missed = 0;
            Empty = 0;
            Ident = 0;
            Vacuum = 0;
            Line = "";
            Recipe = "";
            Tower = 0;
            Shape = "";
            TrackID = 0;
        }
    }
}
