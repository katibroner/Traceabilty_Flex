using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityTestGui;

namespace Traceabilty_Flex
{
    public class Worker
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public int Level { get; set; }

        public Worker()
        {
            ID = "";
            Name = "";
            Password = "";
            Level = 1;
        }

        public Worker(string i, string n, string p, int l)
        {
            ID = i;
            Name = n;
            Password = p;
            Level = l;
        }

        public static Worker findMatch(string p)
        {
            SQLClass sql = new SQLClass();

            Worker w = null;
            w =  sql.SelectOneWorker(p);

            return w;
        }
    }
}
