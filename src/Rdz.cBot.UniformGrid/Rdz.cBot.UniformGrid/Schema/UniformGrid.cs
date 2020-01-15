using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using Rdz.cBot.Library.Extensions;


namespace Rdz.cBot.UniformGrid.Schema
{
	internal class UniformGrid
	{
		internal UniformGridBot robot { get; set; }
		internal List<double> GridPrices { get; set; }

		internal UniformGrid(UniformGridBot robot)
		{
			this.GridPrices = new List<double>();
			this.robot = robot;
			Initialize();
		}

		internal void Initialize()
		{
			robot.ExecuteMarketOrderAsync(robot.GridTradeType, robot.Symbol.Name, robot.LotToVolume(robot.LotSize), "UG" + (robot.Positions.Count + 1).ToString(), result =>
			{
				if (result.IsSuccessful)
				{
					GridPrices.Add(result.Position.EntryPrice);

					GridPrices.Add(robot.ShiftPrice(result.Position.EntryPrice, robot.RowHeights));
					if ()
					{ }
					robot.PlaceStopOrder(robot.GridTradeType, robot.Symbol.Name, robot.LotToVolume(robot.LotSize), "UG" + (robot.Positions.Count + 1))
				}
			});
		}

		internal bool IsReachingTarget
		{
			get
			{
				var netProfit = robot.Positions.Select(x => x.NetProfit).Sum();
				//netProfit += NetTemporaryLoss;
				robot.Print("Net Profit: {0}", netProfit.ToString());
				return (netProfit > robot.TakeNetProfit);

			}
		}
	}
}
