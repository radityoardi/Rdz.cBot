using Rdz.cBot.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rdz.cBot.Library.Extensions;
using cAlgo.API;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using cAlgo.API.Indicators;

namespace Rdz.cBot.TunnelMartingale.Schemas
{
	internal class TunnelMartingaleEngine
	{
		internal const string TMLabel = "TM";
		internal const string TMLabelSeparator = "-";
		internal TunnelMartingaleBot tmBot { get; private set; }
		internal RelativeStrengthIndex rsi { get; set; }
		internal BollingerBands bb { get; set; }
		internal TunnelMartingaleEngine(TunnelMartingaleBot robot)
		{
			//Ask is always higher than Bid, Buy is always take the Ask while Sell is always take the Bid.
			tmBot = robot;
			LastTradeType = new Random().Next(1, 2) == 1 ? TradeType.Buy : TradeType.Sell;
			SessionNumber = 0;
			Statistics = new TunnelStatistics();
			TunnelChartObjects = new TunnelChartObjects();
			rsi = tmBot.Indicators.RelativeStrengthIndex(tmBot.Bars.ClosePrices, tmBot.RSIPeriods);
			bb = tmBot.Indicators.BollingerBands(tmBot.Bars.ClosePrices, tmBot.BBPeriods, 2, tmBot.BBMAType);
			if (tmBot.config.InitialBucket.IsPositive())
			{
				NetLossBucket = !tmBot.config.InitialBucket.IsNegative() ? -tmBot.config.InitialBucket : tmBot.config.InitialBucket;
				tmBot.Print("Initial bucket set at {0:#,##0.00}", NetLossBucket);
			}
			Initialize();
		}

		internal void Initialize()
		{
			tmBot.Print("»»»»»»»» Initializing...");
			TunnelStatus = enTunnelStatus.Inactive;
			LastVolume = tmBot.LotToVolume(StartingLotSize);
			if (tmBot.config != null)
			{
				if (!IsSessionDatesEnabled) // if SessionDates is not enabled
				{
					TunnelStatus = enTunnelStatus.PendingActivation;
					DateMarker = DateTime.Now;
					if (tmBot.config.OpenCycleMethod == enOpenCycleMethod.AlwaysBuy || (tmBot.config.OpenCycleMethod == enOpenCycleMethod.RememberLastProfitable && LastTradeType == TradeType.Buy))
					{
						tmBot.ExecuteMarketOrderAsync(TradeType.Buy, tmBot.Symbol.Name, LastVolume, TMLabel + GetPositionIndexNumber() + PositionSessionNumber, InitialPositionOpened);
					}
					else if (tmBot.config.OpenCycleMethod == enOpenCycleMethod.AlwaysSell || (tmBot.config.OpenCycleMethod == enOpenCycleMethod.RememberLastProfitable && LastTradeType == TradeType.Sell))
					{
						tmBot.ExecuteMarketOrderAsync(TradeType.Sell, tmBot.Symbol.Name, LastVolume, TMLabel + GetPositionIndexNumber() + PositionSessionNumber, InitialPositionOpened);
					}
					else if (tmBot.config.OpenCycleMethod == enOpenCycleMethod.Trap)
					{
						if (tmPositions.Count() == 0 && IsTrapTime)
						{
							int HalfHeight = Math.Abs(TunnelHeight / 2);
							TunnelCeiling = tmBot.ShiftPriceInPips(tmBot.Symbol.Ask, HalfHeight);
							tmBot.Print("Tunnel Ceiling set at {0}", TunnelCeiling.ToString());
							TunnelFloor = tmBot.ShiftPriceInPips(tmBot.Symbol.Bid, -HalfHeight);
							tmBot.Print("Tunnel Floor set at {0}", TunnelFloor.ToString());

							TunnelCenter = TunnelCeiling.FindCenterAgainst(TunnelFloor, tmBot.Symbol.Digits);
							tmBot.Print("Tunnel Center set at {0}", TunnelCenter.ToString());

							TunnelChartObjects.DrawMainLines(tmBot, TunnelFloor, TunnelCeiling, TunnelCenter);
							TunnelChartObjects.DrawMainLinesHistory(tmBot, TunnelFloor, TunnelCeiling, TunnelCenter, TMLabel + PositionSessionNumber);
							TunnelChartObjects.DrawDirection(tmBot, Direction);

							tmBot.PlaceStopOrderAsync(TradeType.Buy, tmBot.Symbol.Name, LastVolume, TunnelCeiling, TMLabel + GetPositionIndexNumber() + PositionSessionNumber, TrapStarted);
							tmBot.PlaceStopOrderAsync(TradeType.Sell, tmBot.Symbol.Name, LastVolume, TunnelFloor, TMLabel + GetPositionIndexNumber() + PositionSessionNumber, TrapStarted);
						}
						else
						{
							tmBot.Print("BollingerBandsDistance: {0}", BollingerBandsDistance);
							TunnelStatus = enTunnelStatus.Inactive;
						}
					}
					else
					{
						tmBot.Print("Open Cycle Method: '{0}' is not implemented", tmBot.config.OpenCycleMethod.ToString());
						TunnelStatus = enTunnelStatus.Inactive;
					}
				}
				else
				{
					// if SessionDates is enabled
					ParsedSessionDates = GetParsedSessionDates();
					CurrentSessionDate = IsSessionDatesExists ? ParsedSessionDates.First() : null;

					if (ParsedSessionDates.Count > 0)
					{
						tmBot.Print("Session Dates found {0} records. The next is at: {1:dd MMM yyyy HH:mm:ss}.", ParsedSessionDates.Count, CurrentSessionDate.ActualStartDate);
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

		private List<TradeSession> GetParsedSessionDates()
		{
			tmBot.config.SessionDates.Sessions.ForEach(x => {
				x.ActualStartDate = x.Date.ParseDateTime(tmBot.config.SessionDates.ParseFormat);
				x.ActualEndDate = x.Date.ParseDateTime(tmBot.config.SessionDates.ParseFormat).Add(tmBot.config.SessionDates.Interval);
			});
			return tmBot.config.SessionDates.Sessions.Where(x => x.Enabled && x.ActualStartDate.ToUniversalTime() >= tmBot.TimeInUtc).OrderBy(x => x.ActualStartDate).ToList();
		}

		private void TrapStarted(TradeResult result)
		{
			if (result.IsSuccessful)
			{
				//write here if successful
				TunnelStatus = enTunnelStatus.PendingOrders;
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
				if ((IsSessionDatesEnabled && CurrentSessionDate.SessionTradeType == TradeType.Buy) || tmBot.config.OpenCycleMethod == enOpenCycleMethod.AlwaysBuy || (tmBot.config.OpenCycleMethod == enOpenCycleMethod.RememberLastProfitable && LastTradeType == TradeType.Buy))
				{
					TunnelCeiling = result.Position.EntryPrice;
					tmBot.Print("Tunnel Ceiling set at {0}", TunnelCeiling.ToString());
					TunnelFloor = tmBot.ShiftPriceInPips(TunnelCeiling, -TunnelHeight);
					tmBot.Print("Tunnel Floor set at {0}", TunnelFloor.ToString());
					TunnelCenter = TunnelCeiling.FindCenterAgainst(TunnelFloor, tmBot.Symbol.Digits);
					tmBot.Print("Tunnel Center set at {0}", TunnelCenter.ToString());

					TunnelChartObjects.DrawMainLines(tmBot, TunnelFloor, TunnelCeiling, TunnelCenter);
					TunnelChartObjects.DrawDirection(tmBot, Direction);

					LastVolume = tmBot.Symbol.NormalizeVolumeInUnits(result.Position.VolumeInUnits * tmBot.config.LotMultiplier);

					tmBot.PlaceStopOrderAsync(TradeType.Sell, IsSessionDatesEnabled ? GetSymbolName(CurrentSessionDate) : tmBot.Symbol.Name, LastVolume, TunnelFloor, TMLabel + GetPositionIndexNumber() +PositionSessionNumber, AnotherOrderPlaced);
				}
				else if ((IsSessionDatesEnabled && CurrentSessionDate.SessionTradeType == TradeType.Sell) || tmBot.config.OpenCycleMethod == enOpenCycleMethod.AlwaysSell || (tmBot.config.OpenCycleMethod == enOpenCycleMethod.RememberLastProfitable && LastTradeType == TradeType.Sell))
				{
					TunnelFloor = result.Position.EntryPrice;
					tmBot.Print("Tunnel Floor set at {0}", TunnelFloor.ToString());
					TunnelCeiling = tmBot.ShiftPriceInPips(TunnelFloor, TunnelHeight);
					tmBot.Print("Tunnel Ceiling set at {0}", TunnelCeiling.ToString());
					TunnelCenter = TunnelCeiling.FindCenterAgainst(TunnelFloor, tmBot.Symbol.Digits);
					tmBot.Print("Tunnel Center set at {0}", TunnelCenter.ToString());

					TunnelChartObjects.DrawMainLines(tmBot, TunnelFloor, TunnelCeiling, TunnelCenter);
					TunnelChartObjects.DrawDirection(tmBot, Direction);

					LastVolume = tmBot.Symbol.NormalizeVolumeInUnits(result.Position.VolumeInUnits * tmBot.config.LotMultiplier);

					tmBot.PlaceStopOrderAsync(TradeType.Buy, IsSessionDatesEnabled ? GetSymbolName(CurrentSessionDate) : tmBot.Symbol.Name, LastVolume, TunnelCeiling, TMLabel + GetPositionIndexNumber() +PositionSessionNumber, AnotherOrderPlaced);
				}
				Statistics.SetMaxBounce(tmPositions.Count() + 1, tmBot.Time);
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
			if (result.Position.Label.StartsWith(TMLabel + TMLabelSeparator))
			{
				//add routine for Trap model, close all pending orders when first order is activated/filled
				if (tmBot.config.OpenCycleMethod == enOpenCycleMethod.Trap && result.Position.Label == TMLabel + TMLabelSeparator + "I" + (1).ToString("#000") + PositionSessionNumber)
				{
					TunnelStatus = enTunnelStatus.PendingActivation;
					foreach (var to in tmBot.PendingOrders)
					{
						tmBot.CancelPendingOrderAsync(to);
					}

				}

				if (result.Position.Label == TMLabel + GetPositionIndexNumber(0) + PositionSessionNumber) //check whether this order filled is the last position, if yes then...
				{
					Statistics.SetMaxBounce(tmPositions.Count(), tmBot.Time);

					LastTradeType = result.Position.TradeType;
					TunnelStatus = enTunnelStatus.PendingActivation;
					LastVolume = tmBot.Symbol.NormalizeVolumeInUnits(result.Position.VolumeInUnits * tmBot.config.LotMultiplier);
					tmBot.Print("LastVolume: {0:#,##0}, Previous Trade: {1}", LastVolume, LastTradeType.ToString());

					tmBot.PlaceStopOrderAsync(result.Position.TradeType == TradeType.Buy ? TradeType.Sell : TradeType.Buy,
						IsSessionDatesEnabled ? GetSymbolName(CurrentSessionDate) : tmBot.Symbol.Name,
						LastVolume,
						(result.Position.TradeType == TradeType.Buy ? TunnelFloor : TunnelCeiling),
						TMLabel + GetPositionIndexNumber() + PositionSessionNumber, AnotherOrderPlaced);
				}

				TunnelChartObjects.DrawDirection(tmBot, Direction);

				//no IF, executes every pending order filled
				UpdateTargetLines(result.Position.EntryPrice);
			}
		}

		internal void PositionsClosed(PositionClosedEventArgs result)
		{
			if (result.Position.Label.StartsWith(TMLabel + TMLabelSeparator))
			{
				if (result.Position.Label.EndsWith(GetPositionIndexNumber(-tmPositions.Count() + 1) + PositionSessionNumber))
				{
					//EndTunnel(); //not on, due to cyclic reference
				}
			}
		}

		internal void UpdateTargetLines(double EntryPrice)
		{
			double breakEvenPrice =
				LastTradeType == TradeType.Buy ?
				tmPositions.FindNetBreakEvenPrice(EntryPrice, tmBot.ShiftPriceInPips(EntryPrice, TunnelHeight), tmBot.Symbol.Digits) :
				tmPositions.FindNetBreakEvenPrice(EntryPrice, tmBot.ShiftPriceInPips(EntryPrice, -TunnelHeight), tmBot.Symbol.Digits);
			tmBot.Print("Break Even Price is at: {0}", breakEvenPrice);
			BreakEven = breakEvenPrice;
			TunnelChartObjects.DrawBreakEvenPrice(tmBot, BreakEven);

			double targetPrice =
				LastTradeType == TradeType.Buy ?
				tmPositions.FindTargetPrice(EntryPrice, tmBot.ShiftPriceInPips(EntryPrice, TunnelHeight), tmBot.Symbol.Digits, WhatIfProfit()) :
				tmPositions.FindTargetPrice(EntryPrice, tmBot.ShiftPriceInPips(EntryPrice, -TunnelHeight), tmBot.Symbol.Digits, WhatIfProfit());
			tmBot.Print("Target Price is at: {0}", targetPrice);
			TunnelChartObjects.DrawTargetPrice(tmBot, targetPrice);
		}

		internal DateTime GetLatestTransDate()
		{
			if (tmBot.History == null || (tmBot.History != null && tmBot.History.Count == 0))
			{
				return tmPositions.Last().EntryTime;
			}
			else
			{
				return tmBot.History.Last().EntryTime;
			}
		}

		internal string GetSymbolName(TradeSession si)
		{
			if (IsSessionDatesEnabled && si != null && si.SymbolRetrieval == enSymbolRetrieval.Custom && !string.IsNullOrEmpty(si.SymbolName))
			{
				return si.SymbolName;
			}
			else return tmBot.Symbol.Name;
		}

		internal void TickCheck()
		{
			if (TunnelStatus == enTunnelStatus.Running || TunnelStatus == enTunnelStatus.PendingOrders)
			{
				//when it's a trap, and out of the validity period
				if (tmBot.config.OpenCycleMethod == enOpenCycleMethod.Trap && tmPositions.Count() == 0 && tmBot.config.Trap.Validity != TimeSpan.Zero && (IsSessionDatesExists && tmBot.TimeInUtc >= CurrentSessionDate.ActualStartDate.Add(tmBot.config.Trap.Validity).ToUniversalTime()) || (!IsSessionDatesEnabled && tmBot.TimeInUtc >= DateMarker.ToUniversalTime()))
				{
					tmBot.Print("Validity period is over now at: {0:dd MMM yyyy HH:mm:ss}, closing all and repeating stuff.", IsSessionDatesExists ? CurrentSessionDate.ActualStartDate.Add(tmBot.config.Trap.Validity) : DateMarker);
					EndTunnel();
				}
				//when using SmartBucket
				if (TunnelStatus == enTunnelStatus.Running && UsesAnySmartBucket && IsSmartBucketInCycle && JustPassedCenterTunnel)
				{
					RetreatTunnel();
				}
				if (TunnelStatus == enTunnelStatus.Running && OnTarget)
				{
					EndTunnel();
				}
			}
			else if (TunnelStatus == enTunnelStatus.Inactive)
			{
				if (tmBot.config.OpenCycleMethod == enOpenCycleMethod.Trap && ((IsSessionDatesExists && IsWithinCurrentSessionTime) || (!IsSessionDatesExists && IsTrapTime)))
				{
					TunnelStatus = enTunnelStatus.PendingActivation;
					SessionNumber += 1;
					//executing for Trap method
					var HalfHeight = Math.Abs(TunnelHeight / 2);
					TunnelCeiling = tmBot.ShiftPriceInPips(tmBot.Symbol.Ask, HalfHeight);
					tmBot.Print("Tunnel Ceiling set at {0}", TunnelCeiling.ToString());
					TunnelFloor = tmBot.ShiftPriceInPips(tmBot.Symbol.Bid, -HalfHeight);
					tmBot.Print("Tunnel Floor set at {0}", TunnelFloor.ToString());
					TunnelCenter = TunnelCeiling.FindCenterAgainst(TunnelFloor, tmBot.Symbol.Digits);
					tmBot.Print("Tunnel Center set at {0}", TunnelCenter.ToString());

					TunnelChartObjects.DrawMainLines(tmBot, TunnelFloor, TunnelCeiling, TunnelCenter);
					TunnelChartObjects.DrawMainLinesHistory(tmBot, TunnelFloor, TunnelCeiling, TunnelCenter, TMLabel + PositionSessionNumber);
					TunnelChartObjects.DrawDirection(tmBot, Direction);

					LastVolume = tmBot.LotToVolume(StartingLotSize);

					tmBot.PlaceStopOrderAsync(TradeType.Buy, tmBot.Symbol.Name, LastVolume, TunnelCeiling, TMLabel + GetPositionIndexNumber() + PositionSessionNumber, TrapStarted);
					tmBot.PlaceStopOrderAsync(TradeType.Sell, tmBot.Symbol.Name, LastVolume, TunnelFloor, TMLabel + GetPositionIndexNumber() + PositionSessionNumber, TrapStarted);
				}
				else if (IsSessionDatesExists && IsWithinCurrentSessionTime && tmBot.config.OpenCycleMethod != enOpenCycleMethod.Trap)
				{
					TunnelStatus = enTunnelStatus.PendingActivation;
					SessionNumber += 1;
					LastVolume = tmBot.LotToVolume(StartingLotSize);

					tmBot.ExecuteMarketOrderAsync(CurrentSessionDate.SessionTradeType, GetSymbolName(CurrentSessionDate), LastVolume, TMLabel + GetPositionIndexNumber() + PositionSessionNumber, InitialPositionOpened);
				}
			}
		}

		internal bool OnTarget
		{
			get
			{
				bool profit = false;
				if (TunnelStatus == enTunnelStatus.Running)
				{
					bool sessionDatesFollowParent = IsSessionDatesExists && CurrentSessionDate != null && CurrentSessionDate.Target.TargetType == enSessionTargetType.FollowParent;
					bool noSessionDatesOrFollowParent = !IsSessionDatesEnabled || sessionDatesFollowParent;
					bool withStopLossReached = tmBot.config.Target.EnableStopLoss && CurrentProfit < tmBot.config.Target.FixedStopLoss;
					bool endofIntervalSession = tmBot.config.SessionDates.CloseAllOrdersWhenIntervalEnds && IsSessionDatesExists && CurrentSessionDate != null && tmBot.TimeInUtc >= CurrentSessionDate.ActualEndDate.ToUniversalTime();

					if (tmBot.config.Target.TargetType == enTargetType.FixedTargetProfit)
					{
						bool withTargetProfitReached = tmBot.config.Target.EnableTargetProfit && CurrentProfit > tmBot.config.Target.FixedTargetProfit;

						/*
						//bool IsInsideRSI = rsi.Result.Last() < tmBot.RSIUpperLevel && rsi.Result.Last() > tmBot.RSILowerLevel; //experimental
						bool IsOutsideRSI = rsi.Result.Last() > tmBot.RSIUpperLevel || rsi.Result.Last() < tmBot.RSILowerLevel; //experimental
						bool normalParentFixedTargetProfitReached = noSessionDatesOrFollowParent && (withTargetProfitReached && IsOutsideRSI);
						*/

						bool normalParentFixedTargetProfitReached = noSessionDatesOrFollowParent && withTargetProfitReached;
						bool normalParentFixedStopLossReached = noSessionDatesOrFollowParent && withStopLossReached;
						bool sessionFixedTargetProfit = IsSessionDatesExists && CurrentSessionDate != null && CurrentSessionDate.Target.TargetType == enSessionTargetType.FixedTargetProfit;
						bool withSessionTargetProfitReached = IsSessionDatesExists && CurrentSessionDate != null && CurrentSessionDate.Target.EnableTargetProfit && CurrentProfit > CurrentSessionDate.Target.FixedTargetProfit;
						bool sessionDatesTargetProfitReached = !noSessionDatesOrFollowParent && sessionFixedTargetProfit && withSessionTargetProfitReached;
						bool withSessionStopLossReached = CurrentSessionDate != null && CurrentSessionDate.Target.EnableStopLoss && CurrentProfit < tmBot.config.Target.FixedStopLoss;
						bool sessionDatesStopLossReached = !noSessionDatesOrFollowParent && sessionFixedTargetProfit && withSessionStopLossReached;

						if (normalParentFixedTargetProfitReached)
						{
							tmBot.Print("Recorded Profit at {0}.", CurrentProfit.ToString("#0.00"));
						}
						if (sessionDatesTargetProfitReached)
						{
							tmBot.Print("Recorded Profit at {0} (from session date).", CurrentProfit.ToString("#0.00"));
						}
						if (normalParentFixedStopLossReached)
						{
							tmBot.Print("Recorded Loss at {0}.", CurrentProfit.ToString("#0.00"));
						}
						if (sessionDatesStopLossReached)
						{
							tmBot.Print("Recorded Loss at {0} (from session date).", CurrentProfit.ToString("#0.00"));
						}

						profit = (normalParentFixedTargetProfitReached || sessionDatesTargetProfitReached || normalParentFixedStopLossReached || sessionDatesStopLossReached || endofIntervalSession);

						if (profit && endofIntervalSession)
						{
							tmBot.Print("Interval session ended with recorded Profit at {0}.", CurrentProfit.ToString("#0.00"));
						}
					}
				}

				return profit;
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
								tmBot.Print("Net Loss Bucket: {0:#,##0.00}.", NetLossBucket);

								LastVolume = posResult.Position.VolumeInUnits;

								if (tmBot.config.RunningCycleMethod == enRunningCycleMethod.MartingaleSmartBucket)
								{
									LastVolume = tmBot.Symbol.NormalizeVolumeInUnits(LastVolume * tmBot.config.LotMultiplier);
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
			if (TunnelStatus != enTunnelStatus.Inactive)
			{
				//Positions closing
				if (tmPositions.Count().IsZero())
				{
					tmBot.Print("No position to close.");
					if (NetLossBucket.IsNegative())
					{
						tmBot.Print("Net Loss Bucket: {0:#,##0.00} will be brought over to the next session date.", NetLossBucket);
					}
				}
				else
				{
					var closingPosIDs = new List<int>();//to feed stuff so that the code below can only execute after all been closed
					closingPosIDs.AddRange(PositionsToClose.Select(x => x.Id));


					foreach (var tp in PositionsToClose)
					{
						//tmBot.ClosePositionAsync(tp, AfterEndTunnelPosition);
						tmBot.ClosePositionAsync(tp, poscloseResult =>
						{
							if (poscloseResult.IsSuccessful)
							{
								closingPosIDs.RemoveAll(x => x == poscloseResult.Position.Id);

								ActualProfit += poscloseResult.Position.NetProfit;

								tmBot.Print("Temporary Actual Profit: {0:#,##0.00}.", ActualProfit);

								if (closingPosIDs.Count.IsZero()) //when there's no more position on the line
								{
									tmBot.Print("No more positions, Final Actual Profit: {0:#,##0.00}.", ActualProfit);

									if (UsesAnySmartBucket)
									{
										if (IsSmartBucketAcrossCycle && ActualProfit.IsNegative())
										{
											NetLossBucket = ActualProfit;
											tmBot.Print("Net Loss Bucket: {0:#,##0.00} will be brought over to the next session date.", NetLossBucket);
										}
										else
										{
											NetLossBucket = 0;
											tmBot.Print("Net Loss Bucket resets to zero (0).");
										}
									}

									ActualProfit = 0; //reset the Actual Profit
								}
							}
						});
						//tmBot.ClosePosition(tp);
					}
				}

				//Pending Orders closing
				if (tmBot.PendingOrders.Count.IsZero())
				{
					tmBot.Print("No pending order to close.");
					//in case no pending orders, things must be reset first.
					tmBot.Print("Removing chart objects, set the initial volume back, and set tunnel to inactive, then re-initialize.");
					TunnelChartObjects.RemoveAll(tmBot);
					LastVolume = tmBot.LotToVolume(StartingLotSize);
					if (TunnelStatus != enTunnelStatus.Inactive) TunnelStatus = enTunnelStatus.Inactive;
					Initialize();
				}
				else
				{
					var closingPendOrderIDs = new List<int>();//to feed stuff so that the code below can only execute after all been closed
					closingPendOrderIDs.AddRange(tmBot.PendingOrders.Select(x => x.Id));

					foreach (var to in tmBot.PendingOrders)
					{
						//tmBot.CancelPendingOrderAsync(to, AfterEndTunnelPendingOrder);
						tmBot.CancelPendingOrderAsync(to, orderCloseResult =>
						{
							if (orderCloseResult.IsSuccessful)
							{
								closingPendOrderIDs.RemoveAll(x => x == orderCloseResult.PendingOrder.Id);

								if (closingPendOrderIDs.Count.IsZero()) //when there's no more position on the line
								{
									tmBot.Print("Removing chart objects, set the initial volume back, and set tunnel to inactive, then re-initialize.");
									TunnelChartObjects.RemoveAll(tmBot);
									LastVolume = tmBot.LotToVolume(StartingLotSize);
									if (TunnelStatus != enTunnelStatus.Inactive) TunnelStatus = enTunnelStatus.Inactive;
									Initialize();
								}
							}
						});
						//tmBot.CancelPendingOrder(to);
					}
				}
			}
		}

		internal void EndSessions()
		{
			tmBot.Print("Highest bounce: {0}", Statistics.MaxBounce.ToString());
			if (Statistics.MaxBounce > 0)
			{
				tmBot.Print("Highest bounce at: {0:dd MMM yyyy} {0:HH:mm:ss}", Statistics.LastMaxBounce);
			}
		}


		internal int BollingerBandsDistance
		{
			get
			{
				return tmBot.DistanceInPips(bb.Top.LastValue, bb.Bottom.LastValue, true);
			}
		}

		internal int TunnelHeight
		{
			get
			{
				switch (tmBot.config.TunnelHeightMode)
				{
					case enTunnelHeightMode.Fixed:
						return tmBot.config.TunnelHeight;
					case enTunnelHeightMode.BollingerBandsDistance:
						return BollingerBandsDistance;
				}
				return 0;
			}
		}

		internal double StartingLotSize
		{
			get
			{
				switch (tmBot.config.StartingLotSizeType)
				{
					case enStartingLotSizeType.Fixed:
						return tmBot.config.StartingLotSize;
					case enStartingLotSizeType.BalancePercentage:
						return tmBot.Account.Balance * StartingLotSizeBalancePercentage;
					default:
						return tmBot.config.StartingLotSize;
				}
			}
		}

		internal double StartingLotSizeBalancePercentage
		{
			get
			{
				if (tmBot.config.StartingLotSize > 1) return 1;
				else if (tmBot.config.StartingLotSize < 0) return 0;
				else return tmBot.config.StartingLotSize;
			}
		}

		internal string PositionSessionNumber
		{
			get
			{
				if (IsSessionDatesEnabled)
				{
					return TMLabelSeparator + "S" + SessionNumber.ToString("#000");
				}
				return string.Empty;
			}
		}
		internal bool IsSessionDatesEnabled
        {
			get
            {
				return tmBot.config.SessionDates.Enabled;
			}
        }
		internal bool IsSessionDatesExists
		{
			get
			{
				return IsSessionDatesEnabled && ParsedSessionDates.Count > 0;
			}
		}

		internal string GetPositionIndexNumber(int offset = 1)
		{
			return TMLabelSeparator + "I" + (tmPositions.Count() + offset).ToString("#000");
		}

		internal IEnumerable<Position> tmPositions
		{
			get
			{
				return tmBot.Positions.Where(x => x.Label.StartsWith(TMLabel + TMLabelSeparator));
			}
		}
		internal IEnumerable<Position> OrderedPositions
		{
			get
			{
				return tmPositions.OrderBy(x => x.Label);
			}
		}

		internal IEnumerable<Position> PositionsToClose
		{
			get
			{
				return tmPositions.OrderBy(x => x.NetProfit);
			}
		}

		internal bool IsTrapTime
		{
			get
			{
				return tmBot.config.Trap.TrapEntryMethod == enTrapEntryMethod.Normal || (tmBot.config.Trap.TrapEntryMethod == enTrapEntryMethod.BollingerBandsDistance && tmBot.config.Trap.IsTrapEntryTime(BollingerBandsDistance));
			}
		}

		internal bool IsWithinCurrentSessionTime
		{
			get
			{
				return tmBot.TimeInUtc >= CurrentSessionDate.ActualStartDate.ToUniversalTime() && tmBot.TimeInUtc <= CurrentSessionDate.ActualStartDate.ToUniversalTime().Add(tmBot.config.SessionDates.Interval);
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
				if (UsesAnySmartBucket)
				{
					return tmPositions.Select(x => x.NetProfit).Sum() + NetLossBucket; //reduced by NetLossBucket.
				}
				else
				{
					return tmPositions.Select(x => x.NetProfit).Sum();
				}
			}
		}
		internal bool UsesAnySmartBucket
		{
			get
			{
				return tmBot.config.RunningCycleMethod == enRunningCycleMethod.MartingaleSmartBucket || tmBot.config.RunningCycleMethod == enRunningCycleMethod.NormalSmartBucket;
			}
		}

		internal bool IsSmartBucketAcrossCycle
		{
			get
			{
				return UsesAnySmartBucket && (tmBot.config.SmartBucketModel == enSmartBucketModel.All || tmBot.config.SmartBucketModel == enSmartBucketModel.AcrossCycleOnly);
			}
		}

		internal bool IsSmartBucketInCycle
		{
			get
			{
				return UsesAnySmartBucket && (tmBot.config.SmartBucketModel == enSmartBucketModel.All || tmBot.config.SmartBucketModel == enSmartBucketModel.InCycleOnly);
			}
		}

		internal double WhatIfProfit()
		{
			bool sessionDatesFollowParent = IsSessionDatesEnabled && ParsedSessionDates.Count > 0 && CurrentSessionDate.Target.TargetType == enSessionTargetType.FollowParent;
			bool noSessionDatesOrFollowParent = !IsSessionDatesEnabled || sessionDatesFollowParent;
			bool sessionDatesTarget = IsSessionDatesEnabled && ParsedSessionDates.Count > 0 && CurrentSessionDate.Target.TargetType == enSessionTargetType.FixedTargetProfit;

			double TargetWhatIfProfit = 0;

			if (tmBot.config.Target.TargetType == enTargetType.FixedTargetProfit)
			{
				if (noSessionDatesOrFollowParent)
				{
					TargetWhatIfProfit = tmBot.config.Target.FixedTargetProfit;
				}
				else if (sessionDatesTarget)
				{
					TargetWhatIfProfit = CurrentSessionDate.Target.FixedTargetProfit;
				}

				if (UsesAnySmartBucket)
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
		internal List<TradeSession> ParsedSessionDates { get; set; }
		internal TradeSession CurrentSessionDate { get; set; }
		internal DateTime DateMarker { get; set; }
		internal int SessionNumber { get; set; }
		internal enTunnelStatus TunnelStatus { get; private set; }

		internal double LastVolume { get; set; }
		internal double ActualProfit { get; set; }


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
