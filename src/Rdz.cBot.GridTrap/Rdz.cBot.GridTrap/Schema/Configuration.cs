using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Rdz.cBot.GridTrap.Schema
{
    internal class Configuration
    {

        internal EntryParameters EntryParameters { get; set; }
        internal ClosureParameters ClosureParameters { get; set; }
        internal GridParameters GridParameters { get; set; }
    }

    internal class GridParameters
    {
        internal OrderType OrderType { get; set; }
        internal double Size { get; set; }
        internal double LotSize { get; set; }
        internal Intervals Intervals { get; set; }
    }

    internal class Intervals
    {
        internal int Grid { get; set; }
        internal int Starting { get; set; }
    }

    internal class EntryParameters
    {
        internal EntryMode EntryMode { get; set; }
        internal TickVolumeThreshold TickVolumeThreshold { get; set; }
    }
    internal class TickVolumeThreshold
    {
        internal double Max { get; set; }
        internal double Min { get; set; }
    }
    internal class ClosureParameters
    {
        internal ClosureMode ClosureMode { get; set; }
        internal ClosureModeFixed Fixed { get; set; }
    }
    internal class ClosureModeFixed
    {
        internal double TakeProfit { get; set; }
    }
}
