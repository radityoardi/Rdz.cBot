using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using Rdz.cBot.Library.Chart;
using Rdz.cBot.Library.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Rdz.cBot.BandScalping.Extensions
{
    internal class BandScalpingEngine
    {
        internal BandScalpingcBot bsBot { get; private set; }
        internal BollingerBands bb { get; set; }
        internal RelativeStrengthIndex rsi { get; set; }
        internal DirectionalMovementSystem adx { get; set; }



        internal BandScalpingEngine(BandScalpingcBot robot)
        {
            bsBot = robot;

            bb = bsBot.Indicators.BollingerBands(bsBot.Bars.ClosePrices, bsBot.BBPeriods, bsBot.BBStandardDev, MovingAverageType.Exponential);
            rsi = bsBot.Indicators.RelativeStrengthIndex(bsBot.Bars.ClosePrices, bsBot.RSIPeriods);
            adx = bsBot.Indicators.DirectionalMovementSystem(bsBot.DMSPeriod);

            Start();
        }

        internal void Start()
        {
            bsBot.config.QuietZones.ParseAll();
        }

        internal void Tick()
        {
            var CurrentCandle = bsBot.GetMarketSeries();
            //for closing only
            foreach (var p in Positions)
            {
                if ((p.TradeType == TradeType.Buy && IsTimeToCloseBuy(CurrentCandle, p)) || (p.TradeType == TradeType.Sell && IsTimeToCloseSell(CurrentCandle, p)))
                {
                    bsBot.ClosePositionAsync(p);
                }
            }
            /*
            //for closing only
            foreach (var p in Positions)
            {
                if ((p.TradeType == TradeType.Buy && IsTimeToCloseBuy(LastClosed, p)) || (p.TradeType == TradeType.Sell && IsTimeToCloseSell(LastClosed, p)))
                {
                    bsBot.ClosePositionAsync(p);
                }
            }
            */
        }

        internal void Bar()
        {
            //for opening only
            var LastClosed = bsBot.GetLastClosedMarketSeries();
            var FutureZones = bsBot.config.QuietZones.FutureZones(bsBot).ToList();

            if (FutureZones.Count.IsZero() || (FutureZones.Count > 0 && !FutureZones.First().IsInQuietZone))
            {
                if (IsTimeToBuy(LastClosed))
                {
                    bsBot.ExecuteMarketOrderAsync(TradeType.Buy, bsBot.SymbolName, bsBot.Symbol.QuantityToVolumeInUnits(bsBot.LotSize), string.Concat(bsBot.LabelPrefix, "BUY"));
                }

                if (IsTimeToSell(LastClosed))
                {
                    bsBot.ExecuteMarketOrderAsync(TradeType.Sell, bsBot.SymbolName, bsBot.Symbol.QuantityToVolumeInUnits(bsBot.LotSize), string.Concat(bsBot.LabelPrefix, "SELL"));
                }
            }
        }

        internal void Stop()
        {

        }

        internal void PendingOrdersFilled(PendingOrderFilledEventArgs obj)
        {

        }

        internal void PositionsClosed(PositionClosedEventArgs obj)
        {

        }

        internal bool IsTimeToBuy(Candlestick LastClosed)
        {
            bool bRet = (!bsBot.EnableBB || (bsBot.EnableBB && LastClosed.Low < bb.Bottom.Last(1)))
                && (!bsBot.EnableRSI || (bsBot.EnableRSI && rsi.Result.Last(1) < bsBot.RSILowerLevel))
                && (!bsBot.EnableADX || (bsBot.EnableADX && adx.ADX.Last(1) < bsBot.DMSLevel))
                && (bsBot.Aggressive || (!bsBot.Aggressive && Positions.Count() == 0));
            if (bRet)
            {
                bsBot.Print("LastClosed.Low: {0} < BB Bottom: {1}, RSI: {2} < RSILowerLevel: {3}", LastClosed.Low, bb.Bottom.Last(1), rsi.Result.Last(1), bsBot.RSILowerLevel);
            }
            return bRet;
        }

        internal bool IsTimeToSell(Candlestick LastClosed)
        {
            bool bRet = (!bsBot.EnableBB || (bsBot.EnableBB && LastClosed.High > bb.Top.Last(1)))
                && (!bsBot.EnableRSI || (bsBot.EnableRSI && rsi.Result.Last(1) > bsBot.RSIUpperLevel))
                && (!bsBot.EnableADX || (bsBot.EnableADX && adx.ADX.Last(1) < bsBot.DMSLevel))
                && (bsBot.Aggressive || (!bsBot.Aggressive && Positions.Count() == 0));
            if (bRet)
            {
                bsBot.Print("LastClosed.High: {0} > BB Top: {1}, RSI: {2} > RSIUpperLevel: {3}", LastClosed.High, bb.Top.Last(1), rsi.Result.Last(1), bsBot.RSIUpperLevel);
            }
            return bRet;
        }

        internal bool IsTimeToCloseBuy(Candlestick Last, Position p)
        {
            bsBot.Print("LastClosed.RealBodyLow: {0}", Last.RealBodyLow);
            return (Last.High > bb.Top.Last() || rsi.Result.Last() > bsBot.RSIUpperLevel || Last.RealBodyLow > bb.Main.Last() || Last.RealBodyHigh > bb.Main.Last())
                && (bsBot.TimeInUtc - p.EntryTime) > bsBot.MinimumDurationTimeSpan;
        }
        internal bool IsTimeToCloseSell(Candlestick Last, Position p)
        {
            bsBot.Print("LastClosed.RealBodyHigh: {0}", Last.RealBodyHigh);
            return (Last.Low < bb.Bottom.Last() || rsi.Result.Last() < bsBot.RSILowerLevel || Last.RealBodyHigh < bb.Main.Last() || Last.RealBodyLow < bb.Main.Last())
                && (bsBot.TimeInUtc - p.EntryTime) > bsBot.MinimumDurationTimeSpan;
        }

        #region Props
        internal IEnumerable<Position> Positions
        {
            get
            {
                return bsBot.Positions.Where(x => x.Label.StartsWith(bsBot.LabelPrefix));
            }
        }

        internal IEnumerable<PendingOrder> PendingOrders
        {
            get
            {
                return bsBot.PendingOrders.Where(x => x.Label.StartsWith(bsBot.LabelPrefix));
            }
        }
        #endregion
    }
}
