using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using Rdz.cBot.Library.Extensions;


namespace Rdz.Indi
{
	[Levels(50, -50)]
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AutoRescale = true, ScalePrecision = 0, AccessRights = AccessRights.None)]
    public class BollingerBandDistanceIndicator : Indicator
    {
        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("MA Type", DefaultValue = MovingAverageType.Exponential)]
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
			// Calculate value at specified index
			// Result[index] = ...
			if (bb.Top[index] != double.NaN && bb.Bottom[index] != double.NaN)
			{
                Result[index] = this.DistanceInPips(bb.Top[index], bb.Bottom[index]);
			}

		}
    }
}
