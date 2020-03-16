using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;


namespace Rdz.cBot.Library.Indicators
{
	public class BollingerBandsDistance
	{
		private BollingerBands bb { get; set; }

		public enCodeType CodeType { get; private set; }
		private Indicator indi { get; set; }
		private Robot robot { get; set; }
		public BollingerBandsDistance(Indicator indi, DataSeries Source, MovingAverageType MaType, int Periods, int StandardDeviations)
		{
			bb = indi.Indicators.BollingerBands(Source, Periods, StandardDeviations, MaType);
			this.CodeType = enCodeType.Indicator;
			this.indi = indi;
		}
		public BollingerBandsDistance(Robot robot, DataSeries Source, MovingAverageType MaType, int Periods, int StandardDeviations)
		{
			bb = robot.Indicators.BollingerBands(Source, Periods, StandardDeviations, MaType);
			this.CodeType = enCodeType.Robot;
			this.robot = robot;
		}

		public double Calculate(int index)
		{
			double distance = double.NaN;
			double dx = double.NaN;
			if (bb.Top[index] != double.NaN && bb.Bottom[index] != double.NaN)
			{
				distance = bb.Top[index] - bb.Bottom[index];
				if (this.CodeType == enCodeType.Indicator)
				{
					dx = distance * Math.Pow(10, indi.Symbol.Digits);
				}
				else if (this.CodeType == enCodeType.Robot)
				{
					dx = distance * Math.Pow(10, robot.Symbol.Digits);
				}
			}
			return dx;
		}
	}
}
