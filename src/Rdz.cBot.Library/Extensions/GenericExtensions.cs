using System;
using System.Collections.Generic;
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
				return new Candlestick(robot.MarketSeries.High.Last(index), robot.MarketSeries.Open.Last(index), robot.MarketSeries.Close.Last(index), robot.MarketSeries.Low.Last(index), robot.Symbol.Digits, robot.MarketSeries.OpenTime.Last(index));
			} else {
				return new Candlestick(robot.MarketSeries.High.LastValue, robot.MarketSeries.Open.LastValue, robot.MarketSeries.Close.LastValue, robot.MarketSeries.Low.LastValue, robot.Symbol.Digits, robot.MarketSeries.OpenTime.LastValue);
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
					cdlret.Add(new Candlestick(indi.MarketSeries.High.Last(indices[i]), indi.MarketSeries.Open.Last(indices[i]), indi.MarketSeries.Close.Last(indices[i]), indi.MarketSeries.Low.Last(indices[i]), indi.Symbol.Digits, indi.MarketSeries.OpenTime.Last(indices[i])));
				}
			}
			return cdlret;
		}
		public static Candlestick GetMarketSeries(this Indicator indi, int index = -1)
		{
			if (index > -1)
			{
				return new Candlestick(indi.MarketSeries.High.Last(index), indi.MarketSeries.Open.Last(index), indi.MarketSeries.Close.Last(index), indi.MarketSeries.Low.Last(index), indi.Symbol.Digits, indi.MarketSeries.OpenTime.Last(index));
			}
			else
			{
				return new Candlestick(indi.MarketSeries.High.LastValue, indi.MarketSeries.Open.LastValue, indi.MarketSeries.Close.LastValue, indi.MarketSeries.Low.LastValue, indi.Symbol.Digits, indi.MarketSeries.OpenTime.LastValue);
			}
		}
		public static int CalculateInterval(this Robot robot, double price1, double price2)
		{
			int gap = (int)((price1 - price2) / robot.Symbol.TickSize);
			return (gap < 0 ? gap * -1 : gap); //returns to always positive number
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
	}
}
