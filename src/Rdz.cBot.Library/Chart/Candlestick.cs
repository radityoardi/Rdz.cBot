using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rdz.cBot.Library;

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
			return string.Format("[{10:ddMMMyyyy-HHmmss}] High: {0}, Open: {1}, Close: {2}, Low: {3}, LowerShadow: {4}, UpperShadow: {5}, RealBody: {6}, LowerShadow%: {7}, RealBody%: {8}, Height: {9}", this.High, this.Open, this.Close, this.Low, this.LowerShadowHeight, this.UpperShadowHeight, this.RealBodyHeight, this.LowerShadowPercentage, this.RealBodyPercentage, this.Height, this.OpenTime);
		}
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

		public double Height
		{
			get
			{
				return Math.Round(this.High - this.Low, Digits);
			}
		}

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

		public double RealBodyHeight
		{
			get
			{
				return Math.Round(this.RealBodyHigh - this.RealBodyLow, Digits);
			}
		}

		public double UpperShadowHeight
		{
			get
			{
				return Math.Round(this.High - this.RealBodyHigh, Digits);
			}
		}

		public double HighToCloseHeight
		{
			get
			{
				return Math.Round(this.High - this.Close, Digits);
			}
		}

		public double CloseToLowHeight
		{
			get
			{
				return Math.Round(this.Close - this.Low, Digits);
			}
		}

		public bool HasUpperShadow
		{
			get
			{
				return this.UpperShadowHeight > 0;
			}
		}

		public double LowerShadowHeight
		{
			get
			{
				return Math.Round(this.RealBodyLow - this.Low, Digits);
			}
		}

		public bool HasLowerShadow
		{
			get
			{
				return this.LowerShadowHeight > 0;
			}
		}

		public double RealBodyPercentage
		{
			get
			{
				return Math.Round(this.RealBodyHeight / this.Height, Digits);
			}
		}

		public double UpperShadowPercentage
		{
			get
			{
				return Math.Round(this.UpperShadowHeight / this.Height, Digits);
			}
		}

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
