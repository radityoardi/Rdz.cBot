using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using Rdz;
using Rdz.Indi;
using Rdz.cBot.Library.Extensions;

namespace Rdz.Indi
{
    [Levels(-20, 20)]
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None, ScalePrecision = 0)]
    public class CandleDistanceIndicator : Indicator
    {
        [Parameter("Candlestick Source 1", Group = "Source")]
        public DataSeries Source1 { get; set; }
        [Parameter("Candlestick Source 2", Group = "Source")]
        public DataSeries Source2 { get; set; }

        [Parameter("Distance Display", Group = "Display", DefaultValue = enDistanceType.Pips)]
        public enDistanceType DistanceDisplay { get; set; }

        [Output("Main")]
        public IndicatorDataSeries Result { get; set; }


        protected override void Initialize()
        {
            // Initialize and create nested indicators
        }

        public override void Calculate(int index)
        {
            switch (DistanceDisplay)
            {
                case enDistanceType.Pips:
                    Result[index] = this.DistanceInPips(Source2[index], Source1[index]);
                    break;
                case enDistanceType.Points:
                    Result[index] = this.Distance(Source2[index], Source1[index]);
                    break;
                default:
                    break;
            }
        }
    }

    public enum enDistanceType
    {
        Pips,
        Points
    }

}
