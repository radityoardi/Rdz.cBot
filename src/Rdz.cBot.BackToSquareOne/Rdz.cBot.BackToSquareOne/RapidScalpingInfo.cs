using Rdz.cBot.Library;
using Rdz.cBot.Library.Chart;
using Rdz.cBot.Library.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rdz.cBot.BackToSquareOne
{
	internal class RapidScalpingInfo
	{
		internal Candlestick TriggerBar { get; set; }

		internal double NegativeLine
		{
			get
			{
				double r = double.NaN;
				switch (Direction)
				{
					case enDirection.Bullish:
						r = this.robot.ShiftPrice(TriggerBar.Low, -TolerancePoints);
						break;
					case enDirection.Bearish:
						r = this.robot.ShiftPrice(TriggerBar.High, TolerancePoints);
						break;
				}
				return r;
			}
		}
		internal double TriggerLine
		{
			get
			{
				double r = double.NaN;
				switch (Direction)
				{
					case enDirection.Bullish:
						double Highestof5 = double.NaN;
						for (int i = 0; i < 5; i++)
						{
							if (i == 0 || this.Last5Candlesticks[i].High > Highestof5) Highestof5 = this.Last5Candlesticks[i].High;
						}
						r = this.robot.ShiftPrice(TriggerBar.High, TolerancePoints);
						break;
					case enDirection.Bearish:
						double Lowestof5 = double.NaN;
						for (int i = 0; i < 5; i++)
						{
							if (i == 0 || this.Last5Candlesticks[i].Low < Lowestof5) Lowestof5 = this.Last5Candlesticks[i].Low;
						}
						r = this.robot.ShiftPrice(Lowestof5, -TolerancePoints);
						break;
				}
				return r;
			}
		}

		internal double TakeProfit0
		{
			get
			{
				double r = double.NaN;
				switch (Direction)
				{
					case enDirection.Bullish:
						r = TriggerLine + (Risk / 2);
						break;
					case enDirection.Bearish:
						r = TriggerLine - (Risk / 2);
						break;
				}
				return r;
			}
		}

		internal double Risk
		{
			get
			{
				double r = double.NaN;
				switch (Direction)
				{
					case enDirection.Bullish:
						r = (TriggerLine - NegativeLine) * RiskMultiplier;
						break;
					case enDirection.Bearish:
						r = (NegativeLine - TriggerLine) * RiskMultiplier;
						break;
				}
				return r;
			}
		}
		internal double TakeProfit1
		{
			get
			{
				double r = double.NaN;
				switch (Direction)
				{
					case enDirection.Bullish:
						r = TriggerLine + Risk;
						break;
					case enDirection.Bearish:
						r = TriggerLine - Risk;
						break;
				}
				return r;
			}
		}
		internal double TakeProfit2
		{
			get
			{
				double r = double.NaN;
				switch (Direction)
				{
					case enDirection.Bullish:
						r = TakeProfit1 + (Risk * 2);
						break;
					case enDirection.Bearish:
						r = TakeProfit1 + (-Risk * 2);
						break;
				}
				return r;
			}
		}
		internal enDirection Direction { get; private set; }

		internal bool IsDefined { get; private set; }
		internal int TolerancePoints { get; private set; }
		internal RdzRobot robot { get; private set; }
		internal List<Candlestick> Last5Candlesticks { get; private set; }
		internal int RiskMultiplier { get; private set; }

		internal RapidScalpingInfo(RdzRobot robot, Candlestick TriggerBar, enDirection Direction, int TolerancePoints = 3, int RiskMultiplier = 1)
		{
			this.TriggerBar = TriggerBar;
			this.Direction = Direction;
			this.IsDefined = false;
			this.TolerancePoints = TolerancePoints;
			this.robot = robot;
			this.RiskMultiplier = RiskMultiplier;
			Last5Candlesticks = new List<Candlestick>();
			for (int i = 0; i < 5; i++)
			{
				Last5Candlesticks.Add(this.robot.GetMarketSeries(i + 1));
			}
		}
		public override string ToString()
		{
			return string.Format("Negative: {0}, Trigger: {1}, Risk: {2}, TakeProfit1: {3}, TakeProfit: {4}", this.NegativeLine, this.TriggerLine, this.Risk, this.TakeProfit1, this.TakeProfit2);
		}
	}
}
