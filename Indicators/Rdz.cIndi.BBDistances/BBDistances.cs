using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using Rdz.cTrader.Library;

namespace Rdz.cIndi
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AutoRescale = true, ScalePrecision = 0, AccessRights = AccessRights.None)]
    public class BBDistances : Indicator
    {
        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("MA Type", DefaultValue = MovingAverageType.Simple)]
        public MovingAverageType MaType { get; set; }

        [Parameter("MA Periods", DefaultValue = 14)]
        public int Periods { get; set; }

        [Parameter("Standard Dev", DefaultValue = 2)]
        public int StandardDeviations { get; set; }

        [Output("Bollinger Bands Distance", LineColor = "Red", PlotType = PlotType.Histogram, Thickness = 2, IsHistogram = true)]
        public IndicatorDataSeries Result { get; set; }

        private BollingerBands bb { get; set; }

        protected override void Initialize()
        {
            // Initialize and create nested indicators
            bb = Indicators.BollingerBands(this.Source, this.Periods, this.StandardDeviations, this.MaType);
        }

        public override void Calculate(int index)
        {
            if (!double.IsNaN(bb.Top[index]) && !double.IsNaN(bb.Bottom[index]))
            {
                Result[index] = Symbol.Distance(bb.Top[index], bb.Bottom[index]);
            }
        }
    }
}