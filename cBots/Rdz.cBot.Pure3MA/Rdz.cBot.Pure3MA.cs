using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using Rdz.cTrader.Library;

namespace Rdz.cBot
{
    [Robot(AccessRights = AccessRights.None)]
    public partial class Pure3MA : Robot
    {
        #region Variables
        private enState State { get; set; }
        private enDirection Direction { get; set; }
        private MovingAverage SlowMA { get; set; }
        private MovingAverage MediumMA { get; set; }
        private MovingAverage FastMA { get; set; }
        private double StopLossPrice { get; set; }
        private string GuidKey { get; set; }
        #endregion

        #region Constants
        private const string InfoLabel = "InfoLabel";
        #endregion

        protected override void OnStart()
        {
            SlowMA = Indicators.MovingAverage(MASource, SlowPeriod, MAType);
            MediumMA = Indicators.MovingAverage(MASource, MediumPeriod, MAType);
            FastMA = Indicators.MovingAverage(MASource, FastPeriod, MAType);
            Reset();
        }

        protected override void OnTick()
        {
            Execute();
            ShowInfo($"State: {State}, StopLossPrice: {StopLossPrice}, Direction: {Direction}");
        }

        protected override void OnBar()
        {
            OnBarDetection();
        }

        protected override void OnStop()
        {
            // Handle cBot stop here
        }
    }
}