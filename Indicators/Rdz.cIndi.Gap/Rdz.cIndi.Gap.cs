using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using Rdz.cTrader.Library;

namespace cAlgo
{
    [Indicator("RdzIndi Gaps", AccessRights = AccessRights.None, IsOverlay = false, ScalePrecision = 0)]
    public class RdzcIndiGap : Indicator
    {
        [Parameter("Data Source 1")]
        public DataSeries Source1 { get; set; }

    	[Parameter("Data Source 2")]
		public DataSeries Source2 { get; set; }

		[Parameter("Non-negative", DefaultValue = false)]
		public bool AlwaysPositive { get; set; }

		[Output("Main", IsHistogram = true, PlotType = PlotType.Histogram)]
        public IndicatorDataSeries Result { get; set; }

        protected override void Initialize()
        {
        }

        public override void Calculate(int index)
        {
            // Calculate value at specified index
            Result[index] = Symbol.Distance(Source1[index], Source2[index], AlwaysPositive);
        }
    }
}