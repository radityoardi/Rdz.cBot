using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using Rdz.cBot.Library.Extensions;

namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class NewIndicator : Indicator
    {
        [Parameter("Indicator Offset", DefaultValue = 5)]
        public int Offset { get; set; }
        private double IndicatorOffset
        {
            get
            {
                return Symbol.PipSize * Offset;
            }
        }

        [Parameter("Period", DefaultValue = 20)]
        public int Period { get; set; }

        [Output("Main")]
        public IndicatorDataSeries Result { get; set; }


        protected override void Initialize()
        {
            // Initialize and create nested indicators
        }

        public override void Calculate(int index)
        {
            // Calculate value at specified index
            // Result[index] = ...
            var lastPeriodCandlesticks = this.GetMarketSeries(index, index - Period);
        }
    }
}
