using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using Rdz.cTrader.Library;

namespace Rdz.cBot
{
	[Robot(AccessRights = AccessRights.None)]
	public partial class MeshGrid : Robot
	{
		#region Variables
		List<GridLine> Grid = new List<GridLine>();
		KeyGrid keyGrid = new KeyGrid();

		private ChartIcon icon { get; set; }
		private const string LastStopLossIconName = "LastStopLossIcon";
		private const string MoneyFormat = "#,##0.00";

		private double ProfitAfterLastStopLoss { get; set; }


		private double Gap
		{
			get
			{
				return Symbol.PipSize * gridInterval;
			}
		}
		private double MarginDistanceSize
		{
			get
			{
				return Symbol.PipSize * marginDistance;
			}
		}
		private double Volume
		{
			get
			{
				return Symbol.LotToVolume(LotSize);
			}
		}
		#endregion

		#region Basic methods
		protected override void OnStart()
		{
			PendingOrders.Filled += PendingOrders_Filled;
			Positions.Closed += Positions_Closed;
			if (VisualAid) icon = Chart.DrawIcon(LastStopLossIconName, ChartIconType.Diamond, Chart.BarsTotal, Chart.Bars.LastBar.Close, Color.LightPink);
		}

		protected override void OnTick()
		{
			if (Grid.Count.IsBelowOrEqual(0))
			{
				CalculateNearestGrid(Symbol.Ask);
				generateInitialGridOrder();
			}
			else
			{
				//if there are grids
				if ((Symbol.Ask >= keyGrid.Upper.Price) || (Symbol.Ask <= keyGrid.Lower.Price))
				{
					CalculateNearestGrid(Symbol.Ask);
					ExpandGrid();
					ClearOutOfBoundGridLines();
					if (enableSmartStopLoss) RunSmartStopLoss();
				}
			}
		}

		protected override void OnTimer()
		{
		}

		protected override void OnStop()
		{
			//CloseEverything();
		}
		#endregion

		#region Custom methods
		private void CloseEverything()
		{
			foreach (var gridline in Grid)
			{
				gridline.RemoveText(Chart);
				gridline.RemoveLine(Chart);
				gridline.ClosePosition();
			}
			Grid.Clear();
		}

		private void ClearOutOfBoundGridLines()
		{
			var CountofRemovedItems = Grid.RemoveAll(x => x.IsEmpty && (x.Price > keyGrid.UpperPreorder.Price || x.Price < keyGrid.LowerPreorder.Price));
			if (PrintLogs) Print($"{CountofRemovedItems} grid lines removed (out of boundaries and empty).");
		}

		private void ExpandGrid()
		{
			TradeType[] tradeTypes = { TradeType.Buy, TradeType.Sell };

			foreach (var tradeType in tradeTypes)
			{
				//find the highest and lowest grid of the same TradeType, then add interval, and add a new 1 grid.
				var HighestGridLine = Grid.OrderByDescending(x => x.Price).FirstOrDefault(x => !x.IsEmpty && x.GridTradeType == tradeType);
				var LowestGridLine = Grid.OrderBy(x => x.Price).FirstOrDefault(x => !x.IsEmpty && x.GridTradeType == tradeType);

				if (HighestGridLine != null && HighestGridLine.Price < keyGrid.UpperPreorder.Price)
				{
					double Upper = HighestGridLine.Price + Gap;

					while (Upper < keyGrid.UpperPreorder.Price)
					{
						//for buy
						if (tradeType == TradeType.Buy)
						{
							var _gl = new GridLine(Upper);
							PlaceStopOrderAsync(TradeType.Buy, Symbol.Name, Volume, Upper, _gl.ShortID, null, gridInterval, (TradeResult result) => {
								_gl.PendingOrder = result.PendingOrder;
								if (VisualAid) _gl.ShowText(Chart, Color.LawnGreen);
								Grid.Add(_gl);
								if (PrintLogs) Print($"EXPAND: {result.PendingOrder.TradeType} of {LotSize} at {Upper}");
							});
						}
						//for sell
						if (tradeType == TradeType.Sell)
						{
							var _gl = new GridLine(Upper);
							PlaceLimitOrderAsync(TradeType.Sell, Symbol.Name, Volume, Upper, _gl.ShortID, null, gridInterval, (TradeResult result) => {
								_gl.PendingOrder = result.PendingOrder;
								if (VisualAid) _gl.ShowText(Chart, Color.Salmon);
								Grid.Add(_gl);
								if (PrintLogs) Print($"EXPAND: {result.PendingOrder.TradeType} of {LotSize} at {Upper}");
							});
						}
						Upper += Gap;
					}
				}

				if (LowestGridLine != null && LowestGridLine.Price > keyGrid.LowerPreorder.Price)
				{
					double Lower = LowestGridLine.Price - Gap;

					while (Lower > keyGrid.LowerPreorder.Price)
					{
						//for buy
						if (tradeType == TradeType.Buy)
						{
							var _gl = new GridLine(Lower);
							PlaceLimitOrderAsync(TradeType.Buy, Symbol.Name, Volume, Lower, _gl.ShortID, null, gridInterval, (TradeResult result) => {
								_gl.PendingOrder = result.PendingOrder;
								if (VisualAid) _gl.ShowText(Chart, Color.LawnGreen);
								Grid.Add(_gl);
								if (PrintLogs) Print($"EXPAND: {result.PendingOrder.TradeType} of {LotSize} at {Lower}");
							});
						}
						//for sell
						if (tradeType == TradeType.Sell)
						{
							var _gl = new GridLine(Lower);
							PlaceStopOrderAsync(TradeType.Sell, Symbol.Name, Volume, Lower, _gl.ShortID, null, gridInterval, (TradeResult result) => {
								_gl.PendingOrder = result.PendingOrder;
								if (VisualAid) _gl.ShowText(Chart, Color.Salmon);
								Grid.Add(_gl);
								if (PrintLogs) Print($"EXPAND: {result.PendingOrder.TradeType} of {LotSize} at {Lower}");
							});
						}
						Lower -= Gap;
					}
				}
			}
		}

		private void RunSmartStopLoss()
		{
			TradeType[] tradeTypes = { TradeType.Buy, TradeType.Sell };
			bool IsExecutedOnce = false;
			bool LossIncurred = false;
			double TotalOpenLoss = 0;

			foreach (var tradeType in tradeTypes)
			{
				//find all grid of specific trade types that outside of stop loss line and age more than the smart stop loss duration
				var StopLossGridLines = Grid.Where(x => !x.IsEmpty && x.GridTradeType == tradeType && (x.Price >= keyGrid.UpperStopLoss.Price || x.Price <= keyGrid.LowerStopLoss.Price) && ((x.IsFilled && Chart.Bars.LastBar.OpenTime - x.Position.EntryTime >= smartSLMaxDuration) || x.IsOrdered));
				TotalOpenLoss = StopLossGridLines.Where(x => x.IsFilled).Select(x => x.Position.NetProfit).Sum();
				var IsAllowedLossMeetThreshold = (Math.Abs(TotalOpenLoss) / ProfitAfterLastStopLoss) * 100 <= AllowedLossPercentage;

				if (IsAllowedLossMeetThreshold)
				{
					if (TotalOpenLoss < 0)
					{
						if (PrintLogs) Print($"Total Open Loss: {TotalOpenLoss.ToString(MoneyFormat)} and Profit after last stop loss: {ProfitAfterLastStopLoss.ToString(MoneyFormat)}");
						LossIncurred = true;
					}
					foreach (var slGridLine in StopLossGridLines)
					{
						if (slGridLine.IsFilled)
						{
							slGridLine.RemoveText(Chart);
							slGridLine.RemoveLine(Chart);
							slGridLine.ClosePosition();
							IsExecutedOnce = true;
						}

						if (slGridLine.IsOrdered)
						{
							slGridLine.RemoveText(Chart);
							slGridLine.RemoveLine(Chart);
							slGridLine.CancelOrder();
							IsExecutedOnce = true;
						}
					}
				}
			}

			if (IsExecutedOnce && LossIncurred)
			{
				ProfitAfterLastStopLoss = 0;
				icon = Chart.DrawIcon(LastStopLossIconName, ChartIconType.Diamond, Chart.BarsTotal, Chart.Bars.LastBar.Close, Color.LightPink);
			}
		}

		private void CalculateNearestGrid(double currentPrice)
		{
			double multiplier = Math.Round(currentPrice / Gap, 0, MidpointRounding.ToZero);

			var Upper = Gap * (multiplier + 1);
			var Lower = Gap * multiplier;
			if (enableSmartStopLoss)
			{
				keyGrid.Set(Upper, Lower, Symbol.ShiftPrice(Upper, PreorderZone), Symbol.ShiftPrice(Lower, -PreorderZone), Symbol.ShiftPrice(Upper, smartStopLossDistance), Symbol.ShiftPrice(Lower, -smartStopLossDistance));
			}
			else
			{
				keyGrid.Set(Upper, Lower, Symbol.ShiftPrice(Upper, PreorderZone), Symbol.ShiftPrice(Lower, -PreorderZone));
			}
			if (VisualAid) keyGrid.ShowLines(Chart);
		}

		private void generateInitialGridOrder()
		{
			double askPrice = Symbol.Ask;
			double Upper = keyGrid.Upper.Price;
			double Lower = keyGrid.Lower.Price;
			bool IsUpperBeyondMargin = Symbol.Distance(keyGrid.Upper.Price, askPrice) >= marginDistance;
			bool IsLowerBeyondMargin = Symbol.Distance(askPrice, keyGrid.Lower.Price) >= marginDistance;

			//check if nearest price is actually > allowed distance
			if (IsUpperBeyondMargin && IsLowerBeyondMargin)
			{
				while (Upper < keyGrid.UpperPreorder.Price)
				{
					//for buy
					if (gridType == enGridType.Both || gridType == enGridType.BuyOnly)
					{
						var _gl = new GridLine(Upper);
						PlaceStopOrderAsync(TradeType.Buy, Symbol.Name, Volume, Upper, _gl.ShortID, null, gridInterval, (TradeResult result) => {
							_gl.PendingOrder = result.PendingOrder;
							if (VisualAid) _gl.ShowText(Chart, Color.LawnGreen);
							Grid.Add(_gl);
							if (PrintLogs) Print($"{result.PendingOrder.TradeType} of {LotSize} at {Upper}");
						});
					}
					//for sell
					if (gridType == enGridType.Both || gridType == enGridType.SellOnly)
					{
						var _gl = new GridLine(Upper);
						PlaceLimitOrderAsync(TradeType.Sell, Symbol.Name, Volume, Upper, _gl.ShortID, null, gridInterval, (TradeResult result) => {
							_gl.PendingOrder = result.PendingOrder;
							if (VisualAid) _gl.ShowText(Chart, Color.Salmon);
							Grid.Add(_gl);
							if (PrintLogs) Print($"{result.PendingOrder.TradeType} of {LotSize} at {Upper}");
						});
					}
					Upper += Gap;
				}

				while (Lower > keyGrid.LowerPreorder.Price)
				{
					//for buy
					if (gridType == enGridType.Both || gridType == enGridType.BuyOnly)
					{
						var _gl = new GridLine(Lower);
						PlaceLimitOrderAsync(TradeType.Buy, Symbol.Name, Volume, Lower, _gl.ShortID, null, gridInterval, (TradeResult result) => {
							_gl.PendingOrder = result.PendingOrder;
							if (VisualAid) _gl.ShowText(Chart, Color.LawnGreen);
							Grid.Add(_gl);
							if (PrintLogs) Print($"{result.PendingOrder.TradeType} of {LotSize} at {Upper}");
						});
					}
					//for sell
					if (gridType == enGridType.Both || gridType == enGridType.SellOnly)
					{
						var _gl = new GridLine(Lower);
						PlaceStopOrderAsync(TradeType.Sell, Symbol.Name, Volume, Lower, _gl.ShortID, null, gridInterval, (TradeResult result) => {
							_gl.PendingOrder = result.PendingOrder;
							if (VisualAid) _gl.ShowText(Chart, Color.Salmon);
							Grid.Add(_gl);
							if (PrintLogs) Print($"{result.PendingOrder.TradeType} of {LotSize} at {Upper}");
						});
					}
					Lower -= Gap;
				}
			} else
			{
				if (PrintLogs) Print($"The price is too near to the nearest grid of {Upper} and {Lower}. Please wait until it reaches or beyond {marginDistance} pips.");
			}
		}
		#endregion

		#region Events method

		private void Positions_Closed(PositionClosedEventArgs obj)
		{
			var matchedGrid = Grid.FirstOrDefault(x => x.IsFilled && x.Position.Id == obj.Position.Id);
			if (matchedGrid != null)
			{
				//fill the previously closed object after Take Profit
				if (obj.Reason == PositionCloseReason.TakeProfit)
				{
					ProfitAfterLastStopLoss += obj.Position.NetProfit;

					if (obj.Position.TradeType == TradeType.Buy)
					{
						PlaceLimitOrderAsync(TradeType.Buy, Symbol.Name, Volume, matchedGrid.Price, matchedGrid.ShortID, null, gridInterval, (TradeResult result) => {
							matchedGrid.Clear();
							matchedGrid.PendingOrder = result.PendingOrder;
							if (PrintLogs) Print($"FILL: {result.PendingOrder.TradeType} of {LotSize} at {matchedGrid.Price}");
						});
					}
					else if (obj.Position.TradeType == TradeType.Sell)
					{
						PlaceLimitOrderAsync(TradeType.Sell, Symbol.Name, Volume, matchedGrid.Price, matchedGrid.ShortID, null, gridInterval, (TradeResult result) => {
							matchedGrid.Clear();
							matchedGrid.PendingOrder = result.PendingOrder;
							if (PrintLogs) Print($"FILL: {result.PendingOrder.TradeType} of {LotSize} at {matchedGrid.Price}");
						});
					}

				}
			}
		}

		private void PendingOrders_Filled(PendingOrderFilledEventArgs obj)
		{
			var matchedGrid = Grid.FirstOrDefault(x => x.IsOrdered && x.PendingOrder.Id == obj.PendingOrder.Id);
			if (matchedGrid != null)
			{
				matchedGrid.Position = obj.Position;
				matchedGrid.RefreshText(Chart, Color.WhiteSmoke);
			}
		}
		#endregion
	}
}