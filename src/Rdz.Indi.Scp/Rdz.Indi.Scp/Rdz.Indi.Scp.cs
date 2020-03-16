using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using Rdz.cBot.Library.Chart;
using Rdz.cBot.Library.Extensions;


namespace Rdz.Indi.Scp
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, ScalePrecision = 0, AutoRescale = true, AccessRights = AccessRights.None)]
    public class ScpIndicator : Indicator
    {
		[Parameter("Trigger Candle Ratio", DefaultValue = 3.0)]
		public double TriggerHeightRatio { get; set; }
		[Parameter("Trigger High-Low Ratio", DefaultValue = 2.0)]
		public double TriggerHLRatio { get; set; }

		[Output("Buy", LineColor = "Green", PlotType = PlotType.Histogram, Thickness = 3, IsHistogram = true)]
        public IndicatorDataSeries BuySignal { get; set; }
		[Output("Sell", LineColor = "Red", PlotType = PlotType.Histogram, Thickness = 3, IsHistogram = true)]
		public IndicatorDataSeries SellSignal { get; set; }

		private Candlestick cs1 { get; set; }
		private Candlestick cs2 { get; set; }
		private Candlestick cs3 { get; set; }


		protected override void Initialize()
        {
            // Initialize and create nested indicators
        }

        public override void Calculate(int index)
        {
			cs1 = this.GetMarketSeries(1);
			cs1 = this.GetMarketSeries(2);
			// Calculate value at specified index
			// Result[index] = ...

			if (cs2.Height > 0 && cs1.Height / cs2.Height >= TriggerHeightRatio && cs1.High < cs2.High && cs1.Low < cs2.Low && cs1.CloseToLowHeight / cs2.CloseToLowHeight >= TriggerHLRatio)
			{
				BuySignal[index] = 2;
			}
			else
				BuySignal[index] = 1;


			if (cs2.Height > 0 && cs1.Height / cs2.Height >= TriggerHeightRatio && cs1.High > cs2.High && cs1.Low > cs2.Low && cs1.HighToCloseHeight / cs2.HighToCloseHeight >= TriggerHLRatio)
			{
				SellSignal[index] = 2;
			}
			else
				SellSignal[index] = 1;

		}
	}
}
