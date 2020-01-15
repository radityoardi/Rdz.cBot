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
		public static Candlestick GetMarketSeries(this Robot robot, int index = -1)
		{
			if (index > -1)
			{
				return new Candlestick(robot.MarketSeries.High.Last(index), robot.MarketSeries.Open.Last(index), robot.MarketSeries.Close.Last(index), robot.MarketSeries.Low.Last(index), robot.Symbol.Digits, robot.MarketSeries.OpenTime.Last(index));
			} else {
				return new Candlestick(robot.MarketSeries.High.LastValue, robot.MarketSeries.Open.LastValue, robot.MarketSeries.Close.LastValue, robot.MarketSeries.Low.LastValue, robot.Symbol.Digits, robot.MarketSeries.OpenTime.LastValue);
			}
		}
		public static int CalculateInterval(this Robot robot, double price1, double price2)
		{
			int gap = (int)((price1 - price2) / robot.Symbol.TickSize);
			return (gap < 0 ? gap * -1 : gap); //returns to always positive number
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
