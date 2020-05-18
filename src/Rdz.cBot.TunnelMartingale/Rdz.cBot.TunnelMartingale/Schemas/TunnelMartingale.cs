using Rdz.cBot.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rdz.cBot.Library.Extensions;
using cAlgo.API;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Rdz.cBot.TunnelMartingale.Schemas
{
	internal class Tunnel
	{
		internal string TMLabel = "TM";
		internal TunnelMartingaleBot tmBot { get; private set; }
		internal Tunnel(TunnelMartingaleBot robot)
		{
			//Ask is always higher than Bid, Buy is always take the Ask while Sell is always take the Bid.
			tmBot = robot;
			LastTradeType = new Random().Next(1, 2) == 1 ? TradeType.Buy : TradeType.Sell;
			SessionNumber = 0;
			Statistics = new TunnelStatistics();
			TunnelChartObjects = new TunnelChartObjects();
			Initialize();
		}

		internal void Initialize()
		{
			TunnelStatus = enTunnelStatus.Inactive;
			LastVolume = tmBot.LotToVolume(tmBot.config.StartingLotSize);
			if (tmBot.config != null)
			{
				if (!tmBot.config.SessionDates.Enabled) // if SessionDates is not enabled
				{
					TunnelStatus = enTunnelStatus.Pending;
					if (tmBot.config.OpenCycleMethod == enOpenCycleMethod.AlwaysBuy || (tmBot.config.OpenCycleMethod == enOpenCycleMethod.RememberLastProfitable && LastTradeType == TradeType.Buy))
					{
						tmBot.ExecuteMarketOrderAsync(cAlgo.API.TradeType.Buy, tmBot.Symbol.Name, LastVolume, TMLabel + GetPositionIndexNumber() + PositionSessionNumber, InitialPositionOpened);
					}
					else if (tmBot.config.OpenCycleMethod == enOpenCycleMethod.AlwaysSell || (tmBot.config.OpenCycleMethod == enOpenCycleMethod.RememberLastProfitable && LastTradeType == TradeType.Sell))
					{
						tmBot.ExecuteMarketOrderAsync(cAlgo.API.TradeType.Sell, tmBot.Symbol.Name, LastVolume, TMLabel + GetPositionIndexNumber() + PositionSessionNumber, InitialPositionOpened);
					}
					else if (tmBot.config.OpenCycleMethod == enOpenCycleMethod.Trap && tmBot.Positions.Count == 0)
					{
						var HalfHeight = Math.Abs(tmBot.config.TunnelHeight / 2);
						TunnelCeiling = tmBot.ShiftPrice(tmBot.Symbol.Ask, HalfHeight);
						tmBot.Print("Tunnel Ceiling set at {0}", TunnelCeiling.ToString());
						TunnelFloor = tmBot.ShiftPrice(tmBot.Symbol.Bid, -HalfHeight);
						tmBot.Print("Tunnel Floor set at {0}", TunnelFloor.ToString());
						TunnelCenter = tmBot.ShiftPrice(TunnelFloor, (int)Math.Ceiling((double)(tmBot.config.TunnelHeight / 2)));
						tmBot.Print("Tunnel Center set at {0}", TunnelCenter.ToString());

						TunnelChartObjects.DrawMainLines(tmBot, TunnelFloor, TunnelCeiling, TunnelCenter);
						TunnelChartObjects.DrawMainLinesHistory(tmBot, TunnelFloor, TunnelCeiling, TunnelCenter, TMLabel + PositionSessionNumber);
						TunnelChartObjects.DrawDirection(tmBot, Direction);

						tmBot.PlaceStopOrderAsync(TradeType.Buy, tmBot.Symbol.Name, LastVolume, TunnelCeiling, TMLabel + GetPositionIndexNumber() + PositionSessionNumber, TrapStarted);
						tmBot.PlaceStopOrderAsync(TradeType.Sell, tmBot.Symbol.Name, LastVolume, TunnelFloor, TMLabel + GetPositionIndexNumber() + PositionSessionNumber, TrapStarted);
					}
					else
					{
						tmBot.Print("Open Cycle Method: '{0}' is not implemented", tmBot.config.OpenCycleMethod.ToString());
					}
				}
				else
				{
					ParsedSessionDates = GetParsedSessionDates();
					if (ParsedSessionDates.Count > 0)
					{
						tmBot.Print("Session Dates found {0} records. The next is at: {1:dd MMM yyyy HH:mm:ss}.", ParsedSessionDates.Count, ParsedSessionDates.First().ActualDate);
					}
					else
					{
						tmBot.Print("No more Session Dates.");
					}
				}
			}
			else
			{
				throw new InvalidOperationException("Configuration is not properly loaded.");
			}
		}

		private List<SessionInfo> GetParsedSessionDates()
		{
			tmBot.config.SessionDates.Sessions.ForEach(x => {
				x.ActualDate = x.Date.ParseDateTime(tmBot.config.SessionDates.ParseFormat);
			});
			return tmBot.config.SessionDates.Sessions.Where(x => x.Enabled && x.ActualDate.ToUniversalTime() >= tmBot.TimeInUtc).OrderBy(x => x.ActualDate).ToList();
		}

		private void TrapStarted(TradeResult result)
		{
			if (result.IsSuccessful)
			{
				//write here if successful
			}
			else
			{
				tmBot.Print("FAILED: Order failed to place at TrapStarted, reason: '" + (result.Error.HasValue ? result.Error.Value.ToString() : "no valid error") + "'.");
			}
		}
		private void InitialPositionOpened(TradeResult result)
		{
			if (result.IsSuccessful)
			{
				if ((tmBot.config.SessionDates.Enabled && ParsedSessionDates.First().SessionTradeType == TradeType.Buy) || tmBot.config.OpenCycleMethod == enOpenCycleMethod.AlwaysBuy || (tmBot.config.OpenCycleMethod == enOpenCycleMethod.RememberLastProfitable && LastTradeType == TradeType.Buy))
				{
					TunnelCeiling = result.Position.EntryPrice;
					tmBot.Print("Tunnel Ceiling set at {0}", TunnelCeiling.ToString());
					TunnelFloor = tmBot.ShiftPrice(TunnelCeiling, (int)tmBot.config.TunnelHeight * -1);
					tmBot.Print("Tunnel Floor set at {0}", TunnelFloor.ToString());
					TunnelCenter = tmBot.ShiftPrice(TunnelFloor, (int)Math.Ceiling((double)(tmBot.config.TunnelHeight / 2)));
					tmBot.Print("Tunnel Center set at {0}", TunnelCenter.ToString());

					TunnelChartObjects.DrawMainLines(tmBot, TunnelFloor, TunnelCeiling, TunnelCenter);
					TunnelChartObjects.DrawDirection(tmBot, Direction);

					LastVolume = result.Position.VolumeInUnits * tmBot.config.LotMultiplier;

					tmBot.PlaceStopOrderAsync(TradeType.Sell, tmBot.config.SessionDates.Enabled ? GetSymbolName(ParsedSessionDates.First()) : tmBot.Symbol.Name, LastVolume, TunnelFloor, TMLabel + GetPositionIndexNumber() +PositionSessionNumber, AnotherOrderPlaced);
				}
				else if ((tmBot.config.SessionDates.Enabled && ParsedSessionDates.First().SessionTradeType == TradeType.Sell) || tmBot.config.OpenCycleMethod == enOpenCycleMethod.AlwaysSell || (tmBot.config.OpenCycleMethod == enOpenCycleMethod.RememberLastProfitable && LastTradeType == TradeType.Sell))
				{
					TunnelFloor = result.Position.EntryPrice;
					tmBot.Print("Tunnel Floor set at {0}", TunnelFloor.ToString());
					TunnelCeiling = tmBot.ShiftPrice(TunnelFloor, (int)tmBot.config.TunnelHeight);
					tmBot.Print("Tunnel Ceiling set at {0}", TunnelCeiling.ToString());
					TunnelCenter = tmBot.ShiftPrice(TunnelFloor, (int)Math.Ceiling((double)(tmBot.config.TunnelHeight / 2)));
					tmBot.Print("Tunnel Center set at {0}", TunnelCenter.ToString());

					TunnelChartObjects.DrawMainLines(tmBot, TunnelFloor, TunnelCeiling, TunnelCenter);
					TunnelChartObjects.DrawDirection(tmBot, Direction);

					LastVolume = result.Position.VolumeInUnits * tmBot.config.LotMultiplier;

					tmBot.PlaceStopOrderAsync(TradeType.Buy, tmBot.config.SessionDates.Enabled ? GetSymbolName(ParsedSessionDates.First()) : tmBot.Symbol.Name, LastVolume, TunnelCeiling, TMLabel + GetPositionIndexNumber() +PositionSessionNumber, AnotherOrderPlaced);
				}
				Statistics.SetMaxBounce(tmBot.Positions.Count + 1, tmBot.Time);
			}
			else
			{
				tmBot.Print("FAILED: Order failed to place at InitialPositionOpened, reason: '" + (result.Error.HasValue ? result.Error.Value.ToString() : "no valid error") + "'.");
			}
		}

		private void AnotherOrderPlaced(TradeResult result)
		{
			if (result.IsSuccessful)
			{
				TunnelStatus = enTunnelStatus.Running;
				LastTradeType = OrderedPositions.Last().TradeType;
			}
			else
			{
				tmBot.Print("FAILED: Order failed to place at AnotherOrderPlaced, reason: '" + (result.Error.HasValue ? result.Error.Value.ToString() : "no valid error") + "'.");
			}
		}

		internal void PendingOrderFilled(PendingOrderFilledEventArgs result)
		{
			//add routine for Trap model, close all pending orders when first order is activated/filled
			if (tmBot.config.OpenCycleMethod == enOpenCycleMethod.Trap && result.Position.Label == TMLabel + "-I" + (1).ToString("#000") + PositionSessionNumber)
			{
				foreach (var to in tmBot.PendingOrders)
				{
					tmBot.CancelPendingOrderAsync(to);
				}
			}

			if (result.Position.Label == TMLabel + GetPositionIndexNumber(0) + PositionSessionNumber)
			{
				Statistics.SetMaxBounce(tmBot.Positions.Count, tmBot.Time);

				LastTradeType = result.Position.TradeType;
				TunnelStatus = enTunnelStatus.Pending;
				LastVolume = result.Position.VolumeInUnits * tmBot.config.LotMultiplier;

				tmBot.PlaceStopOrderAsync((result.Position.TradeType == TradeType.Buy ? TradeType.Sell : TradeType.Buy),
					tmBot.config.SessionDates.Enabled ? GetSymbolName(ParsedSessionDates.First()) : tmBot.Symbol.Name,
					result.Position.VolumeInUnits * tmBot.config.LotMultiplier,
					(result.Position.TradeType == TradeType.Buy ? TunnelFloor : TunnelCeiling),
					TMLabel + GetPositionIndexNumber() + PositionSessionNumber, AnotherOrderPlaced);
			}

			TunnelChartObjects.DrawDirection(tmBot, Direction);

			//no IF, executes every pending order filled
			UpdateTargetLines(result.Position.EntryPrice);
		}

		internal void UpdateTargetLines(double EntryPrice)
		{
			double breakEvenPrice =
				LastTradeType == TradeType.Buy ?
				tmBot.Positions.FindNetBreakEvenPrice(EntryPrice, tmBot.ShiftPrice(EntryPrice, tmBot.config.TunnelHeight), tmBot.Symbol.Digits) :
				tmBot.Positions.FindNetBreakEvenPrice(EntryPrice, tmBot.ShiftPrice(EntryPrice, -tmBot.config.TunnelHeight), tmBot.Symbol.Digits);
			tmBot.Print("Break Even Price is at: {0}", breakEvenPrice);
			BreakEven = breakEvenPrice;
			TunnelChartObjects.DrawBreakEvenPrice(tmBot, BreakEven);

			double targetPrice =
				LastTradeType == TradeType.Buy ?
				tmBot.Positions.FindTargetPrice(EntryPrice, tmBot.ShiftPrice(EntryPrice, tmBot.config.TunnelHeight), tmBot.Symbol.Digits, WhatIfProfit()) :
				tmBot.Positions.FindTargetPrice(EntryPrice, tmBot.ShiftPrice(EntryPrice, -tmBot.config.TunnelHeight), tmBot.Symbol.Digits, WhatIfProfit());
			tmBot.Print("Target Price is at: {0}", targetPrice);
			TunnelChartObjects.DrawTargetPrice(tmBot, targetPrice);
		}

		internal DateTime GetLatestTransDate()
		{
			if (tmBot.History == null || (tmBot.History != null && tmBot.History.Count == 0))
			{
				return tmBot.Positions.Last().EntryTime;
			}
			else
			{
				return tmBot.History.Last().EntryTime;
			}
		}

		internal string GetSymbolName(SessionInfo si)
		{
			if (tmBot.config.SessionDates.Enabled && si != null && si.SymbolRetrieval == enSymbolRetrieval.Custom && !string.IsNullOrEmpty(si.SymbolName))
			{
				return si.SymbolName;
			}
			else return tmBot.Symbol.Name;
		}

		internal void TickCheck()
		{
			//this is when Session Dates is enabled
			if (tmBot.config.SessionDates.Enabled && ParsedSessionDates != null && TunnelStatus == enTunnelStatus.Inactive)
			{
				if (tmBot.config.OpenCycleMethod == enOpenCycleMethod.Trap) //for Trap
				{
					if (ParsedSessionDates.Count > 0 && tmBot.TimeInUtc >= ParsedSessionDates.First().ActualDate.ToUniversalTime() && tmBot.TimeInUtc <= ParsedSessionDates.First().ActualDate.ToUniversalTime().Add(tmBot.config.SessionDates.Interval))
					{
						TunnelStatus = enTunnelStatus.Pending;
						SessionNumber += 1;
						//executing for Trap method
						var HalfHeight = Math.Abs(tmBot.config.TunnelHeight / 2);
						TunnelCeiling = tmBot.ShiftPrice(tmBot.Symbol.Ask, HalfHeight);
						tmBot.Print("Tunnel Ceiling set at {0}", TunnelCeiling.ToString());
						TunnelFloor = tmBot.ShiftPrice(tmBot.Symbol.Bid, -HalfHeight);
						tmBot.Print("Tunnel Floor set at {0}", TunnelFloor.ToString());
						TunnelCenter = tmBot.ShiftPrice(TunnelFloor, (int)Math.Ceiling((double)(tmBot.config.TunnelHeight / 2)));
						tmBot.Print("Tunnel Center set at {0}", TunnelCenter.ToString());

						TunnelChartObjects.DrawMainLines(tmBot, TunnelFloor, TunnelCeiling, TunnelCenter);
						TunnelChartObjects.DrawMainLinesHistory(tmBot, TunnelFloor, TunnelCeiling, TunnelCenter, TMLabel + PositionSessionNumber);
						TunnelChartObjects.DrawDirection(tmBot, Direction);

						LastVolume = tmBot.LotToVolume(tmBot.config.StartingLotSize);

						tmBot.PlaceStopOrderAsync(TradeType.Buy, tmBot.Symbol.Name, LastVolume, TunnelCeiling, TMLabel + GetPositionIndexNumber() + PositionSessionNumber, TrapStarted);
						tmBot.PlaceStopOrderAsync(TradeType.Sell, tmBot.Symbol.Name, LastVolume, TunnelFloor, TMLabel + GetPositionIndexNumber() + PositionSessionNumber, TrapStarted);

					}
				}
				else
				{
					if (ParsedSessionDates.Count > 0 && tmBot.TimeInUtc >= ParsedSessionDates.First().ActualDate.ToUniversalTime() && tmBot.TimeInUtc <= ParsedSessionDates.First().ActualDate.ToUniversalTime().Add(tmBot.config.SessionDates.Interval))
					{
						TunnelStatus = enTunnelStatus.Pending;
						SessionNumber += 1;
						LastVolume = tmBot.LotToVolume(tmBot.config.StartingLotSize);

						tmBot.ExecuteMarketOrderAsync(ParsedSessionDates.First().SessionTradeType, GetSymbolName(ParsedSessionDates.First()), LastVolume, TMLabel + GetPositionIndexNumber() + PositionSessionNumber, InitialPositionOpened);
					}
				}
			}

			if (tmBot.config.RunningCycleMethod == enRunningCycleMethod.NormalSmartBucket && JustPassedCenterTunnel)
			{
				RetreatTunnel();
			}

			if (OnTarget)
			{
				EndTunnel();
				Initialize();

				/*
				var lastTrans = (GetLatestTransDate() - tmBot.TimeInUtc);

				if (lastTrans.TotalDays >= 1 || (lastTrans.TotalDays < 1 && GetLatestTransDate().DayOfWeek != tmBot.TimeInUtc.DayOfWeek)) {
				}
				*/
			}
		}

		internal bool OnTarget
		{
			get
			{
				if (ParsedSessionDates.Count > 0)
				{
					bool profit = false;
					bool sessionDatesFollowParent = tmBot.config.SessionDates.Enabled && ParsedSessionDates.Count > 0 && ParsedSessionDates.First().Target.TargetType == enSessionTargetType.FollowParent;
					bool noSessionDatesOrFollowParent = !tmBot.config.SessionDates.Enabled || sessionDatesFollowParent;
					bool withStopLossReached = tmBot.config.Target.EnableStopLoss && CurrentProfit < tmBot.config.Target.FixedStopLoss;

					if (tmBot.config.Target.TargetType == enTargetType.FixedTargetProfit)
					{
						bool withTargetProfitReached = tmBot.config.Target.EnableTargetProfit && CurrentProfit > tmBot.config.Target.FixedTargetProfit;
						bool normalParentFixedTargetProfitReached = noSessionDatesOrFollowParent && withTargetProfitReached;
						bool normalParentFixedStopLossReached = noSessionDatesOrFollowParent && withStopLossReached;
						bool sessionFixedTargetProfit = tmBot.config.SessionDates.Enabled && ParsedSessionDates.Count > 0 && ParsedSessionDates.First().Target.TargetType == enSessionTargetType.FixedTargetProfit;
						bool withSessionTargetProfitReached = ParsedSessionDates.First().Target.EnableTargetProfit && CurrentProfit > ParsedSessionDates.First().Target.FixedTargetProfit;
						bool sessionDatesTargetProfitReached = !noSessionDatesOrFollowParent && sessionFixedTargetProfit && withSessionTargetProfitReached;
						bool withSessionStopLossReached = ParsedSessionDates.First().Target.EnableStopLoss && CurrentProfit < tmBot.config.Target.FixedStopLoss;
						bool sessionDatesStopLossReached = !noSessionDatesOrFollowParent && sessionFixedTargetProfit && withSessionStopLossReached;

						if (normalParentFixedTargetProfitReached)
						{
							tmBot.Print("Profit at {0}.", CurrentProfit.ToString("#0.00"));
						}
						if (sessionDatesTargetProfitReached)
						{
							tmBot.Print("Profit at {0} (from session date).", CurrentProfit.ToString("#0.00"));
						}
						if (normalParentFixedStopLossReached)
						{
							tmBot.Print("Cut Loss at {0}.", CurrentProfit.ToString("#0.00"));
						}
						if (sessionDatesStopLossReached)
						{
							tmBot.Print("Cut Loss at {0} (from session date).", CurrentProfit.ToString("#0.00"));
						}

						profit = (normalParentFixedTargetProfitReached || sessionDatesTargetProfitReached || normalParentFixedStopLossReached || sessionDatesStopLossReached) && TunnelStatus == enTunnelStatus.Running;
					}
					return profit;
				}
				else
					return false;
			}
		}

		internal bool JustPassedCenterTunnel
		{
			get
			{
				return
				(
					OrderedPositions.Count() > 1 &&
					(
					(OrderedPositions.Last().TradeType == TradeType.Sell && tmBot.Symbol.Bid >= TunnelCenter)
					||
					(OrderedPositions.Last().TradeType == TradeType.Buy && tmBot.Symbol.Ask <= TunnelCenter)
					)
				);
			}
		}

		internal void RetreatTunnel()
		{
			foreach (var to in tmBot.PendingOrders)
			{
				tmBot.CancelPendingOrderAsync(to, orderResult =>
				{
					if (orderResult.IsSuccessful)
					{
						tmBot.ClosePositionAsync(OrderedPositions.Last(), posResult =>
						{
							if (posResult.IsSuccessful)
							{
								NetLossBucket += posResult.Position.NetProfit;
								tmBot.Print("Net Loss Bucket: {0:#0.00}", NetLossBucket);

								LastVolume = posResult.Position.VolumeInUnits;

								if (tmBot.config.RunningCycleMethod == enRunningCycleMethod.MartingaleSmartBucket)
								{
									LastVolume = LastVolume * tmBot.config.LotMultiplier;
								}

								tmBot.PlaceStopOrderAsync(posResult.Position.TradeType,
									tmBot.Symbol.Name, LastVolume,
									(posResult.Position.TradeType == TradeType.Buy ? TunnelCeiling : TunnelFloor),
									TMLabel + GetPositionIndexNumber() + PositionSessionNumber, AnotherOrderPlaced);

								UpdateTargetLines(OrderedPositions.Last().EntryPrice);
							}
						});
					}
				});
			}
		}

		internal void EndTunnel()
		{
			if (TunnelStatus == enTunnelStatus.Running)
			{
				foreach (var tp in tmBot.Positions.OrderByDescending(x => x.NetProfit))
				{
					tmBot.ClosePosition(tp);
				}
				foreach (var to in tmBot.PendingOrders)
				{
					tmBot.CancelPendingOrder(to);
				}
				TunnelChartObjects.RemoveAll(tmBot);
			}
			NetLossBucket = 0;
			LastVolume = tmBot.LotToVolume(tmBot.config.StartingLotSize);

			if (TunnelStatus != enTunnelStatus.Inactive) TunnelStatus = enTunnelStatus.Inactive;
		}

		internal void EndSessions()
		{
			tmBot.Print("Highest bounce: {0}", Statistics.MaxBounce.ToString());
			if (Statistics.MaxBounce > 0)
			{
				tmBot.Print("Highest bounce at: {0:dd MMM yyyy} {0:HH:mm:ss}", Statistics.LastMaxBounce);
			}
		}

		internal string PositionSessionNumber
		{
			get
			{
				if (tmBot.config.SessionDates.Enabled)
				{
					return "-S" + SessionNumber.ToString("#000");
				}
				return string.Empty;
			}
		}

		internal string GetPositionIndexNumber(int offset = 1)
		{
			return "-I" + (tmBot.Positions.Count + offset).ToString("#000");
		}

		internal IEnumerable<Position> OrderedPositions
		{
			get
			{
				return tmBot.Positions.OrderBy(x => x.Label);
			}
		}

		internal TradeType ?Direction
		{
			get
			{
				if (OrderedPositions.Count() > 0)
				{
					return OrderedPositions.Last().TradeType;
				}
				else return null;
			}
		}

		internal double CurrentProfit
		{
			get
			{
				if (tmBot.config.RunningCycleMethod == enRunningCycleMethod.MartingaleSmartBucket || tmBot.config.RunningCycleMethod == enRunningCycleMethod.NormalSmartBucket)
				{
					return tmBot.Positions.Select(x => x.NetProfit).Sum() + NetLossBucket; //reduced by NetLossBucket.
				}
				else
				{
					return tmBot.Positions.Select(x => x.NetProfit).Sum();
				}
			}
		}

		internal double WhatIfProfit()
		{
			bool sessionDatesFollowParent = tmBot.config.SessionDates.Enabled && ParsedSessionDates.Count > 0 && ParsedSessionDates.First().Target.TargetType == enSessionTargetType.FollowParent;
			bool noSessionDatesOrFollowParent = !tmBot.config.SessionDates.Enabled || sessionDatesFollowParent;
			bool sessionDatesTarget = tmBot.config.SessionDates.Enabled && ParsedSessionDates.Count > 0 && ParsedSessionDates.First().Target.TargetType == enSessionTargetType.FixedTargetProfit;

			double TargetWhatIfProfit = 0;

			if (tmBot.config.Target.TargetType == enTargetType.FixedTargetProfit)
			{
				if (noSessionDatesOrFollowParent)
				{
					TargetWhatIfProfit = tmBot.config.Target.FixedTargetProfit;
				}
				else if (sessionDatesTarget)
				{
					TargetWhatIfProfit = ParsedSessionDates.First().Target.FixedTargetProfit;
				}

				if (tmBot.config.RunningCycleMethod == enRunningCycleMethod.MartingaleSmartBucket || tmBot.config.RunningCycleMethod == enRunningCycleMethod.NormalSmartBucket)
				{
					return TargetWhatIfProfit - NetLossBucket; //added with NetLossBucket.
				}
				else
				{
					return TargetWhatIfProfit;
				}
			}
			else
			{
				return double.NaN;
			}
		}






		/// <summary>
		/// It's the Ask price at the time of initialization plus a configurable points
		/// </summary>
		internal double TunnelCeiling { get; set; }
		/// <summary>
		/// It's the Bid price at the time of initialization minus a configurable points
		/// </summary>
		internal double TunnelFloor { get; set; }
		internal double TunnelCenter { get; set; }
		internal double BreakEven { get; set; }
		internal double NetLossBucket { get; set; }
		internal TradeType LastTradeType { get; set; }
		internal List<SessionInfo> ParsedSessionDates { get; set; }
		internal int SessionNumber { get; set; }
		internal enTunnelStatus TunnelStatus { get; private set; }

		internal double LastVolume { get; set; }


		internal TunnelStatistics Statistics { get; private set; }
		internal TunnelChartObjects TunnelChartObjects { get; private set; }
	}
	internal class TunnelChartObjects
	{
		internal ChartHorizontalLine TunnelFloor { get; set; }
		internal ChartHorizontalLine TunnelCeiling { get; set; }
		internal ChartHorizontalLine TunnelCenter { get; set; }
		internal ChartHorizontalLine BreakEven { get; set; }
		internal ChartHorizontalLine Target { get; set; }
		internal ChartIcon DirectionIcon { get; set; }

		internal void DrawMainLines(Robot robot, double FloorPrice, double CeilingPrice, double CenterPrice)
		{
			if (!double.IsNaN(FloorPrice))
			{
				TunnelFloor = robot.Chart.DrawHorizontalLine("TunnelFloor", FloorPrice, Color.Magenta, 1, LineStyle.DotsVeryRare);
			}
			if (!double.IsNaN(CeilingPrice))
			{
				TunnelCeiling = robot.Chart.DrawHorizontalLine("TunnelCeiling", CeilingPrice, Color.Magenta, 1, LineStyle.DotsVeryRare);
			}
			if (!double.IsNaN(CenterPrice))
			{
				TunnelCenter = robot.Chart.DrawHorizontalLine("TunnelCenter", CenterPrice, Color.Cyan, 2, LineStyle.Solid);
			}
		}

		internal void DrawMainLinesHistory(Robot robot, double FloorPrice, double CeilingPrice, double CenterPrice, string Label)
		{
			if (!double.IsNaN(FloorPrice))
			{
				robot.Chart.DrawIcon("TunnelFloor" + Label, ChartIconType.DownTriangle, robot.TimeInUtc, FloorPrice, Color.Green);
			}
			if (!double.IsNaN(CeilingPrice))
			{
				robot.Chart.DrawIcon("TunnelCeiling" + Label, ChartIconType.UpTriangle, robot.TimeInUtc, CeilingPrice, Color.Red);
			}
			if (!double.IsNaN(CenterPrice))
			{
				robot.Chart.DrawIcon("TunnelCenter" + Label, ChartIconType.Diamond, robot.TimeInUtc, CenterPrice, Color.Wheat);
			}
		}

		internal void DrawDirection(Robot robot, TradeType ?direction)
		{
			if (direction.HasValue)
			{
				robot.Print("Direction: {0}", direction.Value.ToString());
				DirectionIcon = robot.Chart.DrawIcon("DirectionIcon", (direction.Value == TradeType.Buy ? ChartIconType.UpArrow : ChartIconType.DownArrow), robot.TimeInUtc, robot.Bid, (direction.Value == TradeType.Buy ? Color.Green : Color.Red));
			}
			else
				robot.Print("Direction is not defined.");
		}

		internal void DrawBreakEvenPrice(Robot robot, double BreakEvenPrice)
		{
			if (!double.IsNaN(BreakEvenPrice))
			{
				BreakEven = robot.Chart.DrawHorizontalLine("BreakEvenLine", BreakEvenPrice, Color.Gray, 3, LineStyle.Solid);
			}
		}
		internal void DrawTargetPrice(Robot robot, double TargetPrice)
		{
			if (!double.IsNaN(TargetPrice))
			{
				Target = robot.Chart.DrawHorizontalLine("TargetLine", TargetPrice, Color.DarkOliveGreen, 2, LineStyle.Solid);
			}
		}

		internal void RemoveAll(Robot robot)
		{
			robot.Chart.RemoveObject("TunnelFloor");
			robot.Chart.RemoveObject("TunnelCeiling");
			robot.Chart.RemoveObject("TunnelCenter");
			robot.Chart.RemoveObject("BreakEvenLine");
			robot.Chart.RemoveObject("TargetLine");
			robot.Chart.RemoveObject("DirectionIcon");
		}
	}
	internal class TunnelStatistics
	{
		internal TunnelStatistics()
		{
			Reset();
		}
		internal void SetMaxBounce(int newValue, DateTime lastMaxBounce)
		{
			if (MaxBounce < newValue)
			{
				MaxBounce = newValue;
				LastMaxBounce = lastMaxBounce;
			}
		}
		internal int MaxBounce { get; set; }
		internal DateTime LastMaxBounce { get; set; }
		internal double MaxProfit { get; set; }
		internal void Reset()
		{
			MaxBounce = 0;
			MaxProfit = 0;
			LastMaxBounce = DateTime.MinValue;
		}
	}
}
