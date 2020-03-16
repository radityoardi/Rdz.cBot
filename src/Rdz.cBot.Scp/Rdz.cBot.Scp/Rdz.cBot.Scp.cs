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

		[Parameter("Trigger Candle Ratio", DefaultValue = 3.0)]
		public double TriggerCandleRatio { get; set; }
		[Parameter("Trigger High-Low Ratio", DefaultValue = 2.0)]
		public double TriggerHLRatio { get; set; }
		[Parameter("Trigger High-Close Ratio", DefaultValue = 2.0)]
		public double TriggerHCRatio { get; set; }
		[Parameter("Volume", DefaultValue = 1000)]
		public int Volume { get; set; }

		[Parameter("Stop Loss in Pips", DefaultValue = 0)]
		public double StopLossPips { get; set; }
		[Parameter("Take Profit in Pips", DefaultValue = 0)]
		public double TakeProfitPips { get; set; }

		private Candlestick cs1 { get; set; }
		private Candlestick cs2 { get; set; }

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
			cs1 = this.GetMarketSeries(1);
			cs2 = this.GetMarketSeries(2);

			if (cs2.Height > 0 && cs1.Height / cs2.Height >= TriggerCandleRatio && cs1.High < cs2.High && cs1.Low < cs2.Low && cs1.CloseToLowHeight / cs2.CloseToLowHeight >= )
			{
				//BUY
				ExecuteMarketOrder(TradeType.Buy, Symbol.Name, Volume, string.Empty, StopLossPips > 0 ? new Nullable<double>(StopLossPips) : null, TakeProfitPips > 0 ? new Nullable<double>(TakeProfitPips) : null);
			}

			if (cs2.Height > 0 && cs1.Height / cs2.Height >= TriggerCandleRatio && cs1.High > cs2.High && cs1.Low > cs2.Low)
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
