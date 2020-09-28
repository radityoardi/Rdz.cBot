using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using Rdz.cBot.Library;
using Rdz.cBot.Library.Chart;
using Rdz.cBot.Library.Extensions;


namespace Rdz.Indi.ActiveCandlesFinder
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, ScalePrecision = 0, AutoRescale = true, AccessRights = AccessRights.None)]
    public class ActiveCandlesFinderIndicator : Indicator
    {
        [Parameter(DefaultValue = 25)]
        public int Threshold { get; set; }
        [Parameter("Open Time", DefaultValue = "17:00")]
        public string sOpenTime { get; set; }
        public TimeSpan OpenTime
        {
            get
            {
                TimeSpan _os = TimeSpan.Zero;
                if (TimeSpan.TryParse(sOpenTime, out _os))
                {
                    return _os;
                }
                return TimeSpan.Zero;
            }
        }
        [Parameter("Use Open Time", DefaultValue = false)]
        public bool UseSpecificOpenTime { get; set; }

        [Output("Above Threshold", LineColor = "Green", PlotType = PlotType.Histogram, Thickness = 3, IsHistogram = true)]
        public IndicatorDataSeries AboveThreshold { get; set; }
        [Output("Below Threshold", LineColor = "Red", PlotType = PlotType.Histogram, Thickness = 3, IsHistogram = true)]
        public IndicatorDataSeries BelowThreshold { get; set; }


        protected override void Initialize()
        {
            // Initialize and create nested indicators
        }

        public override void Calculate(int index)
        {
            // Calculate value at specified index
            // Result[index] = ...
            Candlestick currentCandlestick = this.GetMarketSeries(index);

            if (!UseSpecificOpenTime || (UseSpecificOpenTime && sOpenTime == currentCandlestick.OpenTime.ToLocalTime().ToString("HH:mm")))
            {
                var Heights = currentCandlestick.RealBodyHeight.Undigit(this.Symbol.Digits);
                if (Heights > Threshold)
                {
                    AboveThreshold[index] = Heights;
                }
                else
                {
                    BelowThreshold[index] = Heights;
                }
            }
        }
    }
}
