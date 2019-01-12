using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelBuildsMonitor
{
    public abstract class Utils
    {
        public static long TicksToSeconds(long Ticks)
        {
            return (long)(Ticks / 10000000);
        }


        public static string SecondsToString(long Ticks)
        {
            long seconds = TicksToSeconds(Ticks);
            string ret;
            if (seconds > 9)
            {
                ret = (seconds % 60).ToString() + "s";
            }
            else if (seconds > 0)
            {
                long dsecs = Ticks / 1000000;
                ret = (seconds % 60).ToString() + "." + (dsecs % 10).ToString() + "s";
            }
            else
            {
                long csecs = Ticks / 100000;
                ret = (seconds % 60).ToString() + "." + ((csecs % 100) < 10 ? "0" : "") + (csecs % 100).ToString() + "s";
            }
            long minutes = seconds / 60;
            if (minutes > 0)
            {
                ret = (minutes % 60).ToString() + "m" + ret;
                long hours = minutes / 60;
                if (hours > 0)
                {
                    ret = hours.ToString() + "h" + ret;
                }
            }
            return ret;
        }
    }
}
