using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using Rdz.cTrader.Library;
using static System.Net.Mime.MediaTypeNames;


namespace Rdz.cBot
{
    public partial class Pure3MA
    {
        #region Enums
        public enum enState
        {
            None = 0, //Normal no trade
            TradeEntryDetected = 1, //Stage 1: all bars outside strong trends, last bar passing fast MA
            ClosedInside = 2, //Stage 2: last bar closed inside between fast & slow MA
            SetForTrade = 10, //Stage 3: last bar closed outside fast MA
            Running = 20,
        }
        public enum enDirection
        {
            Neutral = 0,
            Bullish = 1,
            Bearish = 2,
        }
        #endregion

        #region Functions
        private void Reset(string text = null)
        {
            State = enState.None;
            StopLossPrice = double.NaN;
            GuidKey = String.Empty;

            if (!string.IsNullOrEmpty(text))
            {
                Print(text);
            }
        }

        private void ShowInfo(string text = null)
        {
            if (!string.IsNullOrEmpty(text))
            {
                Chart.DrawText(InfoLabel, text, Bars.OpenTimes.Last(30), SlowMA.Result.Last(1), Color.White);
            }
            else
            {
                Chart.RemoveObject(InfoLabel);
            }
            
        }
        private void Execute()
        {
            if (State == enState.SetForTrade && !double.IsNaN(StopLossPrice))
            {
                var LastBar = Bars.Last(1);
                string _GuidKey = Guid.NewGuid().ToString().Substring(9, 4);
                double StopLossPips = this.DistanceInPips(StopLossPrice, LastBar.Close, true) + StopLossMarginPips;
                double TakeProfitPips = StopLossPips * RiskRewardRatio;

                Print("Key:{0}, SL:{1}, TP:{2}", _GuidKey, StopLossPips, TakeProfitPips);

                bool MinStopLossCriteria = !UseMinStopLossPips || (UseMinStopLossPips && StopLossPips >= MinStopLossPips);

                if (MinStopLossCriteria)
                {
                    if (Direction == enDirection.Bearish)
                    {
                        this.ExecuteMarketOrderAsync(TradeType.Sell, Symbol.Name, this.LotToVolume(TradeLotSize), _GuidKey, StopLossPips, TakeProfitPips);
                        State = enState.Running;
                        GuidKey = _GuidKey;
                    }
                    else if (Direction == enDirection.Bullish)
                    {
                        this.ExecuteMarketOrderAsync(TradeType.Buy, Symbol.Name, this.LotToVolume(TradeLotSize), _GuidKey, StopLossPips, TakeProfitPips);
                        State = enState.Running;
                        GuidKey = _GuidKey;
                    }
                }
                else
                {
                    Reset(String.Format("SL:{0} is too low.", StopLossPips));
                }

            }
            else if (State == enState.Running)
            {
                var IsActive = Positions.Where(x => x.Label == GuidKey).Count() > 0;
                if (!IsActive)
                {
                    Reset();
                }

            }
        }

        private void OnBarDetection()
        {
            var DPSlowMA = SlowMA.Result.TakeLast(EntryDetectionPeriod + 1).Take(EntryDetectionPeriod);
            var DPMediumMA = MediumMA.Result.TakeLast(EntryDetectionPeriod + 1).Take(EntryDetectionPeriod);
            var DPFastMA = FastMA.Result.TakeLast(EntryDetectionPeriod + 1).Take(EntryDetectionPeriod);

            var DPBars = Bars.TakeLast(EntryDetectionPeriod + 1).Take(EntryDetectionPeriod);
            //combining all
            var AllDP = DPSlowMA.Zip(DPMediumMA, DPFastMA).Select(x => new { Slow = x.First, Medium = x.Second, Fast = x.Third }).Zip(DPBars).Select(x => new { MA = x.First, Bar = x.Second });
            var LastDP = AllDP.Last();

            Direction = (LastDP.MA.Fast < LastDP.MA.Medium && LastDP.MA.Medium < LastDP.MA.Slow ? enDirection.Bearish : (LastDP.MA.Fast > LastDP.MA.Medium && LastDP.MA.Medium > LastDP.MA.Slow ? enDirection.Bullish : enDirection.Neutral));

            bool IsLastCandleClosedIn =
                (Direction == enDirection.Bearish && LastDP.Bar.Close > LastDP.MA.Fast)
                ||
                (Direction == enDirection.Bullish && LastDP.Bar.Close < LastDP.MA.Fast);


            if (State == enState.None)
            {
                var DPDetection = AllDP.Take(EntryDetectionPeriod - 1);

                //IsCleanDirection = all MA fanning out properly, and all candles faster than Fast MA (except last candle)
                bool IsCleanDirection = DPDetection.All(x =>
                    (Direction == enDirection.Bearish && x.MA.Fast < x.MA.Medium && x.MA.Medium < x.MA.Slow && x.Bar.High < x.MA.Fast)
                    ||
                    (Direction == enDirection.Bullish && x.MA.Fast > x.MA.Medium && x.MA.Medium > x.MA.Slow && x.Bar.Low > x.MA.Fast)
                );

                //IsLastCandleIn = the last candle open from outside, close inside between Fast and Slow, high/low passes fast
                bool IsLastCandleIn =
                    (Direction == enDirection.Bearish && LastDP.Bar.High > LastDP.MA.Fast && LastDP.Bar.Close < LastDP.MA.Slow)
                    ||
                    (Direction == enDirection.Bullish && LastDP.Bar.Low < LastDP.MA.Fast && LastDP.Bar.Close > LastDP.MA.Slow);


                if (IsCleanDirection && IsLastCandleIn && IsLastCandleClosedIn)
                {
                    State = enState.ClosedInside;
                }
                else if (IsCleanDirection && IsLastCandleIn)
                {
                    State = enState.TradeEntryDetected;
                }
            }
            else if (State == enState.TradeEntryDetected)
            {
                //need to handle what if TradeEntryDetected but no candle closed inside
                bool IsLastCandleClosedOutside =
                    (Direction == enDirection.Bearish && LastDP.Bar.High < LastDP.MA.Fast)
                    ||
                    (Direction == enDirection.Bullish && LastDP.Bar.Low > LastDP.MA.Fast);

                if (IsLastCandleClosedIn)
                {
                    State = enState.ClosedInside;
                }
                else if (IsLastCandleClosedOutside)
                {
                    Reset("Last Candle closed outside.");
                }
            }
            else if (State == enState.ClosedInside)
            {
                //need to handle what if ClosedInside but closed passed Slow MA (trend reversal)
                bool IsTrendReversalSign =
                    (Direction == enDirection.Bearish && LastDP.Bar.Close > LastDP.MA.Slow)
                    ||
                    (Direction == enDirection.Bullish && LastDP.Bar.Close < LastDP.MA.Slow);

                bool IsLastCandleClosedOutside =
                    (Direction == enDirection.Bearish && LastDP.Bar.Close < LastDP.MA.Fast && LastDP.Bar.Open > LastDP.MA.Fast)
                    ||
                    (Direction == enDirection.Bullish && LastDP.Bar.Close > LastDP.MA.Fast && LastDP.Bar.Open < LastDP.MA.Fast);
                if (IsLastCandleClosedOutside)
                {
                    State = enState.SetForTrade;

                    var AllDPInside = AllDP.Where(x =>
                        (Direction == enDirection.Bearish && x.Bar.High > x.MA.Fast)
                        ||
                        (Direction == enDirection.Bullish && x.Bar.Low < x.MA.Fast)
                    );

                    StopLossPrice = (Direction == enDirection.Bearish ? AllDPInside.Select(x => x.Bar.High).Max() : (Direction == enDirection.Bullish ? AllDPInside.Select(x => x.Bar.Low).Min() : double.NaN));
                }
                else if (IsTrendReversalSign)
                {
                    Reset("Trend reversal sign.");
                }
            }
            Print("St: {0}, D: {1}, SL: {2}, F: {3:#,##0.00000}, M: {4:#,##0.00000}, S: {5:#,##0.00000}, T: {6}", State, Direction, StopLossPrice, LastDP.MA.Fast, LastDP.MA.Medium, LastDP.MA.Slow, LastDP.Bar.OpenTime);
        }
        #endregion
    }
}
