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
using Rdz.cIndi;

namespace Rdz.cBot
{
    [Robot(AccessRights = AccessRights.None)]
    public partial class HolyGrailGrid : Robot
    {
        private enDirection Direction { get; set; }
        private double UpperBand { get; set; }
        private double LowerBand { get; set; }
        private double SameDirectionLot { get; set; }
        private double CounterDirectionLot { get; set; }
        private bool LastBBDistancesOkay { get; set; }

        private string GuidKey { get; set; }

        private BBDistances bbd { get; set; }

        private const string UpperBandLine = "UpperBandLine";
        private const string LowerBandLine = "LowerBandLine";
        private const string InfoText = "InfoText";

        private void GenerateGuidKey()
        {
            GuidKey = Guid.NewGuid().ToString().Substring(9, 4);
        }
        private void ResetCycle()
        {
            Direction = enDirection.Neutral;
            UpperBand = double.NaN;
            LowerBand = double.NaN;
            SameDirectionLot = double.NaN;
            CounterDirectionLot = double.NaN;
        }

        private void PlotBands()
        {
            UpperBand = Symbol.ShiftPrice(Symbol.Ask, GridInterval);
            LowerBand = Symbol.ShiftPrice(Symbol.Bid, -GridInterval);
            Chart.DrawHorizontalLine(UpperBandLine, UpperBand, Color.Purple);
            Chart.DrawHorizontalLine(LowerBandLine, LowerBand, Color.Purple);
        }

        private void UpdateInfo(string AdditionalText = null)
        {
            string text = $"Direction: {Direction}{AdditionalText}";
            Chart.DrawText(InfoText, text, Bars.OpenTimes.Last(10), Symbol.ShiftPrice(Symbol.Ask, 5), Color.White);
        }

        private void StartCycle()
        {
            if ((UseBBDistances && LastBBDistancesOkay) || !UseBBDistances)
            {
                GenerateGuidKey();
                PlotBands();
                SameDirectionLot = CounterDirectionLot = InitialLotSize;
                this.ExecuteMarketOrderAsync(TradeType.Buy, Symbol.Name, Symbol.LotToVolume(InitialLotSize), GuidKey);
                this.ExecuteMarketOrderAsync(TradeType.Sell, Symbol.Name, Symbol.LotToVolume(InitialLotSize), GuidKey);
            }
        }

        private void UpdateBands()
        {
            PlotBands();
            if (Direction == enDirection.Bullish)
            {
                CounterDirectionLot = CounterDirectionLot * CounterDirectionMultiplier;
                this.ExecuteMarketOrderAsync(TradeType.Buy, Symbol.Name, Symbol.LotToVolume(InitialLotSize), GuidKey);
                this.ExecuteMarketOrderAsync(TradeType.Sell, Symbol.Name, Symbol.LotToVolume(CounterDirectionLot), GuidKey);
            }
            else if (Direction == enDirection.Bearish)
            {
                CounterDirectionLot = CounterDirectionLot * CounterDirectionMultiplier;
                this.ExecuteMarketOrderAsync(TradeType.Buy, Symbol.Name, Symbol.LotToVolume(CounterDirectionLot), GuidKey);
                this.ExecuteMarketOrderAsync(TradeType.Sell, Symbol.Name, Symbol.LotToVolume(InitialLotSize), GuidKey);
            }
        }

        private void CloseStep()
        {
            if (Direction == enDirection.Bullish)
            {
                Position p = this.Positions.OrderByDescending(x => x.EntryPrice).FirstOrDefault();
                if (p != null)
                {
                    p.Close();
                }
            }
            else if (Direction == enDirection.Bearish)
            {
                Position p = this.Positions.OrderBy(x => x.EntryPrice).FirstOrDefault();
                if (p != null)
                {
                    p.Close();
                }
            }
        }

        private void CloseAll()
        {
            foreach (Position p in this.Positions)
            {
                p.Close();
            }
            Chart.RemoveObject(UpperBandLine);
            Chart.RemoveObject(LowerBandLine);
        }

        protected override void OnStart()
        {
            bbd = Indicators.GetIndicator<BBDistances>(BBDistancesSource, BBDistanceMaType, BBDistancesPeriod, 2);
            ResetCycle();
            StartCycle();
        }

        protected override void OnTick()
        {
            double CenterPrice = Symbol.Ask.FindCenterAgainst(Symbol.Bid, Symbol.Digits);
            UpdateInfo();

            if (CenterPrice > UpperBand)
            {
                if (Direction == enDirection.Neutral)
                    Direction = enDirection.Bullish;

                if (Direction == enDirection.Bullish)
                {
                    UpdateBands();
                    CloseStep();
                }
                else if (Direction == enDirection.Bearish)
                {
                    CloseAll();
                    ResetCycle();
                    StartCycle();
                }
            }
            else if (CenterPrice < LowerBand)
            {
                if (Direction == enDirection.Neutral)
                    Direction = enDirection.Bearish;
                if (Direction == enDirection.Bearish)
                {
                    UpdateBands();
                    CloseStep();
                }
                else if (Direction == enDirection.Bullish)
                {
                    CloseAll();
                    ResetCycle();
                    StartCycle();
                }
            }
            else if (double.IsNaN(UpperBand) || double.IsNaN(LowerBand))
            {
                StartCycle();
            }
        }

        protected override void OnBar()
        {
            var TakelastBBD = bbd.Result.TakeLast(BBDistanceReadPeriod + 1).Take(BBDistanceReadPeriod);
            LastBBDistancesOkay = TakelastBBD.All(x => x <= BBDistancesValueBelow);
        }

        protected override void OnStop()
        {
        }
    }
}