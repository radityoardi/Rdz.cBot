using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using Rdz.cBot.Library;
using Rdz.cBot.Library.Chart;
using Rdz.cBot.Library.Extensions;
using Rdz.cBot.BackToSquareOne;
using System.Collections.Generic;

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

		[Parameter("Stop Loss", DefaultValue = 100)]
		public double StopLoss { get; set; }
		[Parameter("Take Profit", DefaultValue = 60)]
		public double TakeProfit { get; set; }

		[Parameter("Inverse", DefaultValue = false)]
		public bool InverseRecommendation { get; set; }
		[Parameter("Single Trade at a time", DefaultValue = true)]
		public bool SingleTrade { get; set; }
		[Parameter("Close when reversal", DefaultValue = false)]
		public bool CloseWhenReversal { get; set; }
		[Parameter("Use Take Profit", DefaultValue = false)]
		public bool UseTakeProfit { get; set; }
		[Parameter("Use Time Limiter", DefaultValue = false)]
		public bool UseTimeLimiter { get; set; }

		private BollingerBands bb { get; set; }
		private SimpleMovingAverage sma1 { get; set; }
		private SimpleMovingAverage sma2 { get; set; }
		private SimpleMovingAverage sma3 { get; set; }
		private SimpleMovingAverage sma4 { get; set; }
		private ExponentialMovingAverage ema1 { get; set; }
		private ExponentialMovingAverage ema2 { get; set; }
		private ExponentialMovingAverage ema3 { get; set; }
		private ExponentialMovingAverage ema1H1 { get; set; }
		private ExponentialMovingAverage ema2H1 { get; set; }
		private MarketSeries msH1 { get; set; }

		private AverageTrueRange atr { get; set; }
		private RelativeStrengthIndex rsi { get; set; }
		private bool IsActive { get; set; }
		private Candlestick LastClosedCandlestick { get; set; }
		private List<Candlestick> Last4ClosedCandlesticks { get; set; }
		private Candlestick CurrentCandlestick { get; set; }

		private RapidScalpingInfo ScalpingInfo { get; set; }


		private Rdz.cBot.BackToSquareOne.BotRecommendation BotRecommendation { get; set; }
		private ChartText Signals { get; set; }

		public BTSOBot()
		{
			IsActive = false;
		}
		protected void SetRecommendation()
		{
			/*
			 * We need signal: 5
			 * When SMA14 crosses above/below BB (typically BB middle is SMA20) --> cancel. Too noisy.
			 * When SMA14 crosses above/below SMA50
			 * When SMA50 crosses above/below SMA100
			 * When SMA75 crosses above/below SMA100
			 * When SMA75 and SMA100 range is at least 0.050
			 * When SMA50 and SMA100 range is at least 0.120
			 * When RSI between 30 and 70
			*/

			/*
			//Signal 1
			BotRecommendation.SmallUnits[0].IsCrossing(sma1.Result, sma2.Result, "SMA5 with SMA14");
			*/
			//Signal 1
			BotRecommendation.SmallUnits[0].IsCrossing(ema1.Result, ema2.Result, "EMA8 with EMA13");
			//Signal 2
			BotRecommendation.SmallUnits[1].IsCrossing(ema1.Result, ema3.Result, "EMA8 with EMA21");
			//Signal 3
			BotRecommendation.SmallUnits[2].IsCrossing(ema2.Result, ema3.Result, "EMA13 with EMA21");

			//Signal 4
			BotRecommendation.SmallUnits[3].IsInside(ema1.Result, ema3.Result, LastClosedCandlestick, "Candle Inside");
			//Signal 5
			BotRecommendation.SmallUnits[4].IsDistant(ema1.Result, ema2.Result, 0.00015, "EMA8 with EMA13 > 12");
			//Signal 6
			BotRecommendation.SmallUnits[5].IsDistant(ema2.Result, ema3.Result, 0.00020, "EMA13 with EMA21 > 15");
			//Signal 7
			BotRecommendation.SmallUnits[6].IsMovingFast(ema1.Result, Last4ClosedCandlesticks, "Last5 is FAST");
			/*
			//Signal 8
			BotRecommendation.SmallUnits[7].IsCrossing(ema1H1.Result, ema2H1.Result, "H1 for EMA8 with EMA21");
			//Signal 9
			BotRecommendation.SmallUnits[8].IsDistant(ema1H1.Result, ema2H1.Result, 0.00045, "H1 EMA8 with EMA21 > 30");
			*/
		}

		protected void DrawSomething()
		{
			/*
			string x = string.Format("Signal 1: {0}\r\nSignal 2: {1}\r\nSignal 3: {2}\r\nSignal 4: {3}\r\nSignal 5: {4}\r\nSignal 6: {5}\r\nALL: {6}",
				BotRecommendation.SmallUnits[0].Recommendation.ToString(),
				BotRecommendation.SmallUnits[1].Recommendation.ToString(),
				BotRecommendation.SmallUnits[2].Recommendation.ToString(),
				BotRecommendation.SmallUnits[3].Recommendation.ToString(),
				BotRecommendation.SmallUnits[4].Recommendation.ToString(),
				BotRecommendation.SmallUnits[5].Recommendation.ToString(),
				BotRecommendation.Recommmendation.ToString()
				);
			*/
			string x = string.Empty;
			for (int i = 0; i < BotRecommendation.SmallUnits.Count; i++)
			{
				x += string.Format("{0} [{1}]\r\n", BotRecommendation.SmallUnits[i].Recommendation.ToString(), BotRecommendation.SmallUnits[i].Description ?? i.ToString());
			}
			x += string.Format("ALL: {0}", BotRecommendation.Recommmendation.ToString());
			Print(x);
			Signals = Chart.DrawText("Signals", x, MarketSeries.OpenTime.Last(100), MarketSeries.Close.Last(1), Color.White);
			
		}
		protected override void OnStart()
        {
			CurrentCandlestick = this.GetMarketSeries();
			LastClosedCandlestick = this.GetMarketSeries(1);
			Last4ClosedCandlesticks = this.GetMarketSeries(new int[] { 2, 3, 4, 5, 6 });

			BotRecommendation = new BackToSquareOne.BotRecommendation(7, InverseRecommendation);
			/*
			BotRecommendation = new BackToSquareOne.BotRecommendation(1, InverseRecommendation);
			*/
			// Put your initialization logic here
			bb = Indicators.BollingerBands(MarketSeries.Close, 20, 2, MovingAverageType.Simple);
			/*
			sma1 = Indicators.SimpleMovingAverage(MarketSeries.Close, 5);
			sma2 = Indicators.SimpleMovingAverage(MarketSeries.Close, 14);
			*/
			/*
			sma1 = Indicators.SimpleMovingAverage(MarketSeries.Close, 14);
			sma2 = Indicators.SimpleMovingAverage(MarketSeries.Close, 50);
			sma3 = Indicators.SimpleMovingAverage(MarketSeries.Close, 75);
			sma4 = Indicators.SimpleMovingAverage(MarketSeries.Close, 100);
			*/
			ema1 = Indicators.ExponentialMovingAverage(MarketSeries.Close, 8);
			ema2 = Indicators.ExponentialMovingAverage(MarketSeries.Close, 13);
			ema3 = Indicators.ExponentialMovingAverage(MarketSeries.Close, 21);


			atr = Indicators.AverageTrueRange(14, MovingAverageType.Simple);
			rsi = Indicators.RelativeStrengthIndex(MarketSeries.Close, 14);

			msH1 = MarketData.GetSeries(TimeFrame.Hour);
			ema1H1 = Indicators.ExponentialMovingAverage(msH1.Close, 8);
			ema2H1 = Indicators.ExponentialMovingAverage(msH1.Close, 21);

			SetRecommendation();
			DrawSomething();
        }

        protected override void OnTick()
        {
			// Put your core logic here
			CurrentCandlestick = this.GetMarketSeries();
			LastClosedCandlestick = this.GetMarketSeries(1);
			Last4ClosedCandlesticks = this.GetMarketSeries(new int[] { 2, 3, 4, 5, 6 });

			/*
			if (ScalpingInfo != null)
				Print("ScalpingInfo >> {0}", ScalpingInfo.ToString());
			*/


			if (Positions.Count == 0 && PendingOrders.Count == 0)
			{
				ScalpingInfo = null;
			}

			if (Positions.Count > 0)
			{
				foreach (var tp in Positions)
				{
					if (ScalpingInfo != null && tp.StopLoss == null)
					{
						tp.ModifyStopLossPrice(ScalpingInfo.NegativeLine);
						tp.ModifyTrailingStop(true);
					}
				}
			}


			if (SingleTrade && ScalpingInfo != null && Positions.Count > 0 && PendingOrders.Count == 0)
			{
				if (ScalpingInfo.Direction == enDirection.Bullish)
				{
					if (Bid > ScalpingInfo.TakeProfit1)
					{
						foreach (var pos in Positions)
						{
							if (Bid > ScalpingInfo.TakeProfit1 && pos.StopLoss < ScalpingInfo.TriggerLine)
							{
								/*
								pos.ModifyStopLossPrice(ScalpingInfo.TakeProfit0);
								*/
								Print("Price reaching TakeProfit1, updated to {0}", ScalpingInfo.TriggerLine);
							}
						}
					}
					else if (Bid > ScalpingInfo.TakeProfit2)
					{
						foreach (var pos in Positions)
						{
							if (Bid > ScalpingInfo.TakeProfit2 && pos.StopLoss < ScalpingInfo.TakeProfit1)
							{
								/*
								pos.ModifyStopLossPrice(ScalpingInfo.TakeProfit1);
								*/
								Print("Price reaching TakeProfit2, updated to {0} with trailing stop", ScalpingInfo.TakeProfit1);
							}
						}
					}
				}
				else if (ScalpingInfo.Direction == enDirection.Bearish)
				{
					if (Ask < ScalpingInfo.TakeProfit1)
					{
						foreach (var pos in Positions)
						{
							if (Ask < ScalpingInfo.TakeProfit1 && pos.StopLoss > ScalpingInfo.TriggerLine)
							{
								//pos.ModifyStopLossPrice(ScalpingInfo.TakeProfit0);
								Print("Price reaching TakeProfit1, updated to {0}", ScalpingInfo.TriggerLine);
							}
						}
					}
					else if (Ask < ScalpingInfo.TakeProfit2)
					{
						foreach (var pos in Positions)
						{
							if (Ask < ScalpingInfo.TakeProfit2 && pos.StopLoss > ScalpingInfo.TakeProfit1)
							{
								//pos.ModifyStopLossPrice(ScalpingInfo.TakeProfit1);
								Print("Price reaching TakeProfit2, updated to {0} with trailing stop", ScalpingInfo.TakeProfit1);
							}
						}
					}
				}
			}
		}

		protected override void OnBar()
		{
			bool IsWithinTradingTimeA = false;
			bool IsWithinTradingTimeB = false;
			IsWithinTradingTimeA = Time.ToLocalTime() > Time.ToLocalTime().Date.Add(TimeSpan.Parse("06:00:00")) && Time.ToLocalTime() < Time.ToLocalTime().Date.Add(TimeSpan.Parse("07:00:00"));
			IsWithinTradingTimeB = Time.ToLocalTime() > Time.ToLocalTime().Date.Add(TimeSpan.Parse("14:00:00")) && Time.ToLocalTime() < Time.ToLocalTime().Date.Add(TimeSpan.Parse("17:00:00"));

			CurrentCandlestick = this.GetMarketSeries();
			LastClosedCandlestick = this.GetMarketSeries(1);
			Last4ClosedCandlesticks = this.GetMarketSeries(new int[] { 2, 3, 4, 5, 6 });

			SetRecommendation();
			DrawSomething();

			if (SingleTrade && Positions.Count == 0 && PendingOrders.Count == 0 && LastClosedCandlestick.Height > atr.Result.Last(1)) //only on Single Trade
			{
				switch (BotRecommendation.Recommmendation)
				{
					case TradeRecommendation.Buy:
						ScalpingInfo = new RapidScalpingInfo(this, this.GetMarketSeries(1), enDirection.Bullish, 5, 10);
						Print("ScalpingInfo >> {0}", ScalpingInfo.ToString());
						var tr = PlaceStopOrder(TradeType.Buy, Symbol.Name, TradeSize, ScalpingInfo.TriggerLine);
						break;
					case TradeRecommendation.Sell:
						ScalpingInfo = new RapidScalpingInfo(this, this.GetMarketSeries(1), enDirection.Bearish, 5, 10);
						Print("ScalpingInfo >> {0}", ScalpingInfo.ToString());
						PlaceStopOrder(TradeType.Sell, Symbol.Name, TradeSize, ScalpingInfo.TriggerLine);
						break;
				}
			}

			if (SingleTrade && PendingOrders.Count > 0 && Positions.Count == 0 && ScalpingInfo != null)
			{
				if (ScalpingInfo.Direction == enDirection.Bullish && LastClosedCandlestick.Low < ema3.Result.Last(1))
				{
					CloseAllOrders();
					ScalpingInfo = null;
					Print("Pending Orders cancelled due to direction change.");
				}
				else if (ScalpingInfo.Direction == enDirection.Bearish && LastClosedCandlestick.High > ema3.Result.Last(1))
				{
					CloseAllOrders();
					ScalpingInfo = null;
					Print("Pending Orders cancelled due to direction change.");
				}
			}
#if ANOTHER


			//closing when
			if (SingleTrade && Positions.Count > 0 && CloseWhenReversal)
			{
				if ((BotRecommendation.Recommmendation == BackToSquareOne.TradeRecommendation.Buy && Positions.First().TradeType == TradeType.Sell) || (BotRecommendation.Recommmendation == BackToSquareOne.TradeRecommendation.Sell && Positions.First().TradeType == TradeType.Buy))
				{
					CloseAllPositions();
				}
			}

			//Open the trade
			if ((!UseTimeLimiter || (UseTimeLimiter && (IsWithinTradingTimeA || IsWithinTradingTimeB))) && (!SingleTrade || (SingleTrade && Positions.Count == 0)))
			{
				if (BotRecommendation.Recommmendation == BackToSquareOne.TradeRecommendation.Buy)// && Positions.Count == 0)
				{
					if (UseTakeProfit)
					{
						this.ExecuteMarketOrder(TradeType.Buy, Symbol.Name, TradeSize, "", StopLoss, TakeProfit, null, true);
					}
					else
					{
						this.ExecuteMarketOrder(TradeType.Buy, Symbol.Name, TradeSize, "", StopLoss, null, null, true);
					}

				}
				else if (BotRecommendation.Recommmendation == BackToSquareOne.TradeRecommendation.Sell)// && Positions.Count == 0)
				{
					if (UseTakeProfit)
					{
						this.ExecuteMarketOrder(TradeType.Sell, Symbol.Name, TradeSize, "", StopLoss, TakeProfit, null, true);
					}
					else
					{
						this.ExecuteMarketOrder(TradeType.Sell, Symbol.Name, TradeSize, "", StopLoss, null, null, true);
					}
				}
			}
#endif
#if NONE
			bool IsSellSignal = sma2.Result.HasCrossedAbove(sma4.Result, 2) && sma1.Result.IsFalling();// && bb.Main.IsFalling() && sma1.Result.IsFalling();
			bool IsBuySignal = sma2.Result.HasCrossedBelow(sma4.Result, 2) && sma1.Result.IsRising();// && bb.Main.IsRising() && sma1.Result.IsRising();

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
			/*
			else if (IsActive)
			{
				//IsActive = false;
				foreach (var tp in Positions.OrderByDescending(x => x.NetProfit))
				{
					if ((tp.TradeType == TradeType.Buy && LastClosedCandlestick.RealBodyHigh > bb.Main.Last(1)) || (tp.TradeType == TradeType.Sell && LastClosedCandlestick.RealBodyLow < bb.Main.Last(1)))
					{
						IsActive = false;
						ClosePositionAsync(tp);
					}
					if ((tp.TradeType == TradeType.Buy && IsSellSignal) || (tp.TradeType == TradeType.Sell && IsBuySignal) || tp.EntryTime.ToLocalTime().Date < Time.ToLocalTime().Date)
					{
						if (ClosePosition(tp).IsSuccessful)
						{
							IsActive = false;
						}
					}
				}
			}
			*/
#endif

		}

		protected void CloseEverything()
		{
			CloseAllOrders();
			CloseAllPositions();
		}
		protected void CloseAllOrders()
		{
			if (PendingOrders.Count > 0)
			{
				foreach (var tp in PendingOrders.OrderByDescending(x => x.Id))
				{
					CancelPendingOrder(tp);
				}
			}
		}
		protected void CloseAllPositions()
		{
			if (Positions.Count > 0)
			{
				foreach (var tp in Positions.OrderByDescending(x => x.NetProfit))
				{
					ClosePosition(tp);
				}
			}
		}

		protected override void OnStop()
        {
			// Put your deinitialization logic here
			CloseAllPositions();
		}
	}
}
