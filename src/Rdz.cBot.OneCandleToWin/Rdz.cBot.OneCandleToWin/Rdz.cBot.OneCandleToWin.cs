using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using Rdz.cBot.Library;
using Rdz.cBot.Library.Chart;
using Rdz.cBot.Library.Extensions;

namespace cAlgo.Robots
{
	[Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
	public class OneCandleToWinBot : RdzRobot, IRdzRobot
	{
		[Parameter("Configuration Path", DefaultValue = @"%USERPROFILE%\Documents\Rdz.cBot.OneCandleToWin.json")]
		public string ConfigurationFilePath { get; set; }
		[Parameter("Auto-refresh", DefaultValue = false)]
		public bool AutoRefreshConfiguration { get; set; }

		private Candlestick AnchorCandlestick { get; set; }
		private Candlestick SecondLastCandlestick { get; set; }
		private Candlestick LastCandlestick { get; set; }

		private TradeType Recommendation { get; set; }
		private string Label { get; set; }

		internal Config.OneCandleToWinConfig config { get; set; }

		private bool IsRunning { get; set; }
		private AverageTrueRange atr { get; set; }

		private string LastLog { get; set; }

        protected override void OnStart()
        {
			// Put your initialization logic here
			config = LoadConfiguration<Config.OneCandleToWinConfig>(ExpandConfigFilePath(ConfigurationFilePath));
			IsRunning = false;
			atr = Indicators.AverageTrueRange(14, MovingAverageType.Simple);
        }

        protected override void OnTick()
        {
			// Put your core logic here
			SecondLastCandlestick = new Candlestick(MarketSeries.High.Last(2), MarketSeries.Open.Last(2), MarketSeries.Close.Last(2), MarketSeries.Low.Last(2), Symbol.Digits, MarketSeries.OpenTime.Last(2));
			LastCandlestick = new Candlestick(MarketSeries.High.Last(1), MarketSeries.Open.Last(1), MarketSeries.Close.Last(1), MarketSeries.Low.Last(1), Symbol.Digits, MarketSeries.OpenTime.Last(2));

			if (!IsRunning)
			{
				//for strategy 1
				if (LastCandlestick.Direction == enDirection.Bullish && SecondLastCandlestick.Direction == enDirection.Bullish
					&& LastCandlestick.Height > SecondLastCandlestick.Height
					&& LastCandlestick.HasUpperShadow && SecondLastCandlestick.HasUpperShadow && LastCandlestick.UpperShadowHeight > SecondLastCandlestick.UpperShadowHeight
					&& LastCandlestick.High > SecondLastCandlestick.High
					&& LastCandlestick.Low > SecondLastCandlestick.Low
					&& LastCandlestick.HasLowerShadow
					)
				{
					Recommendation = TradeType.Sell;
					IsRunning = true;
					Label = "S1";
				}

				else if (LastCandlestick.Direction == enDirection.Bearish && SecondLastCandlestick.Direction == enDirection.Bearish
					&& LastCandlestick.Height > SecondLastCandlestick.Height
					&& LastCandlestick.HasLowerShadow && SecondLastCandlestick.HasLowerShadow && LastCandlestick.LowerShadowHeight > SecondLastCandlestick.LowerShadowHeight
					&& LastCandlestick.High < SecondLastCandlestick.High
					&& LastCandlestick.Low < SecondLastCandlestick.Low
					&& LastCandlestick.HasUpperShadow
					)
				{
					Recommendation = TradeType.Buy;
					IsRunning = true;
					Label = "S1";
				}
				/*
				*/

				if (true
					&& LastCandlestick.Height > (atr.Result.Last(2) * 2)
					&& LastCandlestick.HasLowerShadow && LastCandlestick.LowerShadowHeight > (LastCandlestick.RealBodyHeight * 2.5)
					&& LastCandlestick.UpperShadowHeight < (LastCandlestick.RealBodyHeight * 1.5)
					&& LastCandlestick.RealBodyPercentage > 0.10
					)
				{
					if (LastLog != string.Format("{0}", LastCandlestick.ToString()))
					{
						LastLog = string.Format("{0}", LastCandlestick.ToString());
						Print("{0} (Last 2 ATR: {1:0.00000})", LastLog, atr.Result.Last(2));

						if (Time.ToLocalTime().ToString("dd MM yy") == "02 08 19")
						{
						}
						Print("LastCandlestick.Height > (atr.Result.Last(2) * 2): {0}", LastCandlestick.Height > (atr.Result.Last(2) * 2));
						Print("LastCandlestick.LowerShadowHeight > (LastCandlestick.RealBodyHeight * 2.5): {0}", LastCandlestick.LowerShadowHeight > (LastCandlestick.RealBodyHeight * 2.5));
						Print("LastCandlestick.HasLowerShadow: {0}", LastCandlestick.HasLowerShadow);
						Print("LastCandlestick.HasUpperShadow: {0}", LastCandlestick.HasUpperShadow);
						Print("LastCandlestick.UpperShadowHeight < (LastCandlestick.RealBodyHeight * 1.5): {0}", LastCandlestick.UpperShadowHeight < (LastCandlestick.RealBodyHeight * 1.5));
						Print("LastCandlestick.RealBodyPercentage > 0.10: {0}", LastCandlestick.RealBodyPercentage > 0.10);
					}

					Recommendation = TradeType.Buy;
					IsRunning = true;
					Label = "S2";
				}
				/*
				//for strategy 2
				if ( //&& SecondLastCandlestick.Direction == enDirection.Bullish
					((LastCandlestick.HasLowerShadow && LastCandlestick.LowerShadowHeight.Undigit(Symbol.Digits) < (LastCandlestick.RealBodyHeight.Undigit(Symbol.Digits) / 2)) || !LastCandlestick.HasLowerShadow)
					&& LastCandlestick.HasUpperShadow && ((LastCandlestick.RealBodyHeight.Undigit(Symbol.Digits) * 3) > LastCandlestick.UpperShadowHeight.Undigit(Symbol.Digits))
					&& LastCandlestick.RealBodyHeight.Undigit(Symbol.Digits) > atr.Result.LastValue.Undigit(Symbol.Digits)
					)
				{
					Recommendation = TradeType.Sell;
					IsRunning = true;
					Label = "S2";
				} else if ( //&& SecondLastCandlestick.Direction == enDirection.Bearish
					((LastCandlestick.HasUpperShadow && LastCandlestick.UpperShadowHeight.Undigit(Symbol.Digits) < (LastCandlestick.RealBodyHeight.Undigit(Symbol.Digits) / 2)) || !LastCandlestick.HasUpperShadow)
					&& LastCandlestick.HasLowerShadow && ((LastCandlestick.RealBodyHeight.Undigit(Symbol.Digits) * 3) > LastCandlestick.LowerShadowHeight.Undigit(Symbol.Digits))
					&& LastCandlestick.RealBodyHeight.Undigit(Symbol.Digits) > atr.Result.LastValue.Undigit(Symbol.Digits)
					)
				{
					Recommendation = TradeType.Buy;
					IsRunning = true;
					Label = "S2";
				}
				*/

				if (IsRunning)
				{
					AnchorCandlestick = LastCandlestick;
					this.ExecuteMarketOrder(Recommendation, Symbol.Name, 1000, Label);
				}
			}

			if (IsRunning && AnchorCandlestick != null && SecondLastCandlestick.Same(AnchorCandlestick))
			{
				this.ClosePosition(this.Positions.First());
				AnchorCandlestick = null;
				IsRunning = false;
			}
		}

        protected override void OnStop()
        {
            // Put your deinitialization logic here
			if (Positions.Count > 0)
			{
				if (IsRunning)
				{
					foreach (var tp in Positions.OrderByDescending(x => x.NetProfit))
					{
						ClosePositionAsync(tp);
					}
					foreach (var to in PendingOrders)
					{
						CancelPendingOrderAsync(to);
					}
				}
			}
		}
    }
}
