using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using Rdz.cBot.Library;
using Rdz.cBot.Library.Chart;
using System.Collections.Generic;
using Rdz.cBot.Library.Extensions;
using System.Runtime.CompilerServices;
using cAlgo.API.Requests;
using System.Reflection.Emit;
using Rdz.cBot.SimpleScalping.Types;

namespace Rdz.cBot
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public partial class SimpleScalpingcBot : RdzRobot, IRdzRobot
    {
        const string gLMA = "Long Moving Average";
        const string gSMA = "Short Moving Average";
        const string gTF = "Time Frames";
        const string gTRD = "Trades";
        const string gDBG = "Debugging Mode";

        public const string T1Label = "-Trade#1";
        public const string T2Label = "-Trade#2";

        const string PendingOrderPriceLabel = "-POP";
        const string StopLossLabel = "-SL";
        const string TakeProfit1Label = "-TP1";
        const string TakeProfit2Label = "-TP2";

        [Parameter("Short Time Frame", Group = gTF, DefaultValue = "Minute5")]
        public TimeFrame shortTimeFrame { get; set; }

        [Parameter("Long Time Frame", Group = gTF, DefaultValue = "Hour")]
        public TimeFrame longTimeFrame { get; set; }

        [Parameter("Long MA Threshold (fast-slow)", Group = gLMA, MinValue = 0, DefaultValue = 60)]
        public int LongMAThreshold { get; set; }
        [Parameter("Long MA (fast) Period", Group = gLMA, MinValue = 0, DefaultValue = 8)]
        public int LongMAFastPeriod { get; set; }
        [Parameter("Long MA (slow) Period", Group = gLMA, MinValue = 0, DefaultValue = 21)]
        public int LongMASlowPeriod { get; set; }
        [Parameter("Short MA Threshold (fast-medium)", Group = gSMA, MinValue = 0, DefaultValue = 20)]
        public int ShortMAThreshold1 { get; set; }
        [Parameter("Short MA Threshold (medium-slow)", Group = gSMA, MinValue = 0, DefaultValue = 20)]
        public int ShortMAThreshold2 { get; set; }
        [Parameter("Short MA (fast) Period", Group = gSMA, MinValue = 0, DefaultValue = 8)]
        public int ShortMAFastPeriod { get; set; }
        [Parameter("Short MA (medium) Period", Group = gSMA, MinValue = 0, DefaultValue = 13)]
        public int ShortMAMediumPeriod { get; set; }
        [Parameter("Short MA (slow) Period", Group = gSMA, MinValue = 0, DefaultValue = 21)]
        public int ShortMASlowPeriod { get; set; }

        [Parameter("Lot Size", Group = gTRD, MinValue = 0.01, Step = 0.01, DefaultValue = 0.01)]
        public double LotSize { get; set; }
        [Parameter("Buffer in Pips", Group = gTRD, MinValue = 0, Step = 1, DefaultValue = 3)]
        public int BufferInPips { get; set; }
        [Parameter("Label Identifier", Group = gTRD, DefaultValue = "SimpleScalping")]
        public string LabelIdentifier { get; set; }
        [Parameter("Trade 2 Risk Multiplier", Group = gTRD, DefaultValue = 2)]
        public int Trade2Multiplier { get; set; }

        [Parameter("Debugging", Group = gDBG, DefaultValue = false)]
        public bool IsDebug { get; set; }

        private ExponentialMovingAverage shortEMAfast { get; set; }
        private ExponentialMovingAverage shortEMAmedium { get; set; }
        private ExponentialMovingAverage shortEMAslow { get; set; }
        private ExponentialMovingAverage longEMAfast { get; set; }
        private ExponentialMovingAverage longEMAslow { get; set; }
        private Bars longBars { get; set; }
        private Bars shortBars { get; set; }

        private ChartIcon PendingOrderIcon { get; set; }
        private ChartIcon StopLossIcon { get; set; }
        private ChartIcon TakeProfit1Icon { get; set; }
        private ChartIcon TakeProfit2Icon { get; set; }

        [Obsolete()]
        private double HighestBuffer
        {
            get
            {
                return this.ShiftPriceInPips(Highest, BufferInPips);
            }
        }
        [Obsolete()]
        private double Highest { get; set; }
        [Obsolete()]
        private double Lowest { get; set; }
        [Obsolete()]
        private double LowestBuffer
        {
            get
            {
                return this.ShiftPriceInPips(Lowest, -BufferInPips);
            }
        }
        public string FullT1Label
        {
            get
            {
                return LabelIdentifier + T1Label;
            }
        }
        public string FullT2Label
        {
            get
            {
                return LabelIdentifier + T2Label;
            }
        }
        [Obsolete()]
        private int Risk
        {
            get
            {
                return Symbol.DistanceInPips(HighestBuffer, LowestBuffer);
            }
        }

        [Obsolete()]
        private enum Status
        {
            Inactive,
            InProgress,
            Pending,
            Active,
            AdvancedActive
        }
        [Obsolete()]
        private Status TradeStatus { get; set; }

        private IEnumerable<Position> SimpleScalpingPositions
        {
            get
            {
                return this.Positions.Where(x => x.Label.StartsWith("SimpleScalping"));
            }
        }

        private List<SimpleScalpingTrade> SimpleScalpingTrades { get; set; }

        protected override void OnStart()
        {
            longBars = MarketData.GetBars(longTimeFrame);
            shortBars = MarketData.GetBars(shortTimeFrame);

            shortEMAfast = Indicators.ExponentialMovingAverage(shortBars.ClosePrices, ShortMAFastPeriod);//green
            shortEMAmedium = Indicators.ExponentialMovingAverage(shortBars.ClosePrices, ShortMAMediumPeriod);//blue
            shortEMAslow = Indicators.ExponentialMovingAverage(shortBars.ClosePrices, ShortMASlowPeriod);//red

            longEMAfast = Indicators.ExponentialMovingAverage(longBars.ClosePrices, LongMAFastPeriod);//green
            longEMAslow = Indicators.ExponentialMovingAverage(longBars.ClosePrices, LongMASlowPeriod);//red

            SimpleScalpingTrades = new List<SimpleScalpingTrade>();

            Positions.Closed += Positions_Closed;
            PendingOrders.Filled += PendingOrders_Filled;

        }

        private void PendingOrders_Filled(PendingOrderFilledEventArgs obj)
        {
            var sst = SimpleScalpingTrades.Where(x => x.T1Label == obj.PendingOrder.Label || x.T2Label == obj.PendingOrder.Label).FirstOrDefault();
            if (sst != null && sst.Status == SimpleScalpingTrade.State.Pending)
            {
                sst.Status = SimpleScalpingTrade.State.Active;
            }
        }

        private void Positions_Closed(PositionClosedEventArgs obj)
        {
            var sst = SimpleScalpingTrades.Where(x => x.T1Label == obj.Position.Label || x.T2Label == obj.Position.Label).FirstOrDefault();
            if (sst != null)
            {
                if (sst.T1Label == obj.Position.Label && obj.Position.GrossProfit.IsPositive())
                {
                    sst.Status = SimpleScalpingTrade.State.AdvancedActive;

                    var pos2 = SimpleScalpingPositions.Where(x => x.Label == sst.T2Label).FirstOrDefault();
                    if (pos2 != null)
                    {
                        if (pos2.TradeType == TradeType.Buy)
                        {
                            pos2.ModifyStopLossPrice(Symbol.ShiftPriceInPips(sst.HighestBuffer, -sst.Risk + BufferInPips));
                        }
                        else
                        {
                            pos2.ModifyStopLossPrice(Symbol.ShiftPriceInPips(sst.LowestBuffer, sst.Risk - BufferInPips));
                        }
                    }
                }
                else if (sst.T2Label == obj.Position.Label)
                {
                    sst.Status = SimpleScalpingTrade.State.Finished;
                }
            }
        }

        protected override void OnTick()
        {
            // Put your core logic here
        }

        protected override void OnBar()
        {
            //Clear all finished sst
            if (SimpleScalpingTrades.Count > 0)
                SimpleScalpingTrades.RemoveAll(x => x.Status == SimpleScalpingTrade.State.Finished);
            if (IsDebug)
            {
                Print("Remaining SSTs: {0}", SimpleScalpingTrades.Count);
            }

            var longCandlesticks = longBars.GetMarketSeries(Symbol, 1, 2);
            var shortCandlesticks = shortBars.GetMarketSeries(Symbol, 1, 6);

            List<TradeType?> SmallCriterias = new List<TradeType?>();
            List<TradeType?> MainCriterias = new List<TradeType?>();

            var shortEMAfastResult = shortEMAfast.Result.Reverse().ToList();
            var shortEMAmediumResult = shortEMAmedium.Result.Reverse().ToList();
            var shortEMAslowResult = shortEMAslow.Result.Reverse().ToList();

            var longEMAfastResult = longEMAfast.Result.Reverse().ToList();
            var longEMAslowResult = longEMAslow.Result.Reverse().ToList();


            var sst = new SimpleScalpingTrade(this);

            #region long TF
            for (int i = 1; i <= 2; i++)
            {
                SmallCriterias.Clear();

                //C1: fan out nicely & C2: above/below fast EMA.
                var longEMADistance = this.Distance(longEMAfastResult[i], longEMAslowResult[i]);
                if (longEMADistance.IsBelow(-LongMAThreshold))
                {
                    SmallCriterias.Add(TradeType.Sell);

                    /*
                    */
                    if (longCandlesticks[i - 1].RealBodyHigh.IsBelow(longEMAfastResult[i]))
                    {
                        SmallCriterias.Add(TradeType.Sell);
                    }
                    else
                        SmallCriterias.Add(null);
                }
                else if (longEMADistance.IsAbove(LongMAThreshold))
                {
                    SmallCriterias.Add(TradeType.Buy);

                    /*
                    */
                    if (longCandlesticks[i - 1].RealBodyLow.IsAbove(longEMAfastResult[i]))
                    {
                        SmallCriterias.Add(TradeType.Buy);
                    }
                    else
                        SmallCriterias.Add(null);
                }
                else
                {
                    SmallCriterias.Add(null);
                    /*
                    */
                    SmallCriterias.Add(null);
                }

                if (SmallCriterias.All(x => x.HasValue && x.Value == TradeType.Buy))
                {
                    MainCriterias.Add(TradeType.Buy);
                }
                else if (SmallCriterias.All(x => x.HasValue && x.Value == TradeType.Sell))
                {
                    MainCriterias.Add(TradeType.Sell);
                }
                else
                    MainCriterias.Add(null);
                if (IsDebug)
                {
                    Print("longEMADistance: {0}", longEMADistance);
                    Print("SmallCriterias {0}: {1}", i, string.Join(" | ", SmallCriterias.Select(x => !x.HasValue ? "NULL" : x.Value.ToString()).ToArray()));
                    Print("-------------- LONG BAR --------------");
                }
            }
            #endregion

            #region short TF
            for (int i = 1; i <= 6; i++)
            {
                SmallCriterias.Clear();

                //C3: fan out nicely between fast & medium
                var shortEMADistance1 = this.Distance(shortEMAfastResult[i], shortEMAmediumResult[i]);
                TradeType? shortCriteriaResult1 = null;
                if (shortEMADistance1.IsBelow(-ShortMAThreshold1))
                {
                    shortCriteriaResult1 = TradeType.Sell;
                }
                else if (shortEMADistance1.IsAbove(ShortMAThreshold1))
                {
                    shortCriteriaResult1 = TradeType.Buy;
                }
                else
                    shortCriteriaResult1 = null;
                SmallCriterias.Add(shortCriteriaResult1);

                //C4: fan out nicely between medium & low
                var shortEMADistance2 = this.Distance(shortEMAmediumResult[i], shortEMAslowResult[i]);
                TradeType? shortCriteriaResult2 = null;
                if (shortEMADistance2.IsBelow(-ShortMAThreshold2))
                {
                    shortCriteriaResult2 = TradeType.Sell;
                }
                else if (shortEMADistance2.IsAbove(ShortMAThreshold2))
                {
                    shortCriteriaResult2 = TradeType.Buy;
                }
                else
                    shortCriteriaResult2 = null;
                SmallCriterias.Add(shortCriteriaResult2);

                if (i > 1)
                {
                    //C5:
                    if (shortCriteriaResult1.HasValue && shortCriteriaResult2.HasValue && shortCriteriaResult1.Value == shortCriteriaResult2.Value)
                    {
                        if (shortCriteriaResult1.Value == TradeType.Buy)
                        {
                            if (shortCandlesticks[i - 1].RealBodyLow.IsAbove(shortEMAfastResult[i]))
                            {
                                SmallCriterias.Add(TradeType.Buy);
                            }
                            else
                                SmallCriterias.Add(null);
                        }
                        else if (shortCriteriaResult1.Value == TradeType.Sell)
                        {
                            if (shortCandlesticks[i - 1].RealBodyHigh.IsBelow(shortEMAfastResult[i]))
                            {
                                SmallCriterias.Add(TradeType.Sell);
                            }
                            else
                                SmallCriterias.Add(null);
                        }
                        else
                            SmallCriterias.Add(null);
                    }
                    else
                        SmallCriterias.Add(null);
                }
                else if (i == 1)
                {
                    //C5: trigger bar
                    if (shortCriteriaResult1.HasValue && shortCriteriaResult2.HasValue && shortCriteriaResult1.Value == shortCriteriaResult2.Value)
                    {
                        if (shortCriteriaResult1.Value == TradeType.Buy)
                        {
                            if (shortCandlesticks[i - 1].RealBodyHigh.IsAbove(shortEMAfastResult[i]) && shortEMAfastResult[i].IsBetween(shortCandlesticks[i - 1].RealBodyHigh, shortCandlesticks[i - 1].RealBodyLow))
                            {
                                SmallCriterias.Add(TradeType.Buy);
                            }
                        }
                        else if (shortCriteriaResult1.Value == TradeType.Sell)
                        {
                            if (shortCandlesticks[i - 1].RealBodyLow.IsBelow(shortEMAfastResult[i]) && shortEMAfastResult[i].IsBetween(shortCandlesticks[i - 1].RealBodyHigh, shortCandlesticks[i - 1].RealBodyLow))
                            {
                                SmallCriterias.Add(TradeType.Buy);
                            }
                        }
                        else
                            SmallCriterias.Add(null);
                    }
                    else
                        SmallCriterias.Add(null);
                }

                #region Find Highest & Lowest
                if ((sst.Highest == 0) || (sst.Highest > 0 && sst.Highest < shortCandlesticks[i - 1].High))
                    sst.Highest = shortCandlesticks[i - 1].High;
                if ((sst.Lowest == 0) || (sst.Lowest > 0 && sst.Lowest > shortCandlesticks[i - 1].Low))
                    sst.Lowest = shortCandlesticks[i - 1].Low;
                #endregion

                if (SmallCriterias.All(x => x.HasValue && x.Value == TradeType.Buy))
                {
                    MainCriterias.Add(TradeType.Buy);
                }
                else if (SmallCriterias.All(x => x.HasValue && x.Value == TradeType.Sell))
                {
                    MainCriterias.Add(TradeType.Sell);
                }
                else
                    MainCriterias.Add(null);

                if (IsDebug)
                {
                    Print("shortEMADistance1: {0} | shortEMADistance2: {1}", shortEMADistance1, shortEMADistance2);
                    Print(string.Format("shortEMAfast: {{0:{0}}} | shortEMAmedium: {{1:{0}}} | shortEMAslow: {{2:{0}}}", Symbol.Digits.DigitFormat()), shortEMAfastResult[i], shortEMAmediumResult[i], shortEMAslowResult[i]);
                    Print("Short Candlestick: {0}", shortCandlesticks[i - 1].ToString(Symbol.Digits));
                    Print(string.Format("longEMAfast: {{0:{0}}} | longEMAslow: {{1:{0}}}", Symbol.Digits.DigitFormat()), longEMAfastResult[i], longEMAslowResult[i]);
                    Print("Long Candlestick: {0}", longCandlesticks[i - 1].ToString(Symbol.Digits));
                    Print("SmallCriterias {0}: {1}", i, string.Join(" | ", SmallCriterias.Select(x => !x.HasValue ? "NULL" : x.Value.ToString()).ToArray()));
                    Print("-------------- SHORT BAR --------------");
                }
            }
            #endregion

            
            if (MainCriterias.All(x => x.HasValue && x.Value == TradeType.Buy))
            {
                sst.TradeType = TradeType.Buy;
            }
            else if (MainCriterias.All(x => x.HasValue && x.Value == TradeType.Sell))
            {
                sst.TradeType = TradeType.Sell;
            }

            if (IsDebug)
            {
                Print("MainCriterias: {0}", string.Join(" | ", MainCriterias.Select(x => !x.HasValue ? "NULL" : x.Value.ToString()).ToArray()));
                Print("-------------- WHOLE --------------");
            }

            if (sst.TradeType.HasValue && sst.Highest.IsPositive() && sst.Lowest.IsPositive() && sst.Status == SimpleScalpingTrade.State.Inactive)
            {
                sst.Status = SimpleScalpingTrade.State.InProgress;
                if (IsDebug)
                {
                    Print("Executing order");
                }

                if (sst.TradeType.Value == TradeType.Buy)
                {
                    PendingOrderIcon = Chart.DrawIcon(LabelIdentifier + PendingOrderPriceLabel, ChartIconType.UpTriangle, TimeInUtc, sst.HighestBuffer, Color.Turquoise);
                    StopLossIcon = Chart.DrawIcon(LabelIdentifier + StopLossLabel, ChartIconType.Square, TimeInUtc, sst.LowestBuffer, Color.Red);
                    TakeProfit1Icon = Chart.DrawIcon(LabelIdentifier + TakeProfit1Label, ChartIconType.Star, TimeInUtc, Symbol.ShiftPriceInPips(sst.HighestBuffer, sst.Risk), Color.Green);
                    TakeProfit2Icon = Chart.DrawIcon(LabelIdentifier + TakeProfit2Label, ChartIconType.Star, TimeInUtc, Symbol.ShiftPriceInPips(sst.HighestBuffer, sst.Risk * Trade2Multiplier), Color.Green);
                }
                else
                {
                    PendingOrderIcon = Chart.DrawIcon(LabelIdentifier + PendingOrderPriceLabel, ChartIconType.DownTriangle, TimeInUtc, sst.LowestBuffer, Color.Turquoise);
                    StopLossIcon = Chart.DrawIcon(LabelIdentifier + StopLossLabel, ChartIconType.Square, TimeInUtc, sst.HighestBuffer, Color.Red);
                    TakeProfit1Icon = Chart.DrawIcon(LabelIdentifier + TakeProfit1Label, ChartIconType.Star, TimeInUtc, Symbol.ShiftPriceInPips(sst.HighestBuffer, -sst.Risk), Color.Green);
                    TakeProfit2Icon = Chart.DrawIcon(LabelIdentifier + TakeProfit2Label, ChartIconType.Star, TimeInUtc, Symbol.ShiftPriceInPips(sst.HighestBuffer, -sst.Risk * Trade2Multiplier), Color.Green);
                }

                if (PlaceStopOrder(sst.TradeType.Value, SymbolName, this.LotToVolume(LotSize), sst.TradeType.Value == TradeType.Buy ? sst.HighestBuffer : sst.LowestBuffer, sst.T1Label, sst.Risk, sst.Risk).IsSuccessful)
                {
                    if (PlaceStopOrder(sst.TradeType.Value, SymbolName, this.LotToVolume(LotSize), sst.TradeType.Value == TradeType.Buy ? sst.HighestBuffer : sst.LowestBuffer, sst.T2Label, sst.Risk, sst.Risk * Trade2Multiplier).IsSuccessful)
                    {
                        sst.Status = SimpleScalpingTrade.State.Pending;
                        SimpleScalpingTrades.Add(sst);
                    }
                }
            }
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }
    }
}
