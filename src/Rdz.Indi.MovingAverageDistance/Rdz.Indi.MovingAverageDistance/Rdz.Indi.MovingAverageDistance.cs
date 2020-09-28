using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using Rdz.cBot.Library.Extensions;
using System.Runtime.Remoting.Channels;

namespace Rdz.Indi
{
    [Levels(-20, 20)]
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None, ScalePrecision = 0)]
    public class MovingAverageDistanceIndicator : Indicator
    {
        #region Input: Moving Average A
        [Parameter("MA Source", Group = "Moving Average A")]
        public DataSeries SourceA { get; set; }

        [Parameter("MA Type", Group = "Moving Average A", DefaultValue = MovingAverageType.Exponential)]
        public MovingAverageType MaTypeA { get; set; }

        [Parameter("MA Periods", Group = "Moving Average A", DefaultValue = 13, MinValue = 1)]
        public int PeriodsA { get; set; }
        #endregion

        #region Input: Moving Average B
        [Parameter("MA Source", Group = "Moving Average B")]
        public DataSeries SourceB { get; set; }

        [Parameter("MA Type", Group = "Moving Average B", DefaultValue = MovingAverageType.Exponential)]
        public MovingAverageType MaTypeB { get; set; }

        [Parameter("MA Periods", Group = "Moving Average B", DefaultValue = 21, MinValue = 1)]
        public int PeriodsB { get; set; }
        #endregion

        [Output("Main", LineColor = "#008000", IsHistogram = true, PlotType = PlotType.Histogram)]
        public IndicatorDataSeries Result { get; set; }


        private MovingAverage MovingAverageA { get; set; }
        private MovingAverage MovingAverageB { get; set; }

        protected override void Initialize()
        {
            // Initialize and create nested indicators
            MovingAverageA = this.Indicators.MovingAverage(SourceA, PeriodsA, MaTypeA);
            MovingAverageB = this.Indicators.MovingAverage(SourceB, PeriodsB, MaTypeB);
        }

        public override void Calculate(int index)
        {
            Result[index] = this.Distance(MovingAverageA.Result[index], MovingAverageB.Result[index]);
        }
    }
}
