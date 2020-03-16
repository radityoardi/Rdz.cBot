using Rdz.cBot.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rdz.cBot.Library.Extensions;
using cAlgo.API;

namespace Rdz.cBot.TunnelMartingale.Schemas
{
	internal class Tunnel
	{
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
			if (!tmBot.config.SessionDates.Enabled)
			{
				TunnelStatus = enTunnelStatus.Pending;
				if (tmBot.config.OpenCycleMethod == enOpenCycleMethod.AlwaysBuy || (tmBot.config.OpenCycleMethod == enOpenCycleMethod.RememberLastProfitable && LastTradeType == TradeType.Buy))
				{
					tmBot.ExecuteMarketOrderAsync(cAlgo.API.TradeType.Buy, tmBot.Symbol.Name, tmBot.LotToVolume(tmBot.config.StartingLotSize), "TM" + (tmBot.Positions.Count + 1).ToString() + GetSessionNumber, InitialPositionOpened);
				}
				else if (tmBot.config.OpenCycleMethod == enOpenCycleMethod.AlwaysSell || (tmBot.config.OpenCycleMethod == enOpenCycleMethod.RememberLastProfitable && LastTradeType == TradeType.Sell))
				{
					tmBot.ExecuteMarketOrderAsync(cAlgo.API.TradeType.Sell, tmBot.Symbol.Name, tmBot.LotToVolume(tmBot.config.StartingLotSize), "TM" + (tmBot.Positions.Count + 1).ToString() + GetSessionNumber, InitialPositionOpened);
				}
				else if (tmBot.config.OpenCycleMethod == enOpenCycleMethod.Trap && tmBot.Positions.Count == 0)
				{
					var HalfHeight = Math.Abs(tmBot.config.TunnelHeight / 2);
					TunnelCeiling = tmBot.ShiftPrice(tmBot.Symbol.Ask, HalfHeight);
					tmBot.Print("Tunnel Ceiling set at {0}", TunnelCeiling.ToString());
					TunnelFloor = tmBot.ShiftPrice(tmBot.Symbol.Bid, -HalfHeight);
					tmBot.Print("Tunnel Floor set at {0}", TunnelFloor.ToString());
					TunnelCenter = (TunnelCeiling - TunnelFloor) / 2;

					TunnelChartObjects.DrawMainLines(tmBot, TunnelFloor, TunnelCeiling, TunnelCenter);

					tmBot.PlaceStopOrderAsync(TradeType.Buy, tmBot.Symbol.Name, tmBot.LotToVolume(tmBot.config.StartingLotSize), TunnelCeiling, "TM" + (tmBot.Positions.Count + 1).ToString() + GetSessionNumber, TrapStarted);
					tmBot.PlaceStopOrderAsync(TradeType.Sell, tmBot.Symbol.Name, tmBot.LotToVolume(tmBot.config.StartingLotSize), TunnelFloor, "TM" + (tmBot.Positions.Count + 1).ToString() + GetSessionNumber, TrapStarted);
				}
				else
				{
					tmBot.Print("Open Cycle Method: '{0}' is not implemented", tmBot.config.OpenCycleMethod.ToString());
				}
			}
			else
			{
				ParsedSessionDates = GetParsedSessionDates();
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

					TunnelChartObjects.DrawMainLines(tmBot, TunnelFloor, TunnelCeiling, TunnelCenter);

					tmBot.PlaceStopOrderAsync(TradeType.Sell, tmBot.config.SessionDates.Enabled ? GetSymbolName(ParsedSessionDates.First()) : tmBot.Symbol.Name, result.Position.VolumeInUnits * tmBot.config.LotMultiplier, TunnelFloor, "TM" + (tmBot.Positions.Count + 1).ToString() + GetSessionNumber, AnotherOrderPlaced);
				}
				else if ((tmBot.config.SessionDates.Enabled && ParsedSessionDates.First().SessionTradeType == TradeType.Sell) || tmBot.config.OpenCycleMethod == enOpenCycleMethod.AlwaysSell || (tmBot.config.OpenCycleMethod == enOpenCycleMethod.RememberLastProfitable && LastTradeType == TradeType.Sell))
				{
					TunnelFloor = result.Position.EntryPrice;
					tmBot.Print("Tunnel Floor set at {0}", TunnelFloor.ToString());
					TunnelCeiling = tmBot.ShiftPrice(TunnelFloor, (int)tmBot.config.TunnelHeight);
					tmBot.Print("Tunnel Ceiling set at {0}", TunnelCeiling.ToString());
					TunnelCenter = tmBot.ShiftPrice(TunnelFloor, (int)Math.Ceiling((double)(tmBot.config.TunnelHeight / 2)));

					TunnelChartObjects.DrawMainLines(tmBot, TunnelFloor, TunnelCeiling, TunnelCenter);

					tmBot.PlaceStopOrderAsync(TradeType.Buy, tmBot.config.SessionDates.Enabled ? GetSymbolName(ParsedSessionDates.First()) : tmBot.Symbol.Name, result.Position.VolumeInUnits * tmBot.config.LotMultiplier, TunnelCeiling, "TM" + (tmBot.Positions.Count + 1).ToString() + GetSessionNumber, AnotherOrderPlaced);
				}
				Statistics.SetMaxBounce(tmBot.Positions.Count + 1, tmBot.Time);
			}
			else
			{
				tmBot.Print(result.ToString());
			}
		}

		private void AnotherOrderPlaced(TradeResult result)
		{
			if (result.IsSuccessful) TunnelStatus = enTunnelStatus.Running;
		}

		internal void PendingOrderFilled(PendingOrderFilledEventArgs result)
		{
			//add routine for Trap model, close all pending orders
			if (tmBot.config.OpenCycleMethod == enOpenCycleMethod.Trap && result.Position.Label == "TM1" + GetSessionNumber)
			{
				foreach (var to in tmBot.PendingOrders)
				{
					tmBot.CancelPendingOrderAsync(to);
				}
			}

			if (result.Position.Label == "TM" + (tmBot.Positions.Count).ToString() + GetSessionNumber)
			{
				Statistics.SetMaxBounce(tmBot.Positions.Count, tmBot.Time);

				LastTradeType = result.Position.TradeType;
				TunnelStatus = enTunnelStatus.Pending;
				tmBot.PlaceStopOrderAsync((result.Position.TradeType == TradeType.Buy ? TradeType.Sell : TradeType.Buy),
					tmBot.config.SessionDates.Enabled ? GetSymbolName(ParsedSessionDates.First()) : tmBot.Symbol.Name,
					result.Position.VolumeInUnits * tmBot.config.LotMultiplier,
					(result.Position.TradeType == TradeType.Buy ? TunnelFloor : TunnelCeiling),
					"TM" + (tmBot.Positions.Count + 1).ToString() + GetSessionNumber, AnotherOrderPlaced);
			}
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
			if (tmBot.config.SessionDates.Enabled && ParsedSessionDates != null && TunnelStatus == enTunnelStatus.Inactive)
			{
				if (ParsedSessionDates.Count > 0 && tmBot.TimeInUtc >= ParsedSessionDates.First().ActualDate.ToUniversalTime() && tmBot.TimeInUtc <= ParsedSessionDates.First().ActualDate.ToUniversalTime().Add(tmBot.config.SessionDates.Interval))
				{
					TunnelStatus = enTunnelStatus.Pending;
					SessionNumber += 1;
					tmBot.ExecuteMarketOrderAsync(ParsedSessionDates.First().SessionTradeType, GetSymbolName(ParsedSessionDates.First()), tmBot.LotToVolume(tmBot.config.StartingLotSize), "TM" + (tmBot.Positions.Count + 1).ToString() + GetSessionNumber, InitialPositionOpened);
				}
			}

			/*
			if (IsPassingCenter())
			{
				RetreatTunnel();
			}
			*/
			if (IsReachingTargetProfit())
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

		internal bool IsReachingTargetProfit()
		{
			double netProfit = 0;
			if (tmBot.config.TargetType == enTargetType.FixedTargetProfit)
			{
				netProfit = tmBot.Positions.Select(x => x.NetProfit).Sum();
			}
			else if (tmBot.config.TargetType == enTargetType.LevelTargetProfit)
			{
				
			}
			//netProfit += NetTemporaryLoss;
			var noSessionDatesOrFollowParent = !tmBot.config.SessionDates.Enabled || (tmBot.config.SessionDates.Enabled && ParsedSessionDates.Count > 0 && ParsedSessionDates.First().TargetType == enSessionTargetType.FollowParent);
			var normalParentTargetProfit = noSessionDatesOrFollowParent && (netProfit > tmBot.config.FixedTargetProfit);

			var sessionDatesTargetProfit = !noSessionDatesOrFollowParent && (tmBot.config.SessionDates.Enabled && ParsedSessionDates.Count > 0 && ParsedSessionDates.First().TargetType == enSessionTargetType.FixedTargetProfit && netProfit > ParsedSessionDates.First().FixedTargetProfit);
			var profit = ((normalParentTargetProfit || sessionDatesTargetProfit) && TunnelStatus == enTunnelStatus.Running);
			if (profit) tmBot.Print("Recorded net profit: {0}", netProfit.ToString("#,##0.00"));
			return profit;
		}

		internal bool IsPassingCenter()
		{
			var orderpos = tmBot.Positions.OrderBy(x => x.Label);
			return
				(
				tmBot.Positions.Count > 1 &&
				(
				(tmBot.Positions.Last().TradeType == TradeType.Sell && tmBot.Symbol.Bid >= TunnelCenter)
				||
				(tmBot.Positions.Last().TradeType == TradeType.Buy && tmBot.Symbol.Ask <= TunnelCenter)
				)
				);
		}

		internal void RetreatTunnel()
		{
			foreach (var to in tmBot.PendingOrders)
			{
				tmBot.CancelPendingOrderAsync(to, orderResult =>
				{
					if (orderResult.IsSuccessful)
					{
						tmBot.ClosePositionAsync(tmBot.Positions.OrderBy(x => x.Label).Last(), posResult =>
						{
							if (posResult.IsSuccessful)
							{
								NetTemporaryLoss += posResult.Position.NetProfit;
								tmBot.PlaceStopOrderAsync(posResult.Position.TradeType,
									tmBot.Symbol.Name, posResult.Position.VolumeInUnits,
									(posResult.Position.TradeType == TradeType.Buy ? TunnelCeiling : TunnelFloor),
									"TM" + (tmBot.Positions.Count + 1).ToString() + GetSessionNumber, AnotherOrderPlaced);
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
			NetTemporaryLoss = 0;
			if (TunnelStatus != enTunnelStatus.Inactive) TunnelStatus = enTunnelStatus.Inactive;
		}

		internal void EndSessions()
		{
			tmBot.Print("Highest bounce: {0}", Statistics.MaxBounce.ToString());
			tmBot.Print("Highest bounce at: {0:dd MMM yyyy} {0:HH:mm:ss}", Statistics.LastMaxBounce);
		}

		internal string GetSessionNumber
		{
			get
			{
				if (tmBot.config.SessionDates.Enabled)
				{
					return "-S" + SessionNumber.ToString();
				}
				return string.Empty;
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
		internal double NetTemporaryLoss { get; set; }
		internal TradeType LastTradeType { get; set; }
		internal List<SessionInfo> ParsedSessionDates { get; set; }
		internal int SessionNumber { get; set; }
		internal enTunnelStatus TunnelStatus { get; private set; }


		internal TunnelStatistics Statistics { get; private set; }
		internal TunnelChartObjects TunnelChartObjects { get; private set; }
	}
	internal class TunnelChartObjects
	{
		internal ChartHorizontalLine TunnelFloor { get; set; }
		internal ChartHorizontalLine TunnelCeiling { get; set; }
		internal ChartHorizontalLine TunnelCenter { get; set; }

		internal void DrawMainLines(Robot robot, double FloorPrice, double CeilingPrice, double CenterPrice)
		{
			TunnelFloor = robot.Chart.DrawHorizontalLine("TunnelFloor", FloorPrice, Color.Magenta, 1, LineStyle.DotsVeryRare);
			TunnelCeiling = robot.Chart.DrawHorizontalLine("TunnelCeiling", CeilingPrice, Color.Magenta, 1, LineStyle.DotsVeryRare);
			TunnelCenter = robot.Chart.DrawHorizontalLine("TunnelCenter", CenterPrice, Color.LimeGreen, 1, LineStyle.DotsVeryRare);
		}

		internal void RemoveAll(Robot robot)
		{
			robot.Chart.RemoveObject("TunnelFloor");
			robot.Chart.RemoveObject("TunnelCeiling");
			robot.Chart.RemoveObject("TunnelCenter");
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
