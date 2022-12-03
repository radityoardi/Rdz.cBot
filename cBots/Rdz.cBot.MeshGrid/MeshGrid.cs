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

		private StackPanel sp { get; set; }
		private TextBlock tb { get; set; }
		private ChartIcon icon { get; set; }
		private const string LastStopLossIconName = "LastStopLossIcon";
		private const string MoneyFormat = "#,##0.00";
		private const string PanelTextTemplate = $@"Total loss outside: {{TotalOpenLossOutside:{MoneyFormat}}} {{AssetCurrency}} ({{PctgLossVersusLastNetProfit:{MoneyFormat}}}%)
Net profit (all): {{NetProfitAll:{MoneyFormat}}} {{AssetCurrency}}
Net profit after last SL: {{NetProfitAfterLastStopLoss:{MoneyFormat}}} {{AssetCurrency}}
{{GridType}} {{MABasedDirection}}";

		private double NetProfitAfterLastStopLoss { get; set; }
		private int StopOnLossIterationCount { get; set; }

		private MovingAverage MAFast { get; set; }
		private MovingAverage MASlow { get; set; }

		private Guid LabelID { get; set; }

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

		private double? InitialTakeProfitPips
		{
			get
			{
				return takeProfitMode == enTakeProfitMode.StandardTakeProfit ? gridInterval * InitialTakeProfitThreshold : null;
			}
		}
		private double? InitialStopLossPips
		{
			get
			{
				return takeProfitMode == enTakeProfitMode.StandardTakeProfit ? null : gridInterval * InitialStopLossThreshold;
			}
		}

		private double MABasedDistance
		{
			get
			{
				return Symbol.Distance(MAFast.Result.Last(1), MASlow.Result.Last(1));
			}
		}

		private enMABasedDirection MABasedDirection
		{
			get
			{
				double _mad = MABasedDistance;
				if (_mad.IsAboveOrEqual(MAMinStrongDirection))
					return enMABasedDirection.Buy;
				else if (_mad.IsBelowOrEqual(-MAMinStrongDirection))
					return enMABasedDirection.Sell;
				else
					return enMABasedDirection.Sideways;
			}
		}
		private string ShortLabelID
		{
			get
			{
				return LabelID.ToString("D").Substring(9, 4);
			}
		}
		private string AutomaticLabeling
		{
			get
			{
				return AutoGenerateLabel ? ShortLabelID : Label;
			}
		}

		#endregion

		#region Basic methods
		protected override void OnStart()
		{
			if (PrintLogs) Print("[========== MeshGrid ==========]");
			LabelID = Guid.NewGuid();
			NetProfitAfterLastStopLoss = 0;
			StopOnLossIterationCount = 0;
			MAFast = Indicators.MovingAverage(MASource, MAPeriodFast, MovingAverageType);
			MASlow = Indicators.MovingAverage(MASource, MAPeriodSlow, MovingAverageType);
			PendingOrders.Filled += PendingOrders_Filled;
			Positions.Closed += Positions_Closed;


			if (VisualAid)
			{
				icon = Chart.DrawIcon(LastStopLossIconName, ChartIconType.Diamond, Chart.BarsTotal, Chart.Bars.LastBar.Close, Color.LightPink);
				sp = new StackPanel()
				{
					Width = Chart.Width / 3,
					Opacity = 0.9,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Bottom,
					BackgroundColor = Color.Gray,
				};
				tb = new TextBlock()
				{
					ForegroundColor = Color.White,
					Text = "--empty--",
					Width = sp.Width,
					FontSize = 12,
					FontWeight = FontWeight.Bold,
					Padding = 5,
					TextAlignment = TextAlignment.Left,
					TextWrapping = TextWrapping.WrapWithOverflow,
				};
				sp.AddChild(tb);
				Chart.AddControl(sp);
			}

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
			Chart.RemoveControl(sp);
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
			if (PrintLogs && CountofRemovedItems > 0) Print($"{CountofRemovedItems} grid lines removed (out of boundaries and empty).");
		}

		private void ExpandGrid()
		{
			/*
			 * Need to change the way Expand works, due to addition for MABased logic.
			 * It must add pending orders that is not ordered between Upper and UpperPreorder.
			*/
			TradeType[] tradeTypes = { TradeType.Buy, TradeType.Sell };

			foreach (var tradeType in tradeTypes)
			{
				bool IsBuyButSellOnly = (tradeType == TradeType.Buy && gridType == enGridType.SellOnly);
				bool IsSellButBuyOnly = (tradeType == TradeType.Sell && gridType == enGridType.BuyOnly);
				bool IsBuyButMABasedNotBuy = (tradeType == TradeType.Buy && gridType == enGridType.MABased && MABasedDirection != enMABasedDirection.Buy);
				bool IsSellButMABasedNotSell = (tradeType == TradeType.Sell && gridType == enGridType.MABased && MABasedDirection != enMABasedDirection.Sell);

				if (IsBuyButSellOnly || IsSellButBuyOnly || IsBuyButMABasedNotBuy || IsSellButMABasedNotSell) break;

				//find the highest and lowest grid of the same TradeType, then add interval, and add a new 1 grid.
				var HighestGridLine = Grid.OrderByDescending(x => x.Price).FirstOrDefault(x => !x.IsEmpty && x.GridTradeType == tradeType && x.Price >= keyGrid.Upper.Price && x.Price <= keyGrid.UpperPreorder.Price);
				var LowestGridLine = Grid.OrderBy(x => x.Price).FirstOrDefault(x => !x.IsEmpty && x.GridTradeType == tradeType && x.Price <= keyGrid.Lower.Price && x.Price >= keyGrid.LowerPreorder.Price);

				//var IsHigherCriteriaOkay = gridType != enGridType.MABased && HighestGridLine != null;
				var IsHigherCriteriaOkay = true;
				if (IsHigherCriteriaOkay)
				{
					double Upper = HighestGridLine != null ? HighestGridLine.Price + Gap : keyGrid.Upper.Price;

					while (Upper <= keyGrid.UpperPreorder.Price)
					{
						//for buy
						if (tradeType == TradeType.Buy)
						{
							var _gl = new GridLine(Upper);
							Grid.Add(_gl);
							PlaceStopOrderAsync(TradeType.Buy, Symbol.Name, Volume, Upper, AutomaticLabeling, InitialStopLossPips, InitialTakeProfitPips, null, $"{_gl.ShortID}-EXPAND", (TradeResult result) =>
							{
								_gl.UpdateAsyncOrderResult(result, Chart, VisualAid);
								if (result.IsSuccessful && PrintLogs) Print($"EXPAND-UPPER: {result.PendingOrder.TradeType} of {LotSize} at {result.PendingOrder.TargetPrice}");
							});
						}
						//for sell
						if (tradeType == TradeType.Sell)
						{
							var _gl = new GridLine(Upper);
							Grid.Add(_gl);
							PlaceLimitOrderAsync(TradeType.Sell, Symbol.Name, Volume, Upper, AutomaticLabeling, InitialStopLossPips, InitialTakeProfitPips, null, $"{_gl.ShortID}-EXPAND", (TradeResult result) =>
							{
								_gl.UpdateAsyncOrderResult(result, Chart, VisualAid);
								if (result.IsSuccessful && PrintLogs) Print($"EXPAND-UPPER: {result.PendingOrder.TradeType} of {LotSize} at {result.PendingOrder.TargetPrice}");
							});
						}
						Upper += Gap;
					}
				}

				//var IsLowerCriteriaOkay = gridType != enGridType.MABased && LowestGridLine != null;
				var IsLowerCriteriaOkay = true;
				if (IsLowerCriteriaOkay)
				{
					double Lower = LowestGridLine != null ? LowestGridLine.Price - Gap : keyGrid.Lower.Price;

					while (Lower >= keyGrid.LowerPreorder.Price)
					{
						//for buy
						if (tradeType == TradeType.Buy)
						{
							var _gl = new GridLine(Lower);
							Grid.Add(_gl);
							PlaceLimitOrderAsync(TradeType.Buy, Symbol.Name, Volume, Lower, AutomaticLabeling, InitialStopLossPips, InitialTakeProfitPips, null, $"{_gl.ShortID}-EXPAND", (TradeResult result) =>
							{
								_gl.UpdateAsyncOrderResult(result, Chart, VisualAid);
								if (result.IsSuccessful && PrintLogs) Print($"EXPAND-LOWER: {result.PendingOrder.TradeType} of {LotSize} at {result.PendingOrder.TargetPrice}");
							});
						}
						//for sell
						if (tradeType == TradeType.Sell)
						{
							var _gl = new GridLine(Lower);
							Grid.Add(_gl);
							PlaceStopOrderAsync(TradeType.Sell, Symbol.Name, Volume, Lower, AutomaticLabeling, InitialStopLossPips, InitialTakeProfitPips, null, $"{_gl.ShortID}-EXPAND", (TradeResult result) =>
							{
								_gl.UpdateAsyncOrderResult(result, Chart, VisualAid);
								if (result.IsSuccessful && PrintLogs) Print($"EXPAND-LOWER: {result.PendingOrder.TradeType} of {LotSize} at {result.PendingOrder.TargetPrice}");
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
			double TotalOpenLossOutside = 0, NetProfitAll = 0;

			if (takeProfitMode == enTakeProfitMode.StandardTakeProfit)
			{ //for standard take profit mode, stop loss will be handled here programmatically
			  //find all grid of specific trade types that outside of stop loss line and age more than the smart stop loss duration
				var StopLossGridLines = Grid.Where(x => !x.IsEmpty &&
				(x.Price >= keyGrid.UpperStopLoss.Price || x.Price <= keyGrid.LowerStopLoss.Price) &&
				((x.IsFilled && Chart.Bars.LastBar.OpenTime - x.Position.EntryTime >= smartSLMaxDuration) || x.IsOrdered)
				);
				TotalOpenLossOutside = StopLossGridLines.Where(x => x.IsFilled).Select(x => x.Position.NetProfit).Sum();
				NetProfitAll = Grid.Where(x => x.IsFilled).Select(x => x.Position.NetProfit).Sum();
				var PctgLossVersusLastNetProfit = (Math.Abs(TotalOpenLossOutside) / NetProfitAfterLastStopLoss) * 100;
				var IsAllowedLossMeetThreshold = PctgLossVersusLastNetProfit <= AllowedLossPercentage;

				dynamic data = new
				{
					AssetCurrency = Account.Asset.Name,
					TotalOpenLossOutside = TotalOpenLossOutside,
					PctgLossVersusLastNetProfit = PctgLossVersusLastNetProfit,
					NetProfitAll = NetProfitAll,
					NetProfitAfterLastStopLoss = NetProfitAfterLastStopLoss,
					GridType = gridType,
					MABasedDirection = MABasedDirection
				};

				tb.Text = PanelTextTemplate.FormatTemplate(data as object);
				/*
				tb.Text = $@"Total loss outside: {TotalOpenLossOutside.ToString(MoneyFormat)} {Account.Asset.Name} ({PctgLossVersusLastNetProfit.ToString("#,##0.00")}%)
Net profit (all): {NetProfitAll.ToString(MoneyFormat)} {Account.Asset.Name}
Net profit after last SL: {NetProfitAfterLastStopLoss.ToString(MoneyFormat)} {Account.Asset.Name}";
				if (gridType == enGridType.MABased)
				{
					tb.Text += $"\r\nMA Based: {MABasedDirection}";
				}
				*/

				if (IsAllowedLossMeetThreshold)
				{
					if (TotalOpenLossOutside < 0)
					{
						if (PrintLogs) Print(tb.Text);
						LossIncurred = true;
						StopOnLossIterationCount += 1;

						if (IsBacktesting && StopOnLossIteration.IsAbove(0) && StopOnLossIterationCount.IsAboveOrEqual(StopOnLossIteration))
						{
							Stop();
						}
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
			else if (takeProfitMode == enTakeProfitMode.TrailingStopLoss)
			{ //for trailing stop loss, stop loss will be updated here
				var TrailingSLGridLines = Grid.Where(x => !x.IsEmpty &&
				(x.Price >= keyGrid.UpperStopLoss.Price || x.Price <= keyGrid.LowerStopLoss.Price) && x.IsFilled
				);

				var UpperGridLines = TrailingSLGridLines.Where(x => x.Price >= keyGrid.UpperStopLoss.Price).OrderBy(x => x.Price);
				var LowerGridLines = TrailingSLGridLines.Where(x => x.Price <= keyGrid.LowerStopLoss.Price).OrderByDescending(x => x.Price);

				var UpperTrailingSL = UpperGridLines.Select(x => x.Price).DefaultIfEmpty(double.NaN).First();
				var LowerTrailingSL = LowerGridLines.Select(x => x.Price).DefaultIfEmpty(double.NaN).First();

				if (UpperTrailingSL != double.NaN)
				{
					foreach (var tslGridLine in UpperGridLines)
					{
						if (
								(tslGridLine.Position.TradeType == TradeType.Buy && tslGridLine.Position.StopLoss < UpperTrailingSL) ||
								(tslGridLine.Position.TradeType == TradeType.Sell && tslGridLine.Position.StopLoss > UpperTrailingSL)
						)
						{
							tslGridLine.Position.ModifyStopLossPrice(UpperTrailingSL);
						}
					}
				}

				if (LowerTrailingSL != double.NaN)
				{
					foreach (var tslGridLine in LowerGridLines)
					{
						if (
								(tslGridLine.Position.TradeType == TradeType.Buy && tslGridLine.Position.StopLoss < LowerTrailingSL) ||
								(tslGridLine.Position.TradeType == TradeType.Sell && tslGridLine.Position.StopLoss > LowerTrailingSL)
						)
						{
							tslGridLine.Position.ModifyStopLossPrice(LowerTrailingSL);
						}
					}
				}

			}



			if (takeProfitMode == enTakeProfitMode.StandardTakeProfit && IsExecutedOnce && LossIncurred)
			{
				NetProfitAfterLastStopLoss = 0;
				icon = Chart.DrawIcon(LastStopLossIconName, ChartIconType.Diamond, Chart.BarsTotal, Chart.Bars.LastBar.Close, Color.LightPink);
			}
		}

		private void CalculateNearestGrid(double currentPrice)
		{
			var nearestGridPrices = GetNearestGridPrices(currentPrice);

			double Upper = nearestGridPrices.Upper;
			double Lower = nearestGridPrices.Lower;
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

		private double GetNextUpperGridPrice(double currentPrice)
		{
			return Gap * (GetLowerMultiplierFromPrice(currentPrice) + 1);
		}

		private double GetNextLowerGridPrice(double currentPrice)
		{
			return Gap * GetLowerMultiplierFromPrice(currentPrice);
		}

		private double GetLowerMultiplierFromPrice(double currentPrice)
		{
			return Math.Round(currentPrice / Gap, 0, MidpointRounding.ToZero);
		}

		private dynamic GetNearestGridPrices(double currentPrice)
		{
			double multiplier = GetLowerMultiplierFromPrice(currentPrice);
			return new { Upper = Gap * (multiplier + 1), Lower = Gap * multiplier };
		}

		private void generateInitialGridOrder()
		{
			double askPrice = Symbol.Ask;
			double Upper = keyGrid.Upper.Price;
			double Lower = keyGrid.Lower.Price;
			bool IsUpperBeyondMargin = Symbol.Distance(keyGrid.Upper.Price, askPrice) >= marginDistance;
			bool IsLowerBeyondMargin = Symbol.Distance(askPrice, keyGrid.Lower.Price) >= marginDistance;

			bool IsMABasedBuy = gridType == enGridType.MABased && (MABasedDirection == enMABasedDirection.Buy || MABasedDirection == enMABasedDirection.Sideways);
			bool IsMABasedSell = gridType == enGridType.MABased && (MABasedDirection == enMABasedDirection.Sell || MABasedDirection == enMABasedDirection.Sideways);

			//check if nearest price is actually > allowed distance
			if (IsUpperBeyondMargin && IsLowerBeyondMargin)
			{
				//UPPER
				while (Upper < keyGrid.UpperPreorder.Price)
				{
					//for buy
					if (IsMABasedBuy || gridType == enGridType.Both || gridType == enGridType.BuyOnly)
					{
						var _gl = new GridLine(Upper);
						Grid.Add(_gl);
						PlaceStopOrderAsync(TradeType.Buy, Symbol.Name, Volume, Upper, AutomaticLabeling, InitialStopLossPips, InitialTakeProfitPips, null, $"{_gl.ShortID}-INITIAL", (TradeResult result) =>
						{
							_gl.UpdateAsyncOrderResult(result, Chart, VisualAid);
							if (result.IsSuccessful && PrintLogs) Print($"INITIAL: {result.PendingOrder.TradeType} of {result.PendingOrder.Quantity} at {result.PendingOrder.TargetPrice}");
						});
					}
					//for sell
					if (IsMABasedSell || gridType == enGridType.Both || gridType == enGridType.SellOnly)
					{
						var _gl = new GridLine(Upper);
						Grid.Add(_gl);
						PlaceLimitOrderAsync(TradeType.Sell, Symbol.Name, Volume, Upper, AutomaticLabeling, InitialStopLossPips, InitialTakeProfitPips, null, $"{_gl.ShortID}-INITIAL", (TradeResult result) =>
						{
							_gl.UpdateAsyncOrderResult(result, Chart, VisualAid);
							if (result.IsSuccessful && PrintLogs) Print($"INITIAL: {result.PendingOrder.TradeType} of {result.PendingOrder.Quantity} at {result.PendingOrder.TargetPrice}");
						});
					}
					Upper += Gap;
				}

				//LOWER
				while (Lower > keyGrid.LowerPreorder.Price)
				{
					//for buy
					if (IsMABasedBuy || gridType == enGridType.Both || gridType == enGridType.BuyOnly)
					{
						var _gl = new GridLine(Lower);
						Grid.Add(_gl);
						PlaceLimitOrderAsync(TradeType.Buy, Symbol.Name, Volume, Lower, AutomaticLabeling, InitialStopLossPips, InitialTakeProfitPips, null, $"{_gl.ShortID}-INITIAL", (TradeResult result) =>
						{
							_gl.UpdateAsyncOrderResult(result, Chart, VisualAid);
							if (result.IsSuccessful && PrintLogs) Print($"INITIAL: {result.PendingOrder.TradeType} of {result.PendingOrder.Quantity} at {result.PendingOrder.TargetPrice}");
						});
					}
					//for sell
					if (IsMABasedSell || gridType == enGridType.Both || gridType == enGridType.SellOnly)
					{
						var _gl = new GridLine(Lower);
						Grid.Add(_gl);
						PlaceStopOrderAsync(TradeType.Sell, Symbol.Name, Volume, Lower, AutomaticLabeling, InitialStopLossPips, InitialTakeProfitPips, null, $"{_gl.ShortID}-INITIAL", (TradeResult result) =>
						{
							_gl.UpdateAsyncOrderResult(result, Chart, VisualAid);
							if (result.IsSuccessful && PrintLogs) Print($"INITIAL: {result.PendingOrder.TradeType} of {result.PendingOrder.Quantity} at {result.PendingOrder.TargetPrice}");
						});
					}
					Lower -= Gap;
				}
			}
			else
			{
				if (PrintLogs) Print($"The price is too near to the nearest grid of {Upper} and {Lower}. Please wait until it reaches or beyond {marginDistance} pips.");
			}
		}
		#endregion

		#region Callback method
		#endregion

		#region Events method
		private void Positions_Closed(PositionClosedEventArgs obj)
		{
			var matchedGrid = Grid.FirstOrDefault(x => x.IsFilled && x.Position.Id == obj.Position.Id);
			if (matchedGrid != null)
			{
				var withinStopLossRange = matchedGrid.Price.IsBetween(keyGrid.UpperStopLoss.Price, keyGrid.LowerStopLoss.Price);

				var IsStandardTakeProfit = (obj.Reason == PositionCloseReason.TakeProfit || obj.Reason == PositionCloseReason.Closed) && takeProfitMode == enTakeProfitMode.StandardTakeProfit;
				var IsTrailingStopLoss = (obj.Reason == PositionCloseReason.StopLoss || obj.Reason == PositionCloseReason.Closed) && takeProfitMode == enTakeProfitMode.TrailingStopLoss;

				var withinAskMarginDistance = Symbol.Distance(Symbol.Ask, matchedGrid.Price, true) >= marginDistance; //for later


				//fill the previously closed object after Take Profit
				if (IsStandardTakeProfit && matchedGrid.Position.NetProfit > 0) NetProfitAfterLastStopLoss += obj.Position.NetProfit;

				if (obj.Position.TradeType == TradeType.Buy)
				{
					if (IsStandardTakeProfit || (IsTrailingStopLoss && matchedGrid.Price < Symbol.Ask))
					{
						PlaceLimitOrderAsync(TradeType.Buy, Symbol.Name, Volume, matchedGrid.Price, AutomaticLabeling, InitialStopLossPips, InitialTakeProfitPips, null, $"{matchedGrid.ShortID}-CLOSEDFILL", (TradeResult result) =>
						{
							matchedGrid.Clear();
							matchedGrid.UpdateAsyncOrderResult(result, Chart, VisualAid);
							if (result.IsSuccessful && PrintLogs) Print($"CLOSEDFILL: {result.PendingOrder.TradeType} of {LotSize} at {result.PendingOrder.TargetPrice}");

						});
					}
					else if (IsTrailingStopLoss && matchedGrid.Price > Symbol.Ask)
					{
						PlaceStopOrderAsync(TradeType.Buy, Symbol.Name, Volume, matchedGrid.Price, AutomaticLabeling, InitialStopLossPips, InitialTakeProfitPips, null, $"{matchedGrid.ShortID}-CLOSEDFILL", (TradeResult result) =>
						{
							matchedGrid.Clear();
							matchedGrid.UpdateAsyncOrderResult(result, Chart, VisualAid);
							if (result.IsSuccessful && PrintLogs) Print($"CLOSEDFILL: {result.PendingOrder.TradeType} of {LotSize} at {result.PendingOrder.TargetPrice}");
						});
					}
				}
				else if (obj.Position.TradeType == TradeType.Sell)
				{
					if (IsStandardTakeProfit || (IsTrailingStopLoss && matchedGrid.Price > Symbol.Ask))
					{
						PlaceLimitOrderAsync(TradeType.Sell, Symbol.Name, Volume, matchedGrid.Price, AutomaticLabeling, InitialStopLossPips, InitialTakeProfitPips, null, $"{matchedGrid.ShortID}-CLOSEDFILL", (TradeResult result) =>
						{
							matchedGrid.Clear();
							matchedGrid.UpdateAsyncOrderResult(result, Chart, VisualAid);
							if (result.IsSuccessful && PrintLogs) Print($"CLOSEDFILL: {result.PendingOrder.TradeType} of {LotSize} at {result.PendingOrder.TargetPrice}");
						});
					}
					else if (IsTrailingStopLoss && matchedGrid.Price < Symbol.Ask)
					{
						PlaceStopOrderAsync(TradeType.Sell, Symbol.Name, Volume, matchedGrid.Price, AutomaticLabeling, InitialStopLossPips, InitialTakeProfitPips, null, $"{matchedGrid.ShortID}-CLOSEDFILL", (TradeResult result) =>
						{
							matchedGrid.Clear();
							matchedGrid.UpdateAsyncOrderResult(result, Chart, VisualAid);
							if (result.IsSuccessful && PrintLogs) Print($"CLOSEDFILL: {result.PendingOrder.TradeType} of {LotSize} at {result.PendingOrder.TargetPrice}");
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
				matchedGrid.RefreshText(Chart, Color.Cyan);
			}
		}
		#endregion
	}
}