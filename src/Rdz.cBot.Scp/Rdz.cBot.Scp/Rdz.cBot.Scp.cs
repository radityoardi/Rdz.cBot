using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using Rdz.cBot.Library;
using Rdz.cBot.Library.Chart;
using Rdz.cBot.Library.Extensions;
using Rdz.cBot.Scp;

namespace Rdz.cBot.Scp
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class NewcBot : RdzRobot
	{
		[Parameter("High Frequency Mode", DefaultValue = false)]
		public bool IsHighFreqMode { get; set; }

		[Parameter("Trigger Height Ratio", DefaultValue = 2.5)]
		public double TriggerHeightRatio { get; set; }
		[Parameter("Trigger High-Low Ratio", DefaultValue = 2.0)]
		public double TriggerHLRatio { get; set; }
		[Parameter("Volume", DefaultValue = 1000)]
		public int Volume { get; set; }

		[Parameter("Stop Loss in Pips", DefaultValue = 0)]
		public double StopLossPips { get; set; }
		[Parameter("Take Profit in Pips", DefaultValue = 10)]
		public double TakeProfitPips { get; set; }

		private Candlestick cs1 { get; set; }
		private Candlestick cs2 { get; set; }
		private Candlestick cs3 { get; set; }

		protected override void OnStart()
        {
            // Put your initialization logic here
        }

        protected override void OnTick()
        {
        }

		protected override void OnBar()
		{
			// Put your core logic here

			cs2 = this.GetMarketSeries(1);
			cs3 = this.GetMarketSeries(2);

			Print("{0}", cs2.OpenTime.ToLocalTime().ToString("dd MMM yy HH:mm"));

			if (
				cs2.Height.IsPositive() && cs3.Height.IsPositive()
				&& cs2.Height / cs3.Height >= TriggerHeightRatio
				&& cs2.High < cs3.High && cs2.Low < cs3.Low
				&& cs2.CloseToLowHeight / cs3.CloseToLowHeight >= TriggerHLRatio
			)
			{
				//BUY
				ExecuteMarketOrder(TradeType.Buy, Symbol.Name, Volume, string.Empty, StopLossPips > 0 ? new Nullable<double>(StopLossPips) : null, TakeProfitPips > 0 ? new Nullable<double>(TakeProfitPips) : null);
			}

			if (
				cs2.Height.IsPositive() && cs3.Height.IsPositive()
				&& cs2.Height / cs3.Height >= TriggerHeightRatio
				&& cs2.High > cs3.High && cs2.Low > cs3.Low
				&& cs2.HighToCloseHeight / cs3.HighToCloseHeight >= TriggerHLRatio
			)
			{
				//SELL
				ExecuteMarketOrder(TradeType.Sell, Symbol.Name, Volume, string.Empty, StopLossPips > 0 ? new Nullable<double>(StopLossPips) : null, TakeProfitPips > 0 ? new Nullable<double>(TakeProfitPips) : null);
			}
		}

		protected override void OnStop()
        {
            // Put your deinitialization logic here
        }
    }
}
