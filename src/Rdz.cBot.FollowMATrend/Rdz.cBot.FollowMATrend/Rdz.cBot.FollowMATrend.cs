using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using Rdz.cBot.Library;
using Rdz.cBot.Library.Chart;
using Rdz.cBot.Library.Extensions;

namespace Rdz.cBot
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class FollowMATrendcBot : RdzRobot, IRdzRobot
    {
        private const string StandardGroupName = "Standard";
        [Parameter("Configuration Path", Group = StandardGroupName, DefaultValue = @"%USERPROFILE%\Documents\Rdz.cBot.BTSOBot\configuration.json")]
        public override string ConfigurationFilePath { get; set; }

        [Parameter("Auto-refresh", Group = StandardGroupName, DefaultValue = false)]
        public override bool AutoRefreshConfiguration { get; set; }

        private const string DetectionGroupName = "Detection";
        [Parameter("Period", Group = DetectionGroupName, MinValue = 1, MaxValue = 100, DefaultValue = 6)]
        public int Period { get; set; }

        [Parameter("Enable MA Distance Threshold", Group = DetectionGroupName, DefaultValue = true)]
        public bool EnableMADistanceThreshold { get; set; }

        [Parameter("MA Distance Threshold (Pips)", Group = DetectionGroupName, DefaultValue = 20)]
        public int MADistanceThreshold { get; set; }

        [Parameter("Enable 'All Except Last Sentiments Green'", Group = DetectionGroupName, DefaultValue = true)]
        public bool EnableAllExceptLastSentimentsGreen { get; set; }

        [Parameter("Enable 'Last Sentiment Red'", Group = DetectionGroupName, DefaultValue = true)]
        public bool EnableLastSentimentRed { get; set; }

        [Parameter("Enable Minimum Highest-Lowest Distance", Group = DetectionGroupName, DefaultValue = false)]
        public bool EnableMinHighestLowestDistance { get; set; }

        [Parameter("MA Distance Threshold (Pips)", Group = DetectionGroupName, DefaultValue = 20)]
        public int HighestLowestThreshold { get; set; }

        private const string VisualGroupName = "Visual";
        [Parameter("Pin Sentiment Distance (Pips)", Group = VisualGroupName, DefaultValue = 20)]
        public int PinSentimentDistance { get; set; }

        [Parameter("Pin Decision Distance (Pips)", Group = VisualGroupName, DefaultValue = 10)]
        public int PinDecisionDistance { get; set; }

        [Parameter("High Low Line Type", Group = VisualGroupName, DefaultValue = enHighLowLineType.Margin)]
        public enHighLowLineType HighLowLineType { get; set; }

        private const string TradeGroupName = "Trade";
        [Parameter("Volume", Group = TradeGroupName, MinValue = 1000, Step = 1000, DefaultValue = 1000)]
        public int Volume { get; set; }

        [Parameter("Aggressive", Group = TradeGroupName, DefaultValue = false)]
        public bool Aggressive { get; set; }

        [Parameter("Line Margin (Pips)", Group = TradeGroupName, DefaultValue = 3)]
        public int Margin { get; set; }

        [Parameter("Close Positions on MA Crossed", Group = TradeGroupName, DefaultValue = false)]
        public bool EnableClosePositionsWhenMACrossed { get; set; }

        [Parameter("Close PendingOrders on MA Crossed", Group = TradeGroupName, DefaultValue = false)]
        public bool EnableClosePendingOrdersWhenMACrossed { get; set; }

        #region Input: Moving Average
        private const string MAGroupName = "Moving Average";

        [Parameter("Source", Group = MAGroupName)]
        public DataSeries MASource { get; set; }

        [Parameter("Type", Group = MAGroupName, DefaultValue = MovingAverageType.Exponential)]
        public MovingAverageType MAType { get; set; }

        [Parameter("Periods", Group = MAGroupName, DefaultValue = 21, MinValue = 1)]
        public int MAPeriods { get; set; }
        #endregion

        #region Input: Bollinger Bands
        private const string BBGroupName = "Exponential Bollinger Bands";

        [Parameter("Source", Group = BBGroupName)]
        public DataSeries BBSource { get; set; }

        [Parameter("Type", Group = BBGroupName, DefaultValue = MovingAverageType.Exponential)]
        public MovingAverageType BBType { get; set; }

        [Parameter("Periods", Group = BBGroupName, DefaultValue = 55, MinValue = 1)]
        public int BBPeriods { get; set; }

        [Parameter("Standard Deviation", Group = BBGroupName, DefaultValue = 2, MinValue = 0)]
        public int BBStandardDev { get; set; }
        #endregion

        private const string OrderLabel = "FollowMATrend";

        private MovingAverage ma { get; set; }
        private BollingerBands bb { get; set; }

        private List<Candlestick> LastClosedCandlesticks { get; set; }

        private const string firstAnchorName = "firstAnchor";
        private ChartIcon firstAnchor { get; set; }
        private const string lastAnchorName = "lastAnchor";
        private ChartIcon lastAnchor { get; set; }
        private const string highestHighLineName = "highestHighLine";
        private ChartTrendLine highestHighLine { get; set; }
        private const string lowestLowLineName = "lowestLowLine";
        private ChartTrendLine lowestLowLine { get; set; }
        private const string candleTrendDetectorName = "candleTrendDetector";

        private const string lastSentimentName = "lastSentiment";
        private ChartIcon lastSentiment { get; set; }

        private const string maDistanceLightName = "maDistanceLight";
        private const string maDistanceTextName = "maDistanceText";
        private const string maDistanceTextValue = "MA Distance Threshold";
        private ChartIcon maDistanceLight { get; set; }
        private ChartText maDistanceText { get; set; }

        private const string allSentimentsGreenLightName = "allSentimentsGreenLight";
        private const string allSentimentsGreenTextName = "allSentimentsGreenText";
        private const string allSentimentsGreenTextValue = "All except last: Green";
        private ChartIcon allSentimentsGreenLight { get; set; }
        private ChartText allSentimentsGreenText { get; set; }

        private const string lastSentimentRedLightName = "lastSentimentRedLight";
        private const string lastSentimentRedTextName = "lastSentimentRedText";
        private const string lastSentimentRedTextValue = "Last: Red";
        private ChartIcon lastSentimentRedLight { get; set; }
        private ChartText lastSentimentRedText { get; set; }

        private IEnumerable<Position> FollowMATrendPositions
        {
            get
            {
                return Positions.Where(x => x.Label.StartsWith(OrderLabel));
            }
        }
        private IEnumerable<PendingOrder> FollowMATrendPendingOrders
        {
            get
            {
                return PendingOrders.Where(x => x.Label.StartsWith(OrderLabel));
            }
        }

        protected override void OnStart()
        {
            PendingOrders.Filled += PendingOrders_Filled;
            Positions.Closed += Positions_Closed;
            ma = Indicators.MovingAverage(MASource, MAPeriods, MAType);
            bb = Indicators.BollingerBands(BBSource, BBPeriods, BBStandardDev, BBType);
        }

        private void Positions_Closed(PositionClosedEventArgs obj)
        {
            if (obj.Position.Label == string.Concat(OrderLabel, "_A") && FollowMATrendPositions.Count() == 1)
            {
                var firstPos = FollowMATrendPositions.First();
                firstPos.ModifyStopLossPrice(firstPos.EntryPrice);
            }
        }

        private void PendingOrders_Filled(PendingOrderFilledEventArgs obj)
        {
        }

        protected override void OnTick()
        {
            //Will not be used
        }

        private IEnumerable<MATrendIndicator> MATrendIndicators
        {
            get
            {
                return
                    from fasterMA in ma.Result.Reverse().Skip(1).Take(Period).Reverse().Select((item, index) => new { item, index })
                    from slowerMA in bb.Main.Reverse().Skip(1).Take(Period).Reverse().Select((item, index) => new { item, index })
                    from candlestick in this.GetMarketSeries(1, Period).Reverse<Candlestick>().Select((item, index) => new { item, index })
                    where fasterMA.index == slowerMA.index && slowerMA.index == candlestick.index
                    select new MATrendIndicator
                    {
                        Index = candlestick.index,
                        FasterMA = fasterMA.item,
                        SlowerMA = slowerMA.item,
                        Candlestick = candlestick.item,
                        Sentiment = candlestick.item.IdentifySentiment(fasterMA.item),
                        Location = candlestick.item.IdentifyLocation(fasterMA.item),
                        FasterSlowerMADistance = this.DistanceInPips(fasterMA.item, slowerMA.item)
                    };
            }
        }

        protected override void OnBar()
        {
            List<MATrendIndicator> trendindi = MATrendIndicators.ToList();

            int LastMADistance = trendindi.Last().FasterSlowerMADistance;
            enSentiment LastMASentiment = LastMADistance.IsPositive() && LastMADistance > MADistanceThreshold ? enSentiment.Bullish : LastMADistance.IsNegative() && LastMADistance < -MADistanceThreshold ? enSentiment.Bearish : enSentiment.Neutral;

            trendindi.ForEach(x =>
            {
                x.GoodSentiment = (LastMASentiment == enSentiment.Bullish && x.Sentiment == enSentiment.Bullish) || (LastMASentiment == enSentiment.Bearish && x.Sentiment == enSentiment.Bearish);
                x.GoodLocation = (LastMASentiment == enSentiment.Bullish && x.Location == enLocation.Above) || (LastMASentiment == enSentiment.Bearish && x.Location == enLocation.Below);
            });

            Candlestick LowestLowCandlestick = trendindi.OrderBy(x => x.Candlestick.Low).Select(x => x.Candlestick).First();
            Candlestick HighestHighCandlestick = trendindi.OrderByDescending(x => x.Candlestick.High).Select(x => x.Candlestick).First();

            bool IsMADistanceAboveThreshold = (LastMADistance.IsPositive() && LastMADistance > MADistanceThreshold) || (LastMADistance.IsNegative() && LastMADistance < -MADistanceThreshold);

            bool AllLocationsGoodExceptLast = trendindi.Take(trendindi.Count - 1).All(x => x.GoodLocation);
            bool LastLocationRed = !trendindi.Last().GoodLocation;

            int HighestLowestDistance = this.DistanceInPips(HighestHighCandlestick.High, LowestLowCandlestick.Low);
            double HighestHighMargin = this.ShiftPriceInPips(HighestHighCandlestick.High, Margin);
            double LowestLowMargin = this.ShiftPriceInPips(LowestLowCandlestick.Low, -Margin);
            int HighestLowestMarginDistance = this.DistanceInPips(HighestHighMargin, LowestLowMargin);

            bool IsHighestLowestMarginDistanceAboveThreshold = HighLowLineType == enHighLowLineType.Actual ? HighestLowestDistance >= HighestLowestThreshold : HighestLowestMarginDistance >= HighestLowestThreshold;

            highestHighLine = Chart.DrawTrendLine(highestHighLineName, trendindi.First().Candlestick.OpenTime, HighLowLineType == enHighLowLineType.Actual ? HighestHighCandlestick.High : HighestHighMargin, trendindi.Last().Candlestick.OpenTime, HighLowLineType == enHighLowLineType.Actual ? HighestHighCandlestick.High : HighestHighMargin, Color.DarkGray);
            lowestLowLine = Chart.DrawTrendLine(lowestLowLineName, trendindi.First().Candlestick.OpenTime, HighLowLineType == enHighLowLineType.Actual ? LowestLowCandlestick.Low : LowestLowMargin, trendindi.Last().Candlestick.OpenTime, HighLowLineType == enHighLowLineType.Actual ? LowestLowCandlestick.Low : LowestLowMargin, Color.DarkGray);

            DateTime indicatorStart = this.GetMarketSeries(Period + 20).OpenTime;
            DateTime textStart = this.GetMarketSeries(Period + 19).OpenTime;

            double anchorPos = HighestHighCandlestick.High;
            if (EnableMADistanceThreshold)
            {
                anchorPos = this.ShiftPriceInPips(anchorPos, -PinDecisionDistance);
                maDistanceLight = Chart.DrawIcon(maDistanceLightName, ChartIconType.Circle, indicatorStart, anchorPos, IsMADistanceAboveThreshold ? Color.LightGreen : Color.IndianRed);
                maDistanceText = Chart.DrawText(maDistanceTextName, maDistanceTextValue, textStart, anchorPos, Color.White);
                maDistanceText.VerticalAlignment = VerticalAlignment.Center;
            }
            if (EnableAllExceptLastSentimentsGreen)
            {
                anchorPos = this.ShiftPriceInPips(anchorPos, -PinDecisionDistance);
                allSentimentsGreenLight = Chart.DrawIcon(allSentimentsGreenLightName, ChartIconType.Circle, indicatorStart, anchorPos, AllLocationsGoodExceptLast ? Color.LightGreen : Color.IndianRed);
                allSentimentsGreenText = Chart.DrawText(allSentimentsGreenTextName, allSentimentsGreenTextValue, textStart, anchorPos, Color.White);
                allSentimentsGreenText.VerticalAlignment = VerticalAlignment.Center;
            }
            if (EnableLastSentimentRed)
            {
                anchorPos = this.ShiftPriceInPips(anchorPos, -PinDecisionDistance);
                lastSentimentRedLight = Chart.DrawIcon(lastSentimentRedLightName, ChartIconType.Circle, indicatorStart, anchorPos, LastLocationRed ? Color.LightGreen : Color.IndianRed);
                lastSentimentRedText = Chart.DrawText(lastSentimentRedTextName, lastSentimentRedTextValue, textStart, anchorPos, Color.White);
                lastSentimentRedText.VerticalAlignment = VerticalAlignment.Center;
            }

            anchorPos = this.ShiftPriceInPips(anchorPos, -PinDecisionDistance);
            lastSentiment = Chart.DrawIcon(lastSentimentName, LastMASentiment == enSentiment.Bullish ? ChartIconType.UpArrow : LastMASentiment == enSentiment.Bearish ? ChartIconType.DownArrow : ChartIconType.Square, indicatorStart, anchorPos, LastMASentiment == enSentiment.Bullish ? Color.LightGreen : LastMASentiment == enSentiment.Bearish ? Color.IndianRed : Color.White);

            trendindi.ForEach(x => {
                Chart.DrawIcon(
                    string.Concat(candleTrendDetectorName, (x.Index + 1).ToString("000")),
                    ChartIconType.Diamond,
                    x.Candlestick.OpenTime,
                    x.SlowerMA,
                    x.GoodSentiment ? Color.LightGreen : Color.IndianRed
                    );
            });

            //close positions when MA is crossed
            if (EnableClosePositionsWhenMACrossed)
            {
                bool LastRBCrossSlowMA = trendindi.Last().Candlestick.IsAbove(trendindi.Last().SlowerMA, enCandlestickPart.RealBodyHigh) && trendindi.Last().Candlestick.IsBelow(trendindi.Last().SlowerMA, enCandlestickPart.RealBodyLow);
                if (LastRBCrossSlowMA)
                {
                    FollowMATrendPositions.ToList().ForEach(x => x.Close());
                }
            }
            //close pending orders when MA is crossed
            if (EnableClosePendingOrdersWhenMACrossed)
            {
                bool LastCSCrossSlowMA = trendindi.Last().Candlestick.IsAbove(trendindi.Last().SlowerMA, enCandlestickPart.High) && trendindi.Last().Candlestick.IsBelow(trendindi.Last().SlowerMA, enCandlestickPart.Low);
                if (LastCSCrossSlowMA)
                {
                    FollowMATrendPendingOrders.ToList().ForEach(x => x.Cancel());
                }
            }

            //place where all orders will be created
            if (
                ((EnableMADistanceThreshold && IsMADistanceAboveThreshold) || !EnableMADistanceThreshold) &&
                ((EnableAllExceptLastSentimentsGreen && AllLocationsGoodExceptLast) || !EnableAllExceptLastSentimentsGreen) &&
                ((EnableLastSentimentRed && LastLocationRed) || !EnableLastSentimentRed) &&
                ((EnableMinHighestLowestDistance && IsHighestLowestMarginDistanceAboveThreshold) || !EnableMinHighestLowestDistance) &&
                ((!Aggressive && FollowMATrendPositions.Count() == 0) || Aggressive)
               )
            {
                if (PlaceStopOrder(LastMASentiment == enSentiment.Bullish ? TradeType.Buy : TradeType.Sell, SymbolName, Volume, LastMASentiment == enSentiment.Bullish ? HighestHighMargin : LowestLowMargin, string.Concat(OrderLabel, "_A"), HighestLowestMarginDistance, HighestLowestMarginDistance).IsSuccessful)
                {
                    PlaceStopOrder(LastMASentiment == enSentiment.Bullish ? TradeType.Buy : TradeType.Sell, SymbolName, Volume, LastMASentiment == enSentiment.Bullish ? HighestHighMargin : LowestLowMargin, string.Concat(OrderLabel, "_B"), HighestLowestMarginDistance, HighestLowestMarginDistance * 2);
                }
            }

        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }
    }
}
