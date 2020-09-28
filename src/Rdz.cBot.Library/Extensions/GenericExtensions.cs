using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
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
		const int LastBar = 0;

		#region GetMarketSeries functions
		#region Common Reference
		public static Candlestick GetMarketSeries(this Bars bars, Symbol symbol, int index = LastBar, bool IsRobot = true)
		{
			if (index >= 0)
			{
				if (IsRobot)
				{
					return new Candlestick(bars.HighPrices.Last(index), bars.OpenPrices.Last(index), bars.ClosePrices.Last(index), bars.LowPrices.Last(index), symbol.Digits, bars.OpenTimes.Last(index));
				}
				else
					return new Candlestick(bars.HighPrices[index], bars.OpenPrices[index], bars.ClosePrices[index], bars.LowPrices[index], symbol.Digits, bars.OpenTimes[index]);

			}
			else
			{
				if (IsRobot)
				{
					return new Candlestick(bars.HighPrices.LastValue, bars.OpenPrices.LastValue, bars.ClosePrices.LastValue, bars.LowPrices.LastValue, symbol.Digits, bars.OpenTimes.LastValue);
				}
				else
					return new Candlestick(bars.HighPrices.First(), bars.OpenPrices.First(), bars.ClosePrices.First(), bars.LowPrices.First(), symbol.Digits, bars.OpenTimes.First());
			}
		}

		public static List<Candlestick> GetMarketSeries(this Bars bars, Symbol symbol, int[] indices, bool IsRobot = true)
		{
			List<Candlestick> cdlret = new List<Candlestick>();
			if (indices != null && indices.Length > 0)
			{
				for (int i = 0; i < indices.Length; i++)
				{
					if (IsRobot)
					{
						cdlret.Add(new Candlestick(bars.HighPrices.Last(indices[i]), bars.OpenPrices.Last(indices[i]), bars.ClosePrices.Last(indices[i]), bars.LowPrices.Last(indices[i]), symbol.Digits, bars.OpenTimes.Last(indices[i])));
					}
					else
						cdlret.Add(new Candlestick(bars.HighPrices[indices[i]], bars.OpenPrices[indices[i]], bars.ClosePrices[indices[i]], bars.LowPrices[indices[i]], symbol.Digits, bars.OpenTimes[indices[i]]));
				}
			}
			return cdlret;
		}
		public static List<Candlestick> GetMarketSeries(this Bars bars, Symbol symbol, int fromIndex, int toIndex, bool IsRobot = true)
		{
			List<Candlestick> cdlret = new List<Candlestick>();
			if (fromIndex < toIndex)
			{
				for (int i = fromIndex; i <= toIndex; i++)
				{
					if (IsRobot)
					{
						cdlret.Add(new Candlestick(bars.HighPrices.Last(i), bars.OpenPrices.Last(i), bars.ClosePrices.Last(i), bars.LowPrices.Last(i), symbol.Digits, bars.OpenTimes.Last(i)));
					}
					else
						cdlret.Add(new Candlestick(bars.HighPrices[i], bars.OpenPrices[i], bars.ClosePrices[i], bars.LowPrices[i], symbol.Digits, bars.OpenTimes[i]));
				}
			}
			else if (toIndex < fromIndex)
			{
				for (int i = fromIndex; i >= toIndex; i--)
				{
					if (IsRobot)
					{
						cdlret.Add(new Candlestick(bars.HighPrices.Last(i), bars.OpenPrices.Last(i), bars.ClosePrices.Last(i), bars.LowPrices.Last(i), symbol.Digits, bars.OpenTimes.Last(i)));
					}
					else
						cdlret.Add(new Candlestick(bars.HighPrices[i], bars.OpenPrices[i], bars.ClosePrices[i], bars.LowPrices[i], symbol.Digits, bars.OpenTimes[i]));
				}
			}
			else
				throw new InvalidOperationException("fromIndex and toIndex must be a different number.");
			return cdlret;
		}
		#endregion

		public static List<Candlestick> GetMarketSeries(this Robot robot, List<int> indices)
		{
			return GetMarketSeries(robot, indices.ToArray());
		}
		public static List<Candlestick> GetMarketSeries(this Robot robot, int[] indices)
		{
			return GetMarketSeries(robot.Bars, robot.Symbol, indices);
		}
		public static List<Candlestick> GetMarketSeries(this Robot robot, int fromIndex, int toIndex)
		{
			return GetMarketSeries(robot.Bars, robot.Symbol, fromIndex, toIndex);
		}
		public static Candlestick GetLastClosedMarketSeries(this Robot robot)
		{
			return robot.GetMarketSeries(1);
		}
		public static Candlestick GetMarketSeries(this Robot robot, int index = LastBar)
		{
			return GetMarketSeries(robot.Bars, robot.Symbol, index);
			/*

			if (index > -1)
			{
				//return new Candlestick(robot.MarketSeries.High.Last(index), robot.MarketSeries.Open.Last(index), robot.MarketSeries.Close.Last(index), robot.MarketSeries.Low.Last(index), robot.Symbol.Digits, robot.MarketSeries.OpenTime.Last(index));
				//return new Candlestick(robot.Bars.HighPrices[index], robot.Bars.OpenPrices[index], robot.Bars.ClosePrices[index], robot.Bars.LowPrices[index], robot.Symbol.Digits, robot.Bars.OpenTimes[index]);
				return new Candlestick(robot.Bars.HighPrices.Last(index), robot.Bars.OpenPrices.Last(index), robot.Bars.ClosePrices.Last(index), robot.Bars.LowPrices.Last(index), robot.Symbol.Digits, robot.Bars.OpenTimes.Last(index));
			}
			else
			{
				//return new Candlestick(robot.MarketSeries.High.LastValue, robot.MarketSeries.Open.LastValue, robot.MarketSeries.Close.LastValue, robot.MarketSeries.Low.LastValue, robot.Symbol.Digits, robot.MarketSeries.OpenTime.LastValue);
				return new Candlestick(robot.Bars.HighPrices.LastValue, robot.Bars.OpenPrices.LastValue, robot.Bars.ClosePrices.LastValue, robot.Bars.LowPrices.LastValue, robot.Symbol.Digits, robot.Bars.OpenTimes.LastValue);
			}
			*/
		}
		public static List<Candlestick> GetMarketSeries(this Indicator indi, List<int> indices)
		{
			return GetMarketSeries(indi, indices.ToArray());
		}
		public static List<Candlestick> GetMarketSeries(this Indicator indi, int[] indices)
		{
			return GetMarketSeries(indi.Bars, indi.Symbol, indices, false);
			/*

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
			*/
		}
		public static List<Candlestick> GetMarketSeries(this Indicator indi, int fromIndex, int toIndex)
		{
			return GetMarketSeries(indi.Bars, indi.Symbol, fromIndex, toIndex, false);
			/*

			List<Candlestick> cdlret = new List<Candlestick>();
			if (fromIndex < toIndex)
			{
				for (int i = fromIndex; i <= toIndex; i++)
				{
					cdlret.Add(new Candlestick(indi.Bars.HighPrices[i], indi.Bars.OpenPrices[i], indi.Bars.ClosePrices[i], indi.Bars.LowPrices[i], indi.Symbol.Digits, indi.Bars.OpenTimes[i]));
				}
			}
			else if (toIndex < fromIndex)
			{
				for (int i = fromIndex; i >= toIndex; i--)
				{
					cdlret.Add(new Candlestick(indi.Bars.HighPrices[i], indi.Bars.OpenPrices[i], indi.Bars.ClosePrices[i], indi.Bars.LowPrices[i], indi.Symbol.Digits, indi.Bars.OpenTimes[i]));
				}
			}
			else
				throw new InvalidOperationException("fromIndex and toIndex must be a different number.");
			return cdlret;
			*/
		}
		public static Candlestick GetMarketSeries(this Indicator indi, int index = LastBar)
		{
			return GetMarketSeries(indi.Bars, indi.Symbol, index, false);
			/*
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
			*/
		}
		#endregion


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

		[Obsolete("Please use DistanceInPips")]
		public static double CalculatePips(this Robot robot, double price1, double price2)
		{
			return Math.Abs((price1 - price2) / robot.Symbol.PipSize);
		}

		#region Distance & DistanceInPips
		public static int Distance(this Indicator indi, double highPrice, double lowPrice, bool AlwaysPositive = false)
		{
			return indi.Symbol.Distance(highPrice, lowPrice, AlwaysPositive);
		}
		public static int Distance(this Robot robot, double highPrice, double lowPrice, bool AlwaysPositive = false)
		{
			return robot.Symbol.Distance(highPrice, lowPrice, AlwaysPositive);
		}
		public static int Distance(this Symbol symbol, double highPrice, double lowPrice, bool AlwaysPositive = false)
		{
			if (AlwaysPositive)
			{
				return Convert.ToInt32(Math.Abs((highPrice - lowPrice) / symbol.TickSize));
			}
			else
			{
				return Convert.ToInt32((highPrice - lowPrice) / symbol.TickSize);
			}
		}
		public static int DistanceInPips(this Indicator indi, double highPrice, double lowPrice, bool AlwaysPositive = false)
		{
			return indi.Symbol.DistanceInPips(highPrice, lowPrice, AlwaysPositive);
		}
		public static int DistanceInPips(this Robot robot, double highPrice, double lowPrice, bool AlwaysPositive = false)
		{
			return robot.Symbol.DistanceInPips(highPrice, lowPrice, AlwaysPositive);
		}
		public static int DistanceInPips(this Symbol symbol, double highPrice, double lowPrice, bool AlwaysPositive = false)
		{
			if (AlwaysPositive)
			{
				return Convert.ToInt32(Math.Abs((highPrice - lowPrice) / symbol.PipSize));
			}
			else
			{
				return Convert.ToInt32((highPrice - lowPrice) / symbol.PipSize);
			}
		}
		#endregion

		#region ShiftPrice & ShiftPriceInPips
		public static double ShiftPrice(this Robot robot, double fromPrice, int points)
		{
			return robot.Symbol.ShiftPrice(fromPrice, points);
		}
		public static double ShiftPrice(this Indicator indi, double fromPrice, int points)
		{
			return indi.Symbol.ShiftPrice(fromPrice, points);
		}
		public static double ShiftPrice(this Symbol symbol, double fromPrice, int points)
		{
			var priceDiff = symbol.TickSize * points;
			return fromPrice + priceDiff;
		}
		public static double ShiftPriceInPips(this Robot robot, double fromPrice, int pips)
		{
			return robot.Symbol.ShiftPriceInPips(fromPrice, pips);
		}
		public static double ShiftPriceInPips(this Indicator indi, double fromPrice, int pips)
		{
			return indi.Symbol.ShiftPriceInPips(fromPrice, pips);
		}
		public static double ShiftPriceInPips(this Symbol symbol, double fromPrice, int pips)
		{
			var priceDiff = symbol.PipSize * pips;
			return fromPrice + priceDiff;
		}
		#endregion

		#region ConvertToPips & ConvertToTick
		public static int ConvertToPips(this Indicator indi, double input)
		{
			return indi.Symbol.ConvertToPips(input);
		}
		public static int ConvertToPips(this Symbol symbol, double input)
		{
			return Convert.ToInt32(Math.Abs(input / symbol.PipSize));
		}
		public static int ConvertToTick(this Robot robot, double input)
		{
			return robot.Symbol.ConvertToTick(input);
		}
		public static int ConvertToTick(this Symbol symbol, double input)
		{
			return Convert.ToInt32(Math.Abs(input / symbol.TickSize));
		}
		#endregion

		public static double Undigit(this double value, int digit)
		{
			return value * Math.Pow(10, digit);
		}

		public static int StepUp(this int value, int step = 1)
		{
			return value + step;
		}
		public static int StepDown(this int value, int step = 1)
		{
			return value - step;
		}

		public static int Reset(this int value, int reset = 0)
		{
			return reset;
		}

		public static TradeType Reverse(this TradeType value)
		{
			return value == TradeType.Buy ? TradeType.Sell : TradeType.Buy;
		}

		public static double LotToVolume(this Robot robot, double LotSize)
		{
			return robot.Symbol.NormalizeVolumeInUnits(robot.Symbol.QuantityToVolumeInUnits(LotSize));
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

		public static string DigitFormat(this int digit)
		{
			string sDigit = string.Empty;
			if (digit > -1 && digit == 0)
			{
				sDigit = "0";
			}
			else if (digit > 0)
			{
				sDigit = string.Concat("0.", new String('0', digit));
			}
			return sDigit;
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
		public static bool IsAbove(this double input, double comparer)
		{
			return input > comparer;
		}
		public static bool IsAboveOrEqual(this double input, double comparer)
		{
			return input >= comparer;
		}
		public static bool IsBelow(this double input, double comparer)
		{
			return input < comparer;
		}
		public static bool IsBelowOrEqual(this double input, double comparer)
		{
			return input <= comparer;
		}
		public static bool IsBetween(this double input, double comparerA, double comparerB)
		{
			if (comparerA >= comparerB)
			{
				return input.IsBelowOrEqual(comparerA) && input.IsAboveOrEqual(comparerB);
			}
			else if (comparerA <= comparerB)
			{
				return input.IsBelowOrEqual(comparerB) && input.IsAboveOrEqual(comparerA);
			}
			else
				return input == comparerA && input == comparerB;
		}
		public static bool IsAbove(this int input, int comparer)
		{
			return input > comparer;
		}
		public static bool IsAboveOrEqual(this int input, int comparer)
		{
			return input >= comparer;
		}
		public static bool IsBelow(this int input, int comparer)
		{
			return input < comparer;
		}
		public static bool IsBelowOrEqual(this int input, int comparer)
		{
			return input <= comparer;
		}
		public static bool IsBetween(this int input, int comparerA, int comparerB)
		{
			if (comparerA >= comparerB)
			{
				return input.IsBelowOrEqual(comparerA) && input.IsAboveOrEqual(comparerB);
			}
			else if (comparerA <= comparerB)
			{
				return input.IsBelowOrEqual(comparerB) && input.IsAboveOrEqual(comparerA);
			}
			else
				return input == comparerA && input == comparerB;
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
