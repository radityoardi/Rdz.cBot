using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rdz.cBot.Library;
using Rdz.cBot.Library.Extensions;

namespace Rdz.cBot.Library.Chart
{
	public class Candlestick
	{
		public double High { get; set; }
		public double Open { get; set; }
		public double Close { get; set; }
		public double Low { get; set; }
		public int Digits { get; set; }
		public DateTime OpenTime { get; set; }
		public Candlestick(double High, double Open, double Close, double Low, int Digits, DateTime OpenTime)
		{
			this.High = Math.Round(High, Digits);
			this.Open = Math.Round(Open, Digits);
			this.Close = Math.Round(Close, Digits);
			this.Low = Math.Round(Low, Digits);
			this.OpenTime = OpenTime;
			this.Digits = Digits;
		}

		public override string ToString()
		{
			return this.ToString(false);
		}
		public string ToString(int digit)
		{
			return this.ToString(false, digit);
		}
		public string ToString(bool IsUTC, int digit = -1)
		{
			string sDigit = digit.DigitFormat();
			return string.Format(string.Format("[{{10:ddMMMyyyy-HH:mm:ss}}] High: {{0:{0}}} | Open: {{1:{0}}} | Close: {{2:{0}}} | Low: {{3:{0}}} | LowerShadow: {{4:{0}}} | UpperShadow: {{5:{0}}} | RealBody: {{6:{0}}} | LowerShadow%: {{7:0.00}} | RealBody%: {{8:0.00}} | Height: {{9:{0}}}", sDigit),
				this.High, this.Open, this.Close, this.Low, this.LowerShadowHeight, this.UpperShadowHeight, this.RealBodyHeight, this.LowerShadowPercentage, this.RealBodyPercentage, this.Height, IsUTC ? this.OpenTime : this.OpenTime.ToLocalTime());
		}
		/// <summary>
		/// The direction of the candlestick, whether bullish or bearish.
		/// </summary>
		public enDirection Direction
		{
			get
			{
				if (this.Close > this.Open)
					return enDirection.Bullish;
				else if (this.Open > this.Close)
					return enDirection.Bearish;
				else
					return enDirection.Neutral;
			}
		}

		/// <summary>
		/// The height between High to Low.
		/// </summary>
		public double Height
		{
			get
			{
				return Math.Round(this.High - this.Low, Digits);
			}
		}

		/// <summary>
		/// The "High" of the candlestick body (between Open and Close).
		/// </summary>
		public double RealBodyHigh
		{
			get
			{
				if (Direction == enDirection.Bullish)
					return this.Close;
				else
					return this.Open;
			}
		}

		/// <summary>
		/// The "Low" of the candlestick body (between Open and Close).
		/// </summary>
		public double RealBodyLow
		{
			get
			{
				if (Direction == enDirection.Bullish)
					return this.Open;
				else
					return this.Close;
			}
		}
		/// <summary>
		/// The height between Open and Close.
		/// </summary>
		public double RealBodyHeight
		{
			get
			{
				return Math.Round(this.RealBodyHigh - this.RealBodyLow, Digits);
			}
		}
		/// <summary>
		/// The height between High and RealBodyHigh.
		/// </summary>
		public double UpperShadowHeight
		{
			get
			{
				return Math.Round(this.High - this.RealBodyHigh, Digits);
			}
		}

		/// <summary>
		/// The height between High to Close.
		/// </summary>
		public double HighToCloseHeight
		{
			get
			{
				return Math.Round(this.High - this.Close, Digits);
			}
		}
		/// <summary>
		/// The height between Close to Low.
		/// </summary>
		public double CloseToLowHeight
		{
			get
			{
				return Math.Round(this.Close - this.Low, Digits);
			}
		}
		/// <summary>
		/// Check whether it has an Upper Shadow (between High and RealBodyHigh).
		/// </summary>
		public bool HasUpperShadow
		{
			get
			{
				return this.UpperShadowHeight > 0;
			}
		}
		/// <summary>
		/// The height between RealBodyLow and Low.
		/// </summary>
		public double LowerShadowHeight
		{
			get
			{
				return Math.Round(this.RealBodyLow - this.Low, Digits);
			}
		}

		/// <summary>
		/// Check whether it has an Lower Shadow (between RealBodyLow and Close).
		/// </summary>
		public bool HasLowerShadow
		{
			get
			{
				return this.LowerShadowHeight > 0;
			}
		}

		/// <summary>
		/// The percentage of RealBodyHeight against Height.
		/// </summary>
		public double RealBodyPercentage
		{
			get
			{
				return Math.Round(this.RealBodyHeight / this.Height, Digits);
			}
		}

		/// <summary>
		/// The percentage of Upper Shadow against Height.
		/// </summary>
		public double UpperShadowPercentage
		{
			get
			{
				return Math.Round(this.UpperShadowHeight / this.Height, Digits);
			}
		}
		/// <summary>
		/// The percentage of Lower Shadow against Height.
		/// </summary>
		public double LowerShadowPercentage
		{
			get
			{
				return Math.Round(this.LowerShadowHeight / this.Height, Digits);
			}
		}

		public bool IsDragonflyDoji
		{
			get
			{
				return (this.RealBodyHigh == this.RealBodyLow && this.RealBodyHigh == this.High && this.RealBodyLow > this.Low);
			}
		}

		public bool IsGravestoneDoji
		{
			get
			{
				return (this.RealBodyHigh == this.RealBodyLow && this.RealBodyLow == this.Low && this.High > this.RealBodyHigh);
			}
		}

		public bool IsDojiStar
		{
			get
			{
				return (this.RealBodyHigh == this.RealBodyLow && this.High > this.RealBodyHigh && this.RealBodyLow > this.Low);
			}
		}

		/// <summary>
		/// To compare between 2 Candlesticks whether it's same.
		/// </summary>
		/// <param name="comparer"></param>
		/// <returns></returns>
		public bool Same(Candlestick comparer)
		{
			return (comparer != null && this.High == comparer.High && this.Low == comparer.Low && this.Open == comparer.Open && this.Close == comparer.Close && this.OpenTime == comparer.OpenTime);
		}

		public bool IsAbove(double price)
		{
			return IsAbove(price, enCandlestickPart.All);
		}
		public bool IsAbove(double price, enCandlestickPart part)
		{
			if (part == enCandlestickPart.All || part == enCandlestickPart.Low || part == enCandlestickPart.LowerShadow)
			{
				return Low > price;
			}
			else if (part == enCandlestickPart.High)
			{
				return High > price;
			}
			else if (part == enCandlestickPart.RealBodyLow || part == enCandlestickPart.RealBody)
			{
				return RealBodyLow > price;
			}
			else if (part == enCandlestickPart.RealBodyHigh || part == enCandlestickPart.UpperShadow)
			{
				return RealBodyHigh > price;
			}
			return false;
		}
		public bool IsAbove(Candlestick candlestick)
		{
			return IsAbove(candlestick, enCandlestickPart.All);
		}
		public bool IsAbove(Candlestick candlestick, enCandlestickPart part)
		{
			if (part == enCandlestickPart.All)
			{
				return this.IsAbove(candlestick, enCandlestickPart.High) && this.IsAbove(candlestick, enCandlestickPart.Low) && this.IsAbove(candlestick, enCandlestickPart.RealBodyHigh) && this.IsAbove(candlestick, enCandlestickPart.RealBodyLow);
			}
			else if (part == enCandlestickPart.High)
			{
				return this.High > candlestick.High;
			}
			else if (part == enCandlestickPart.Low)
			{
				return this.Low > candlestick.Low;
			}
			else if (part == enCandlestickPart.RealBodyHigh)
			{
				return this.RealBodyHigh > candlestick.RealBodyHigh;
			}
			else if (part == enCandlestickPart.RealBodyLow)
			{
				return this.RealBodyLow > candlestick.RealBodyLow;
			}
			return false;
		}
		public bool IsBelow(double price)
		{
			return IsBelow(price, enCandlestickPart.All);
		}
		public bool IsBelow(double price, enCandlestickPart part)
		{
			if (part == enCandlestickPart.All || part == enCandlestickPart.High || part == enCandlestickPart.UpperShadow)
			{
				return High < price;
			}
			else if (part == enCandlestickPart.Low)
			{
				return Low < price;
			}
			else if (part == enCandlestickPart.RealBodyLow || part == enCandlestickPart.LowerShadow)
			{
				return RealBodyLow < price;
			}
			else if (part == enCandlestickPart.RealBodyHigh || part == enCandlestickPart.RealBody)
			{
				return RealBodyHigh < price;
			}
			return false;
		}
		public bool IsBelow(Candlestick candlestick)
		{
			return this.IsBelow(candlestick, enCandlestickPart.All);
		}
		public bool IsBelow(Candlestick candlestick, enCandlestickPart part)
		{
			if (part == enCandlestickPart.All)
			{
				return this.IsBelow(candlestick, enCandlestickPart.High) && this.IsBelow(candlestick, enCandlestickPart.Low) && this.IsBelow(candlestick, enCandlestickPart.RealBodyHigh) && this.IsBelow(candlestick, enCandlestickPart.RealBodyLow);
			}
			else if (part == enCandlestickPart.High)
			{
				return this.High < candlestick.High;
			}
			else if (part == enCandlestickPart.Low)
			{
				return this.Low < candlestick.Low;
			}
			else if (part == enCandlestickPart.RealBodyHigh)
			{
				return this.RealBodyHigh < candlestick.RealBodyHigh;
			}
			else if (part == enCandlestickPart.RealBodyLow)
			{
				return this.RealBodyLow < candlestick.RealBodyLow;
			}
			return false;
		}

		public bool IsBetween(double price1, double price2)
		{
			return IsBetween(price1, price2, enCandlestickPart.All);
		}
		public bool IsBetween(double price1, double price2, enCandlestickPart part)
		{
			double UpperPrice = price1 > price2 ? price1 : price2;
			double LowerPrice = price2 < price1 ? price2 : price1;
			bool IsUpperLowerSame = UpperPrice == LowerPrice;

			if (part == enCandlestickPart.All)
			{
				return High < UpperPrice && Low > LowerPrice;
			}
			else if (part == enCandlestickPart.High)
			{
				return High < UpperPrice && High > LowerPrice;
			}
			else if (part == enCandlestickPart.Low)
			{
				return Low < UpperPrice && Low > LowerPrice;
			}
			else if (part == enCandlestickPart.LowerShadow)
			{
				return RealBodyLow < UpperPrice && Low > LowerPrice;
			}
			else if (part == enCandlestickPart.UpperShadow)
			{
				return High < UpperPrice && RealBodyHigh > LowerPrice;
			}
			else if (part == enCandlestickPart.RealBody)
			{
				return RealBodyHigh < UpperPrice && RealBodyLow > LowerPrice;
			}
			else if (part == enCandlestickPart.RealBodyHigh)
			{
				return RealBodyHigh < UpperPrice && RealBodyHigh > LowerPrice;
			}
			else if (part == enCandlestickPart.RealBodyLow)
			{
				return RealBodyLow < UpperPrice && RealBodyLow > LowerPrice;
			}
			return false;
		}
	}
}
