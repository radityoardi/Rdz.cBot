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
	}
}
