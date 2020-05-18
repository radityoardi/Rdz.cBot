using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo;
using cAlgo.API;
using cAlgo.API.Internals;
using Rdz.cBot.Library.Chart;

namespace Rdz.cBot.Library.Extensions
{
	public static class GenericExtensions
	{
		public static List<Candlestick> GetMarketSeries(this Robot robot, List<int> indices)
		{
			return GetMarketSeries(robot, indices.ToArray());
		}
		public static List<Candlestick> GetMarketSeries(this Robot robot, int[] indices)
		{
			List<Candlestick> cdlret = new List<Candlestick>();
			if (indices != null && indices.Length > 0)
			{
				for (int i = 0; i < indices.Length; i++)
				{
					//cdlret.Add(new Candlestick(robot.MarketSeries.High.Last(indices[i]), robot.MarketSeries.Open.Last(indices[i]), robot.MarketSeries.Close.Last(indices[i]), robot.MarketSeries.Low.Last(indices[i]), robot.Symbol.Digits, robot.MarketSeries.OpenTime.Last(indices[i])));
					cdlret.Add(new Candlestick(robot.Bars.HighPrices[indices[i]], robot.Bars.OpenPrices[indices[i]], robot.Bars.ClosePrices[indices[i]], robot.Bars.LowPrices[indices[i]], robot.Symbol.Digits, robot.Bars.OpenTimes[indices[i]]));
				}
			}
			return cdlret;
		}
		public static Candlestick GetMarketSeries(this Robot robot, int index = -1)
		{
			if (index > -1)
			{
				//return new Candlestick(robot.MarketSeries.High.Last(index), robot.MarketSeries.Open.Last(index), robot.MarketSeries.Close.Last(index), robot.MarketSeries.Low.Last(index), robot.Symbol.Digits, robot.MarketSeries.OpenTime.Last(index));
				return new Candlestick(robot.Bars.HighPrices[index], robot.Bars.OpenPrices[index], robot.Bars.ClosePrices[index], robot.Bars.LowPrices[index], robot.Symbol.Digits, robot.Bars.OpenTimes[index]);
			} else {
				//return new Candlestick(robot.MarketSeries.High.LastValue, robot.MarketSeries.Open.LastValue, robot.MarketSeries.Close.LastValue, robot.MarketSeries.Low.LastValue, robot.Symbol.Digits, robot.MarketSeries.OpenTime.LastValue);
				return new Candlestick(robot.Bars.HighPrices.LastValue, robot.Bars.OpenPrices.LastValue, robot.Bars.ClosePrices.LastValue, robot.Bars.LowPrices.LastValue, robot.Symbol.Digits, robot.Bars.OpenTimes.LastValue);
			}
		}
		public static List<Candlestick> GetMarketSeries(this Indicator indi, List<int> indices)
		{
			return GetMarketSeries(indi, indices.ToArray());
		}
		public static List<Candlestick> GetMarketSeries(this Indicator indi, int[] indices)
		{
			List<Candlestick> cdlret = new List<Candlestick>();
			if (indices != null && indices.Length > 0)
			{
				for (int i = 0; i < indices.Length; i++)
				{
					//cdlret.Add(new Candlestick(indi.MarketSeries.High.Last(indices[i]), indi.MarketSeries.Open.Last(indices[i]), indi.MarketSeries.Close.Last(indices[i]), indi.MarketSeries.Low.Last(indices[i]), indi.Symbol.Digits, indi.MarketSeries.OpenTime.Last(indices[i])));
					cdlret.Add(new Candlestick(indi.Bars.HighPrices[indices[i]], indi.Bars.OpenPrices[indices[i]], indi.Bars.ClosePrices[indices[i]], indi.Bars.LowPrices[indices[i]], indi.Symbol.Digits, indi.Bars.OpenTimes[indices[i]]));
				}
			}
			return cdlret;
		}
		public static Candlestick GetMarketSeries(this Indicator indi, int index = -1)
		{
			if (index > -1)
			{
				//return new Candlestick(indi.MarketSeries.High.Last(index), indi.MarketSeries.Open.Last(index), indi.MarketSeries.Close.Last(index), indi.MarketSeries.Low.Last(index), indi.Symbol.Digits, indi.MarketSeries.OpenTime.Last(index));
				return new Candlestick(indi.Bars.HighPrices[index], indi.Bars.OpenPrices[index], indi.Bars.ClosePrices[index], indi.Bars.LowPrices[index], indi.Symbol.Digits, indi.Bars.OpenTimes[index]);
			}
			else
			{
				//return new Candlestick(indi.MarketSeries.High.LastValue, indi.MarketSeries.Open.LastValue, indi.MarketSeries.Close.LastValue, indi.MarketSeries.Low.LastValue, indi.Symbol.Digits, indi.MarketSeries.OpenTime.LastValue);
				return new Candlestick(indi.Bars.HighPrices.LastValue, indi.Bars.OpenPrices.LastValue, indi.Bars.ClosePrices.LastValue, indi.Bars.LowPrices.LastValue, indi.Symbol.Digits, indi.Bars.OpenTimes.LastValue);
			}
		}
		public static int CalculateInterval(this Robot robot, double price1, double price2)
		{
			int gap = (int)((price1 - price2) / robot.Symbol.TickSize);
			//return (gap < 0 ? gap * -1 : gap); //returns to always positive number
			return Math.Abs(gap);
		}
		
		public static double CalculateIntervalAgainst(this double price1, double price2)
		{
			double gap = price1 - price2;
			return Math.Abs(gap);
		}

		public static double CalculatePips(this Robot robot, double price1, double price2)
		{
			return Math.Abs((price1 - price2) / robot.Symbol.PipSize);
		}

		public static double ShiftPrice(this Robot robot, double fromPrice, int points)
		{
			var priceDiff = robot.Symbol.TickSize * points;
			return fromPrice + priceDiff;
		}

		public static double Undigit(this double value, int digit)
		{
			return value * Math.Pow(10, digit);
		}

		public static double LotToVolume(this Robot robot, double LotSize)
		{
			return robot.Symbol.NormalizeVolumeInUnits(robot.Symbol.LotSize * LotSize);
		}

		public static bool IsNotEmpty(this string input)
		{
			return input != null && !string.IsNullOrEmpty(input) && !string.IsNullOrWhiteSpace(input);
		}

		public static DateTime ParseDateTime(this string input, string DateFormat)
		{
			DateTime dt;
			if (DateTime.TryParseExact(input, DateFormat, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt))
			{
				return dt;
			}
			return DateTime.MinValue;
		}

		public static double ProjectedProfit(this Position input, double projectedPrice)
		{
			switch (input.TradeType)
			{
				case TradeType.Buy:
					return (projectedPrice - input.EntryPrice) * input.VolumeInUnits;
				case TradeType.Sell:
					return (input.EntryPrice - projectedPrice) * input.VolumeInUnits;
				default:
					return double.NaN;
			}
		}

		public static double ProjectedNetProfit(this Position input, double projectedPrice)
		{
			double pp = input.ProjectedProfit(projectedPrice);
			if (double.IsNaN(pp))
			{
				return double.NaN;
			} else
			{
				return (pp - input.Commissions - input.Swap);
			}
		}

		public static bool IsPositive(this double input)
		{
			return (!double.IsNaN(input) && input > 0);
		}
		public static bool IsPositive(this int input)
		{
			return (input > 0);
		}
		public static bool IsNegative(this double input)
		{
			return (!double.IsNaN(input) && input < 0);
		}
		public static bool IsNegative(this int input)
		{
			return (input < 0);
		}
		public static bool IsZero(this double input)
		{
			return (!double.IsNaN(input) && input == 0);
		}
		public static bool IsZero(this int input)
		{
			return (input == 0);
		}
		public static double CutHalf(this double input, int digits = 5)
		{
			return (!double.IsNaN(input) ? Math.Round(input / 2, digits) : input);
		}

		public static double FindCenterAgainst(this double inputA, double inputB, int digits = 5)
		{
			double interval = inputA.CalculateIntervalAgainst(inputB);
			double intervalMid = interval.CutHalf(digits);
			return (inputA > inputB ? inputA - intervalMid : inputB - intervalMid);
		}

		public static double FindNetBreakEvenPrice(this IEnumerable<Position> inputPositions, double anchorPriceA, double anchorPriceB, int digits = 5)
		{
			return FindTargetPrice(inputPositions, anchorPriceA, anchorPriceB, digits, 0);
		}
		public static double FindTargetPrice(this IEnumerable<Position> inputPositions, double anchorPriceA, double anchorPriceB, int digits = 5, double targetProfit = 0)
		{
			double netTargetPrice = 0;
			double interval = Math.Round(anchorPriceA.CalculateIntervalAgainst(anchorPriceB), digits);
			double anchorPriceMid = anchorPriceA.FindCenterAgainst(anchorPriceB, digits);

			while (netTargetPrice.IsZero())
			{
				double anchorAProfit = Math.Round(inputPositions.Select(x => x.ProjectedNetProfit(anchorPriceA)).Sum(), 2);
				double anchorBProfit = Math.Round(inputPositions.Select(x => x.ProjectedNetProfit(anchorPriceB)).Sum(), 2);
				double anchorMidProfit = Math.Round(inputPositions.Select(x => x.ProjectedNetProfit(anchorPriceMid)).Sum(), 2);

				if (anchorAProfit == targetProfit) netTargetPrice = anchorPriceA;
				if (anchorBProfit == targetProfit) netTargetPrice = anchorPriceB;
				if (anchorMidProfit == targetProfit) netTargetPrice = anchorPriceMid;
				if (anchorPriceA == anchorPriceB || anchorPriceA == anchorPriceMid || anchorPriceB == anchorPriceMid)
				{
					//if some prices are the same, then return the nearest positive to target price
					var list = new[]
					{
						new { Price = anchorPriceA, Profit = anchorAProfit },
						new { Price = anchorPriceB, Profit = anchorBProfit },
						new { Price = anchorPriceMid, Profit = anchorMidProfit }
					}.ToList();
					netTargetPrice = list.Where(x => x.Profit >= targetProfit).OrderBy(x => x.Profit).First().Price;
				}

				if (netTargetPrice.IsZero())
				{
					if ((anchorAProfit > targetProfit && anchorMidProfit < targetProfit && anchorBProfit < targetProfit) || (anchorAProfit < targetProfit && anchorMidProfit > targetProfit && anchorBProfit > targetProfit))
					{
						anchorPriceB = anchorPriceMid;
						anchorPriceMid = anchorPriceA.FindCenterAgainst(anchorPriceB, digits);
						interval = Math.Round(anchorPriceA.CalculateIntervalAgainst(anchorPriceB), digits);
					}
					else if ((anchorAProfit > targetProfit && anchorMidProfit > targetProfit && anchorBProfit < targetProfit) || (anchorAProfit < targetProfit && anchorMidProfit < targetProfit && anchorBProfit > targetProfit))
					{
						anchorPriceA = anchorPriceMid;
						anchorPriceMid = anchorPriceA.FindCenterAgainst(anchorPriceB, digits);
						interval = Math.Round(anchorPriceA.CalculateIntervalAgainst(anchorPriceB), digits);
					}
					else if ((anchorAProfit > targetProfit && anchorMidProfit > targetProfit && anchorBProfit > targetProfit) || (anchorAProfit < targetProfit && anchorMidProfit < targetProfit && anchorBProfit < targetProfit))
					{
						//make it wider
						anchorPriceA = (anchorPriceA > anchorPriceB ? anchorPriceA += interval : anchorPriceA -= interval);
						anchorPriceB = (anchorPriceA > anchorPriceB ? anchorPriceB -= interval : anchorPriceB += interval);
						anchorPriceMid = anchorPriceA.FindCenterAgainst(anchorPriceB, digits);
						interval = Math.Round(anchorPriceA.CalculateIntervalAgainst(anchorPriceB), digits);
					}
				}
			}

			return netTargetPrice;
		}

	}
}
