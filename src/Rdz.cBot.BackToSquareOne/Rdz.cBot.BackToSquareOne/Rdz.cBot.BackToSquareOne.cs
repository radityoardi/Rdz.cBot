using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using Rdz.cBot.Library;
using Rdz.cBot.Library.Chart;
using Rdz.cBot.Library.Extensions;

namespace Rdz.cBot
{

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class BTSOBot : RdzRobot, IRdzRobot
	{
		[Parameter("Configuration Path", DefaultValue = @"%USERPROFILE%\Documents\Rdz.cBot.BTSOBot\configuration.json")]
		public string ConfigurationFilePath { get; set; }

		[Parameter("Auto-refresh", DefaultValue = false)]
		public bool AutoRefreshConfiguration { get; set; }

		[Parameter("Crossing Tolerance", DefaultValue = 0.100)]
		public double CrossingTolerance { get; set; }

		[Parameter("Trade Size in Lot", DefaultValue = 1000)]
		public int TradeSize { get; set; }

		private BollingerBands bb { get; set; }
		private SimpleMovingAverage sma1 { get; set; }
		private SimpleMovingAverage sma2 { get; set; }
		private SimpleMovingAverage sma3 { get; set; }
		private AverageTrueRange atr { get; set; }
		private RelativeStrengthIndex rsi { get; set; }
		private bool IsActive { get; set; }
		private Candlestick LastClosedCandlestick { get; set; }
		private Candlestick CurrentCandlestick { get; set; }

		protected override void OnStart()
        {
			// Put your initialization logic here
			bb = Indicators.BollingerBands(MarketSeries.Close, 20, 2, MovingAverageType.Simple);
			sma1 = Indicators.SimpleMovingAverage(MarketSeries.Close, 14);
			sma2 = Indicators.SimpleMovingAverage(MarketSeries.Close, 50);
			sma3 = Indicators.SimpleMovingAverage(MarketSeries.Close, 100);
			atr = Indicators.AverageTrueRange(14, MovingAverageType.Simple);
			rsi = Indicators.RelativeStrengthIndex(MarketSeries.Close, 14);
			IsActive = false;
        }

        protected override void OnTick()
        {
            // Put your core logic here
        }

		protected override void OnBar()
		{
			CurrentCandlestick = this.GetMarketSeries();
			LastClosedCandlestick = this.GetMarketSeries(1);

			bool IsSellSignal = sma2.Result.HasCrossedAbove(sma3.Result, 2) && sma1.Result.IsFalling();// && bb.Main.IsFalling() && sma1.Result.IsFalling();
			bool IsBuySignal = sma2.Result.HasCrossedBelow(sma3.Result, 2) && sma1.Result.IsRising();// && bb.Main.IsRising() && sma1.Result.IsRising();

			//bool IsSellSignal = sma1.Result.HasCrossedAbove(bb.Main, 1) && bb.Main.IsFalling() && sma1.Result.IsFalling();
			//bool IsBuySignal = sma1.Result.HasCrossedBelow(bb.Main, 1) && bb.Main.IsRising() && sma1.Result.IsRising();
			bool IsConcluded = false;
			TradeType tt = TradeType.Sell;
			bool IsWithinTradingTime = false;

			IsWithinTradingTime = Time.ToLocalTime() > Time.ToLocalTime().Date.Add(TimeSpan.Parse("06:00:00")) && Time.ToLocalTime() < Time.ToLocalTime().Date.Add(TimeSpan.Parse("12:00:00"));

			if (!(IsSellSignal && IsBuySignal && !(IsSellSignal && IsBuySignal)))
			{
				IsConcluded = true;
				tt = IsSellSignal ? TradeType.Sell : TradeType.Buy;
			}

			// Put your core logic here
			if (!IsActive && IsConcluded && IsWithinTradingTime)
			{
				/*
				bool IsFootBelow = LastClosedCandlestick.Low < bb.Bottom.Last(1)
					&& LastClosedCandlestick.Low < sma1.Result.Last(1) && LastClosedCandlestick.Low < sma2.Result.Last(1)
					&& LastClosedCandlestick.Low < sma3.Result.Last(1);

				bool IsFootAbove = LastClosedCandlestick.High > bb.Top.Last(1)
					&& LastClosedCandlestick.High > sma1.Result.Last(1) && LastClosedCandlestick.High > sma2.Result.Last(1)
					&& LastClosedCandlestick.High > sma3.Result.Last(1);


				if (IsFootBelow || IsFootAbove)
				{
					IsActive = true;
					this.ExecuteMarketOrder((IsFootBelow ? TradeType.Buy : TradeType.Sell), Symbol.Name, 1000);
				}
				*/
				if (ExecuteMarketOrder(tt, Symbol.Name, TradeSize).IsSuccessful)
				{
					IsActive = true;
				}
			}
			else if (IsActive)
			{
				//IsActive = false;
				foreach (var tp in Positions.OrderByDescending(x => x.NetProfit))
				{
					/*
					if ((tp.TradeType == TradeType.Buy && LastClosedCandlestick.RealBodyHigh > bb.Main.Last(1)) || (tp.TradeType == TradeType.Sell && LastClosedCandlestick.RealBodyLow < bb.Main.Last(1)))
					{
						IsActive = false;
						ClosePositionAsync(tp);
					}
					*/
					if ((tp.TradeType == TradeType.Buy && IsSellSignal) || (tp.TradeType == TradeType.Sell && IsBuySignal) || tp.EntryTime.ToLocalTime().Date < Time.ToLocalTime().Date)
					{
						if (ClosePosition(tp).IsSuccessful)
						{
							IsActive = false;
						}
					}
				}
			}
		}

		protected override void OnStop()
        {
			// Put your deinitialization logic here
			if (Positions.Count > 0)
			{
				foreach (var tp in Positions.OrderByDescending(x => x.NetProfit))
				{
					ClosePositionAsync(tp);
				}
			}
		}
	}
}
