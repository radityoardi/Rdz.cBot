using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using Rdz.cTrader.Library;
using Rdz.cIndi;


namespace Rdz.cBot
{
    public partial class HolyGrailGrid
    {
        [Parameter("BBDistances Source", Group = "Bollinger Bands Distance")]
        public DataSeries BBDistancesSource { get; set; }

        [Parameter("MA Type", Group = "Bollinger Bands Distance", DefaultValue = MovingAverageType.Simple)]
        public MovingAverageType BBDistanceMaType { get; set; }

        [Parameter("Periods", Group = "Bollinger Bands Distance", DefaultValue = 20)]
        public int BBDistancesPeriod { get; set; }

        [Parameter("Standard Dev", Group = "Bollinger Bands Distance", DefaultValue = 2)]
        public int BBDistancesStandardDeviations { get; set; }


        [Parameter("Max Distance", Group = "Bollinger Bands Distance", DefaultValue = 5)]
        public int BBDistancesValueBelow { get; set; }

        [Parameter("Read Period", Group = "Bollinger Bands Distance", DefaultValue = 5)]
        public int BBDistanceReadPeriod { get; set; }

        [Parameter("Grid Interval", DefaultValue = 10)]
        public int GridInterval { get; set; }

        [Parameter("Initial Lot Size", DefaultValue = 0.01)]
        public double InitialLotSize { get; set; }
        [Parameter("Counter Direction Multiplier", DefaultValue = 2)]
        public double CounterDirectionMultiplier { get; set; }

        [Parameter("Use Bollinger Bands Distances", DefaultValue = false)]
        public bool UseBBDistances { get; set; }
    }
}
