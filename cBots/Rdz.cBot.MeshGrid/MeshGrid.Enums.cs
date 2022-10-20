using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;


namespace Rdz.cBot
{
	public partial class MeshGrid
	{
		//Enumerations
		public enum enGridType
		{
			Both = 0, //Both
			BuyOnly = 1, //Buy Only
			SellOnly = 2, //Sell Only
		}

		public enum enGridCalculationMode
		{
			Anchor = 0, //From the anchor price
			CurrentPrice = 1, //From the current price
		}

		public struct KeyGrid
		{
			public GridLine Upper { get; set; }
			public GridLine Lower { get; set; }

			public GridLine UpperPreorder { get; set; }
			public GridLine LowerPreorder { get; set; }

			public GridLine UpperStopLoss { get; set; }
			public GridLine LowerStopLoss { get; set; }

			public ChartRectangle Cell { get; private set; }

			private bool NoStopLoss { get; set; }

			private const string CellName = "ngCell";
			private const int DefaultCellTransparency = 64;
			private const int CellWidth = 30;
			private static readonly Color NearestGridLinesDefaultColor = Color.Cyan;
			private static readonly Color PreorderZoneLinesDefaultColor = Color.Violet;
			private static readonly Color StopLossLinesDefaultColor = Color.Red;
			private static readonly Color CellDefaultColor = Color.Gray;

			public void Set(double UpperPrice, double LowerPrice, double UpperPreorderPrice, double LowerPreorderPrice)
			{
				Set(UpperPrice, LowerPrice, UpperPreorderPrice, LowerPreorderPrice, 0, 0);
			}
			public void Set(double UpperPrice, double LowerPrice, double UpperPreorderPrice, double LowerPreorderPrice, double UpperStopLossPrice, double LowerStopLossPrice)
			{
				if (UpperStopLossPrice == 0 && LowerStopLossPrice == 0)
				{
					NoStopLoss = true;
				}
				if (IsEmpty())
				{
					Upper = new GridLine(UpperPrice);
					Lower = new GridLine(LowerPrice);
					UpperPreorder = new GridLine(UpperPreorderPrice);
					LowerPreorder = new GridLine(LowerPreorderPrice);
					if (!NoStopLoss)
					{
						UpperStopLoss = new GridLine(UpperStopLossPrice);
						LowerStopLoss = new GridLine(LowerStopLossPrice);
					}
				}
				else
				{
					Upper.Price = UpperPrice;
					Lower.Price = LowerPrice;
					UpperPreorder.Price = UpperPreorderPrice;
					LowerPreorder.Price = LowerPreorderPrice;
					if (!NoStopLoss)
					{
						UpperStopLoss.Price = UpperStopLossPrice;
						LowerStopLoss.Price = LowerStopLossPrice;
					}
				}
			}

			public void ShowLines(Chart chart)
			{
				Upper.ShowLine(chart, NearestGridLinesDefaultColor);
				Lower.ShowLine(chart, NearestGridLinesDefaultColor);
				UpperPreorder.ShowLine(chart, PreorderZoneLinesDefaultColor);
				LowerPreorder.ShowLine(chart, PreorderZoneLinesDefaultColor);
				if (!NoStopLoss)
				{
					UpperStopLoss.ShowLine(chart, StopLossLinesDefaultColor);
					LowerStopLoss.ShowLine(chart, StopLossLinesDefaultColor);
				}
				Cell = chart.DrawRectangle(CellName, chart.BarsTotal - CellWidth, Upper.Price, chart.BarsTotal, Lower.Price, Color.FromArgb(DefaultCellTransparency, CellDefaultColor));
				Cell.IsFilled = true;
				Cell.IsLocked = true;
				
			}

			public void RemoveLines(Chart chart, Color lineColor)
			{
				Upper.RemoveLine(chart);
				Lower.RemoveLine(chart);
				UpperPreorder.RemoveLine(chart);
				LowerPreorder.RemoveLine(chart);
				if (!NoStopLoss)
				{
					UpperStopLoss.RemoveLine(chart);
					LowerStopLoss.RemoveLine(chart);
				}
				chart.RemoveObject(CellName);
			}

			public bool IsEmpty()
			{
				return
					(Upper == null || Lower == null || UpperPreorder == null || LowerPreorder == null) && NoStopLoss
					||
					(Upper == null || Lower == null || UpperPreorder == null || LowerPreorder == null || UpperStopLoss == null || LowerStopLoss == null) && !NoStopLoss;
			}
		}
	}
}
