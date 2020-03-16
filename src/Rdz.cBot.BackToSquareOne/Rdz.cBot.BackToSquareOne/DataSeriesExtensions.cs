using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using cAlgo.API.Internals;
using Rdz.cBot.Library.Chart;

namespace Rdz.cBot.BackToSquareOne
{
	public static class BotRecommendationExtensions
	{
		internal static void IsCrossing(this UnitRecommendation ur, DataSeries data1, DataSeries data2, string description = "")
		{
			ur.Description = description;
			if (data1.HasCrossedAbove(data2, 1))
			{
				ur.Recommendation = BackToSquareOne.TradeRecommendation.Buy;
			}
			else if (data1.HasCrossedBelow(data2, 1))
			{
				ur.Recommendation = BackToSquareOne.TradeRecommendation.Sell;
			}
		}

		internal static void IsInside(this UnitRecommendation ur, DataSeries data1, DataSeries data2, Candlestick LastClosedCandleStick, string description = "")
		{
			//data1 is EMA8
			//data2 is EMA13
			ur.Description = description;
			if (data2.Last(1) < data1.Last(1)
				&& LastClosedCandleStick.IsBetween(data1.Last(1), data2.Last(1), Library.enCandlestickPart.LowerShadow)
				&& LastClosedCandleStick.IsAbove(data1.Last(1), Library.enCandlestickPart.UpperShadow)
				&& LastClosedCandleStick.Direction == Library.enDirection.Bearish)
			{
				ur.Recommendation = BackToSquareOne.TradeRecommendation.Buy;
			}
			else if (data1.Last(1) < data2.Last(1)
				&& LastClosedCandleStick.IsBetween(data1.Last(1), data2.Last(1), Library.enCandlestickPart.UpperShadow)
				&& LastClosedCandleStick.IsBelow(data1.Last(1), Library.enCandlestickPart.LowerShadow)
				&& LastClosedCandleStick.Direction == Library.enDirection.Bullish)
			{
				ur.Recommendation = BackToSquareOne.TradeRecommendation.Sell;
			}
			else ur.Recommendation = BackToSquareOne.TradeRecommendation.Nothing;
		}

		internal static void IsMovingFast(this UnitRecommendation ur, DataSeries data, List<Candlestick> Candlesticks, string description = "")
		{
			ur.Description = description;
			if (Candlesticks.Select((item, index) => new { CandleStick = item, Index = index }).All(x => x.CandleStick.IsBelow(data.Last(x.Index + 2), Library.enCandlestickPart.All)))
			{
				ur.Recommendation = TradeRecommendation.Sell;
			}
			else if (Candlesticks.Select((item, index) => new { CandleStick = item, Index = index }).All(x => x.CandleStick.IsAbove(data.Last(x.Index + 2), Library.enCandlestickPart.All)))
			{
				ur.Recommendation = TradeRecommendation.Buy;
			}
			else ur.Recommendation = BackToSquareOne.TradeRecommendation.Nothing;
		}

		internal static void IsDistant(this UnitRecommendation ur, DataSeries data1, DataSeries data2, double distanceby, string description = "")
		{
			ur.Description = description;
			if (data1.Last(1) > data2.Last(2) && data1.Last(1) - data2.Last(1) > distanceby)
			{
				ur.Recommendation = BackToSquareOne.TradeRecommendation.Buy;
			}
			else if (data2.Last(1) > data1.Last(2) && data2.Last(1) - data1.Last(1) > distanceby)
			{
				ur.Recommendation = BackToSquareOne.TradeRecommendation.Sell;
			}
		}
		internal static void IsHolding(this UnitRecommendation ur, DataSeries data1, int barsToCheck, string description = "")
		{
			ur.IsHolding(data1, barsToCheck, 70, 30, description);
		}
		internal static void IsHolding(this UnitRecommendation ur, DataSeries data1, int barsToCheck, double highvalue, double lowvalue, string description = "")
		{
			ur.IsHolding(data1, barsToCheck, 70, 30, false, description);
		}
		internal static void IsHolding(this UnitRecommendation ur, DataSeries data1, int barsToCheck, double highvalue, double lowvalue, bool Inverse = false, string description = "")
		{
			ur.Description = description;
			ur.Recommendation = TradeRecommendation.Nothing; //need to reset
			List<TradeRecommendation> tr = new List<TradeRecommendation>();
			for (int i = 1; i <= barsToCheck; i++)
			{
				if (data1.Last(i) <= lowvalue) tr.Add(Inverse ? TradeRecommendation.Buy : TradeRecommendation.Sell);
				else if (data1.Last(i) >= highvalue) tr.Add(Inverse ? TradeRecommendation.Sell : TradeRecommendation.Buy);
			}
			if (tr.All(x => x == TradeRecommendation.Buy)) ur.Recommendation = TradeRecommendation.Buy;
			else if (tr.All(x => x == TradeRecommendation.Sell)) ur.Recommendation = TradeRecommendation.Sell;
		}

	}
}
