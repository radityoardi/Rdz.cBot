using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using Rdz.cBot.Library;
using Rdz.cBot.Library.Chart;
using Rdz.cBot.Library.Extensions;


namespace Rdz.Indi.Scp
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, ScalePrecision = 0, AutoRescale = true, AccessRights = AccessRights.None)]
    public class ScpIndicator : Indicator
    {
		[Parameter("Trigger Height Ratio", DefaultValue = 2.5)]
		public double TriggerHeightRatio { get; set; }
		[Parameter("Trigger High-Low Ratio", DefaultValue = 2.0)]
		public double TriggerHLRatio { get; set; }
		[Parameter("Target Points", DefaultValue = 10)]
		public int TargetPoints { get; set; }

		[Output("Buy", LineColor = "Green", PlotType = PlotType.Points, Thickness = 5)]
        public IndicatorDataSeries BuySignal { get; set; }
		[Output("Sell", LineColor = "Red", PlotType = PlotType.Points, Thickness = 5)]
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
			//Print("LastCandlestick: {0} at index: {1}", this.GetMarketSeries(index), index);
			cs1 = this.GetMarketSeries(index);
			cs2 = this.GetMarketSeries(index - 1);
			cs3 = this.GetMarketSeries(index - 2);
			// Calculate value at specified index
			// Result[index] = ...

			if (
				cs2.Height.IsPositive() && cs3.Height.IsPositive()
				&& cs2.Height / cs3.Height >= TriggerHeightRatio
				&& cs2.High < cs3.High && cs2.Low < cs3.Low
				&& cs2.CloseToLowHeight / cs3.CloseToLowHeight >= TriggerHLRatio
			)
			{
				BuySignal[index - 1] = cs2.RealBodyLow;
				SellSignal[index] = this.ShiftPrice(cs1.Open, TargetPoints);
			}


			if (
				cs2.Height.IsPositive() && cs3.Height.IsPositive()
				&& cs2.Height / cs3.Height >= TriggerHeightRatio
				&& cs2.High > cs3.High && cs2.Low > cs3.Low
				&& cs2.HighToCloseHeight / cs3.HighToCloseHeight >= TriggerHLRatio
			)
			{
				SellSignal[index - 1] = cs2.RealBodyHigh;
				BuySignal[index] = this.ShiftPrice(cs1.Open, -TargetPoints);
			}

		}
	}
}
