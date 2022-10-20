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


namespace Rdz.cBot
{
    public partial class Pure3MA
    {
        #region Moving Averages
        [Parameter("Moving Average Source", Group = "Moving Averages")]
        public DataSeries MASource { get; set; }

        [Parameter("MA Type", Group = "Moving Averages", DefaultValue = MovingAverageType.Simple)]
        public MovingAverageType MAType { get; set; }

        [Parameter("Slow Period", Group = "Moving Averages", DefaultValue = 100)]
        public int SlowPeriod { get; set; }

        [Parameter("Medium Period", Group = "Moving Averages", DefaultValue = 50)]
        public int MediumPeriod { get; set; }

        [Parameter("Fast Period", Group = "Moving Averages", DefaultValue = 25)]
        public int FastPeriod { get; set; }
        #endregion

        #region Trade Strategy
        [Parameter("Entry Detection Period", Group = "Trade Strategy", DefaultValue = 20)]
        public int EntryDetectionPeriod { get; set; }

        [Parameter("Risk-Reward Ratio", Group = "Trade Strategy", DefaultValue = 1.5)]
        public double RiskRewardRatio { get; set; }

        [Parameter("SL Margin Pips", Group = "Trade Strategy", DefaultValue = 0)]
        public double StopLossMarginPips { get; set; }

        [Parameter("Use Min. SL Pips", Group = "Trade Strategy", DefaultValue = false)]
        public bool UseMinStopLossPips { get; set; }

        [Parameter("Min. SL Pips", Group = "Trade Strategy", DefaultValue = 12)]
        public double MinStopLossPips { get; set; }

        /*
        [Parameter("Use Default SL Pips", Group = "Trade Strategy", DefaultValue = false)]
        public bool UseDefaultStopLossPips { get; set; }

        [Parameter("Default SL Pips", Group = "Trade Strategy", DefaultValue = 12)]
        public double DefaultStopLossPips { get; set; }
        */

        [Parameter("Trade Lot Size", DefaultValue = 0.01)]
        public double TradeLotSize { get; set; }

        #endregion
    }
}
