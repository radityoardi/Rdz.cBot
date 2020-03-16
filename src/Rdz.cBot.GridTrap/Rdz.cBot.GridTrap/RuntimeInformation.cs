using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

using Rdz.cBot.GridTrap.Extensions;
using Rdz.cBot.GridTrap.Schema;

namespace Rdz.cBot.GridTrap
{
    internal class RuntimeInformation
    {
        #region Classes
        internal class Grid
        {
            public Grid()
            {
                Side = GridSide.Undefined;
            }
            internal double EstimatedPricing { get; set; }
            internal double ActualPricing { get; set; }
            internal GridSide Side { get; set; }
            internal OrderType OrderType { get; set; }
            internal double LotSize { get; set; }
            internal TradeStatus Status { get; set; }
            internal int SideIndex { get; set; }
            internal Position RobotPosition { get; set; }
            internal PendingOrder RobotPendingOrder { get; set; }
            internal void PlaceOrder(GridTrapBot robot)
            {
                TradeType tradeType = TradeType.Buy;
                if ((OrderType == OrderType.LIMIT && Side == GridSide.UpperGround) || (OrderType == OrderType.STOP && Side == GridSide.UnderGround))
                {
                    tradeType = TradeType.Sell;
                }

                if (OrderType == OrderType.LIMIT)
                {
                    robot.PlaceLimitOrderAsync(tradeType, robot.Symbol.Name, LotSize, EstimatedPricing, Side.ToString() + "-" + SideIndex.ToString(), PendingOrderPlaced);
                }
                else if (OrderType == OrderType.STOP)
                {
                    robot.PlaceStopOrderAsync(tradeType, robot.Symbol.Name, LotSize, EstimatedPricing, Side.ToString() + "-" + SideIndex.ToString(), PendingOrderPlaced);
                }
            }
            internal void Reset()
            {
                RobotPosition = null;
                RobotPendingOrder = null;
                Status = TradeStatus.Inactive;
            }
            internal void WhenOrderFilled(Position robotPosition)
            {
                RobotPosition = robotPosition;
                RobotPendingOrder = null;
                Status = TradeStatus.Active;
            }
            internal void PendingOrderPlaced(TradeResult tradeResult)
            {
                if (tradeResult.IsSuccessful)
                {
                    RobotPendingOrder = tradeResult.PendingOrder;
                    Status = TradeStatus.Pending;
                }
            }
            [Obsolete()]
            internal void PositionClosed(TradeResult tradeResult)
            {
                if (tradeResult.IsSuccessful)
                {
                    RobotPosition = null;
                    RobotPendingOrder = null;
                    Status = TradeStatus.Inactive;
                }
            }
        }
        #endregion


        public RuntimeInformation(Configuration config, GridTrapBot robot)
        {
            this.c = config;
            this.robot = robot;
            Status = RuntimeStatus.Inactive;
            Grids = new List<Grid>();
        }

        public void Reset()
        {
            Grids.Clear();
            OriginalAsk = OriginalBid = UpperGroundStartingPoint = UnderGroundStartingPoint = TotalNetProfit = GridSize = RunningCycle = 0;
            Active = false;
        }

        internal GridTrapBot robot { get; set; }
        internal Configuration c { get; set; }

        //internal EntryMode EntryMode { get; set; }

        internal RuntimeStatus Status { get; set; }
        internal List<Grid> Grids { get; set; }

        internal double OriginalAsk { get; set; }
        internal double OriginalBid { get; set; }

        internal double UpperGroundStartingPoint { get; private set; }
        internal double UnderGroundStartingPoint { get; private set; }

        internal int GridSize { get; private set; }

        internal bool Active { get; private set; }
		internal int RunningCycle { get; private set; }
		internal DateTime ActiveSince { get; private set; }
        internal double TotalNetProfit { get; private set; }

		internal void InitializeCycle()
		{
			RunningCycle += 1;
		}
		internal void ResetCycle()
		{
			RunningCycle = 0;
		}

        internal void InitializeGrids()
        {
            double LastUpperEstimatedPricing = 0;
            double LastUnderEstimatedPricing = 0;
            if (c == null) return;
            double lotSize = (double)c.GridParameters.LotSize;

            OriginalAsk = robot.Symbol.Ask;
            OriginalBid = robot.Symbol.Bid;

            Grids.Clear();
			ActiveSince = robot.Time;

            UpperGroundStartingPoint = robot.ShiftPrice(OriginalAsk, (int)c.GridParameters.Intervals.Starting);
            UnderGroundStartingPoint = robot.ShiftPrice(OriginalBid, -Math.Abs((int)c.GridParameters.Intervals.Starting));

            GridSize = Math.Abs((int)c.GridParameters.Size);

            for (int i = 0; i < GridSize; i++)
            {
                LastUpperEstimatedPricing = i == 0 ? UpperGroundStartingPoint : robot.ShiftPrice(LastUpperEstimatedPricing, Math.Abs((int)c.GridParameters.Intervals.Grid));
                //robot.Print("EstimatedPricing-Upper: {0} at {1}", LastUpperEstimatedPricing.ToString(), i.ToString());
                var UpperGrid = new Grid()
                {
                    Side = GridSide.UpperGround,
                    OrderType = c.GridParameters.OrderType,
                    LotSize = lotSize,
                    EstimatedPricing = LastUpperEstimatedPricing,
                    SideIndex = i
                };
                Grids.Add(UpperGrid);

                LastUnderEstimatedPricing = i == 0 ? UnderGroundStartingPoint : robot.ShiftPrice(LastUnderEstimatedPricing, -Math.Abs((int)c.GridParameters.Intervals.Grid));
                //robot.Print("EstimatedPricing-Under: {0} at {1}", LastUnderEstimatedPricing.ToString(), i.ToString());
                var UnderGrid = new Grid()
                {
                    Side = GridSide.UnderGround,
                    OrderType = c.GridParameters.OrderType,
                    LotSize = lotSize,
                    EstimatedPricing = LastUnderEstimatedPricing,
                    SideIndex = i
                };
                Grids.Add(UnderGrid);

            }
        }

        internal void PlaceGridOrders()
        {
            for (int i = 0; i < Grids.Count; i++)
            {
                //robot.Print("Executing: {0}({2}-{3}) at {1}", Grids[i].EstimatedPricing.ToString(), Grids[i].SideIndex.ToString(), Grids[i].OrderType.ToString(), Grids[i].Side.ToString());
                Grids[i].PlaceOrder(robot);
                Active = true;
            }
        }
		internal void EnsureAllClosed()
		{
			if (Active && robot.PendingOrders.Count + robot.Positions.Count < GridSize)
			{
				CloseCycle();
			}
		}
        internal void AnalyzeConditionalClosure()
        {
            TotalNetProfit = Grids.Where(x => x.Status == TradeStatus.Active).Select(x => x.RobotPosition.NetProfit).Sum();
			ClosureMode closureMode = c.ClosureParameters.ClosureMode;

            if (closureMode == ClosureMode.Fixed)
            {
                var ClosureCondition1 = TotalNetProfit >= robot.ClosureFixedTP.FallbackIfZero(c.ClosureParameters.Fixed.TakeProfit);
                var ClosureCondition2 = (new[] {
                        Grids.Where(x => x.Status == TradeStatus.Active && x.Side == GridSide.UpperGround).Count(),
                        Grids.Where(x => x.Status == TradeStatus.Active && x.Side == GridSide.UnderGround).Count()
                    }).All(x => x == GridSize) && c.GridParameters.OrderType == OrderType.STOP;
				var IsPassingMaxDuration = c.ClosureParameters.FallbackClosureMode == FallbackClosureMode.MaxDuration && robot.Time.ToLocalTime() > ActiveSince.ToLocalTime().Add(c.ClosureParameters.MaxDurationSpan);

                if (ClosureCondition1 || ClosureCondition2 || IsPassingMaxDuration)
                {
					CloseCycle();
                }
            }
        }
		internal void CloseCycle()
		{
			//close all positions
			Grids.Where(x => x.Status == TradeStatus.Active).OrderBy(x => x.RobotPosition.NetProfit).All(x =>
			{
				robot.ClosePositionAsync(x.RobotPosition, PositionClosed);
				return true;
			});
			//close all pending orders
			Grids.Where(x => x.Status == TradeStatus.Pending).All(x =>
			{
				robot.CancelPendingOrderAsync(x.RobotPendingOrder, PendingOrderCancelled);
				return true;
			});
		}
		internal void PendingOrderCancelled(TradeResult tradeResult)
        {
            if (tradeResult.IsSuccessful)
            {
                Grids.Where(x => x.Status == TradeStatus.Pending && x.RobotPendingOrder.Id == tradeResult.PendingOrder.Id).All(x => {
                    x.Reset();
                    return true;
                });
                if (Grids.All(x => x.Status == TradeStatus.Inactive))
                {
                    Reset();
                }
            }
        }
        internal void PositionClosed(TradeResult tradeResult)
        {
            if (tradeResult.IsSuccessful)
            {
                Grids.Where(x => x.Status == TradeStatus.Active && x.RobotPosition.Id == tradeResult.Position.Id).All(x => {
                    x.Reset();
                    return true;
                });
                if (Grids.All(x => x.Status == TradeStatus.Inactive))
                {
                    Reset();
                }
            }
        }

    }
}
