using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using Rdz.cTrader.Library;
using System.Globalization;

namespace Rdz.cTrader.Library
{
    public static class CommonLibrary
    {
		#region Distance
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

		#region "ShiftPrice & ShiftPriceInPips"
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
            }
            else
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
        public static double CalculateIntervalAgainst(this double price1, double price2)
        {
            double gap = price1 - price2;
            return Math.Abs(gap);
        }

    }
}