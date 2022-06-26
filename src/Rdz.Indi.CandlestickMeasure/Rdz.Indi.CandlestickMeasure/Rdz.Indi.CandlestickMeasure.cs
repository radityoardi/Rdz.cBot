using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using Rdz.cBot.Library.Extensions;
using Rdz.cBot.Library.Chart;

namespace Rdz.Indi
{
    public enum enDistanceType
    {
        Pips,
        Points
    }

    public enum enDisplayType
    {
        Numbers,
        Percentage
    }

    [Levels(110, 20)]
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None, ScalePrecision = 0)]
    public class CandlestickMeasureIndicator : Indicator
    {
        [Parameter("Distance Display", Group = "Display", DefaultValue = enDistanceType.Pips)]
        public enDistanceType DistanceDisplay { get; set; }

        [Parameter("Display Type", Group = "Display", DefaultValue = enDisplayType.Numbers)]
        public enDisplayType DisplayType { get; set; }

        [Parameter("Minimum Height", Group = "Calculation", DefaultValue = 110)]
        public int MinimumHeight { get; set; }

        [Output("Full Height", LineColor = "#FFFFFF")]
        public IndicatorDataSeries FullHeight { get; set; }

        [Output("Real Body Height", LineColor = "#66A8D8")]
        public IndicatorDataSeries RealBodyHeight { get; set; }

        [Output("Upper Shadow Height", LineColor = "#FE0000")]
        public IndicatorDataSeries UpperShadowHeight { get; set; }

        [Output("Lower Shadow Height", LineColor = "#01AF50")]
        public IndicatorDataSeries LowerShadowHeight { get; set; }

        [Output("Result in Pips", LineColor = "#00FF00", IsHistogram = true, PlotType = PlotType.Histogram, Thickness = 3)]
        public IndicatorDataSeries Result { get; set; }

        protected override void Initialize()
        {
        }

        public override void Calculate(int index)
        {
            Candlestick cs = this.GetMarketSeries(index);
            Candlestick csbef = this.GetMarketSeries(index - 1);

            int _fullHeight = int.MinValue;
            switch (DistanceDisplay)
            {
                case enDistanceType.Pips:
                    _fullHeight = this.DistanceInPips(cs.High, cs.Low);
                    switch (DisplayType)
                    {
                        case enDisplayType.Numbers:
                            FullHeight[index] = _fullHeight;
                            UpperShadowHeight[index] = this.DistanceInPips(cs.High, cs.RealBodyHigh);
                            LowerShadowHeight[index] = this.DistanceInPips(cs.RealBodyLow, cs.Low);
                            RealBodyHeight[index] = this.DistanceInPips(cs.RealBodyHigh, cs.RealBodyLow);
                            int pips = csbef.Direction == cBot.Library.enDirection.Bearish ? this.DistanceInPips(cs.High, csbef.RealBodyLow) : this.DistanceInPips(csbef.RealBodyHigh, cs.Low);
                            Result[index] = RealBodyHeight[index - 1] >= MinimumHeight ? pips : 0;
                            break;
                        case enDisplayType.Percentage:
                            UpperShadowHeight[index] = cs.UpperShadowPercentage * 100;
                            LowerShadowHeight[index] = cs.LowerShadowPercentage * 100;
                            RealBodyHeight[index] = cs.RealBodyPercentage * 100;
                            break;
                    }
                    break;
                case enDistanceType.Points:
                    _fullHeight = this.Distance(cs.High, cs.Low);

                    switch (DisplayType)
                    {
                        case enDisplayType.Numbers:
                            FullHeight[index] = _fullHeight;
                            UpperShadowHeight[index] = this.Distance(cs.High, cs.RealBodyHigh);
                            LowerShadowHeight[index] = this.Distance(cs.RealBodyLow, cs.Low);
                            RealBodyHeight[index] = this.Distance(cs.RealBodyHigh, cs.RealBodyLow);
                            int pips = csbef.Direction == cBot.Library.enDirection.Bearish ? this.Distance(cs.High, csbef.RealBodyLow) : this.Distance(csbef.RealBodyHigh, cs.Low);
                            Result[index] = RealBodyHeight[index - 1] >= MinimumHeight ? pips : 0;
                            break;
                        case enDisplayType.Percentage:
                            UpperShadowHeight[index] = cs.UpperShadowPercentage * 100;
                            LowerShadowHeight[index] = cs.LowerShadowPercentage * 100;
                            RealBodyHeight[index] = cs.RealBodyPercentage * 100;
                            break;
                    }
                    break;
            }
        }
    }
}
