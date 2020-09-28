using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using System.IO;
using Newtonsoft.Json;
using Rdz.cBot.GridTrap.Extensions;
using Rdz.cBot.GridTrap.Schema;
using Rdz.cBot.Library;
using Rdz.cBot.Library.Extensions;
using System.Collections.Generic;

namespace Rdz.cBot.GridTrap
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class GridTrapBot : RdzRobot
    {
        [Parameter("Auto-create Config Folder", DefaultValue = true)]
        public bool AutoCreateConfigFolder { get; set; }

		[Parameter("Configuration Path", DefaultValue = @"%USERPROFILE%\Documents\Rdz.cBot.GridTraps\configuration.json")]
		public override string ConfigurationFilePath { get; set; }
        [Parameter("TickVolumeThreshold.Max", DefaultValue = 0)]
        public double TickVolumeThresholdMax { get; set; }
        [Parameter("Closure.Fixed.TP", DefaultValue = 0)]
        public double ClosureFixedTP { get; set; }

        #region Configuration

        #region Entry Parameters
        /*
        [Parameter("Entry Mode", Group = "Entry Parameters", DefaultValue = c.EntryParameters.EntryMode.BollingerBandsDistance)]
        public c.EntryParameters.EntryMode c.EntryParameters.EntryMode { get; set; }
        */
        #endregion

        #endregion

        internal Configuration c;
        internal ChartText TextTotalNetProfit;

        private RuntimeInformation RunInfo { get; set; }
		public override bool AutoRefreshConfiguration { get; set; }

		private BollingerBands bb { get; set; }

		protected override void OnStart()
        {

            Print("Reading '{0}'", ExpandedConfigFilePath);
            c = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(ExpandedConfigFilePath));
			if (c.EntryParameters == null) { throw new Exception("Configuration is not loaded properly"); }

            bb = Indicators.BollingerBands(Bars.ClosePrices, c.EntryParameters.BBDistance.Periods, c.EntryParameters.BBDistance.StandardDeviation, c.EntryParameters.BBDistance.MaType);

			if (RunInfo == null)
            {
                RunInfo = new RuntimeInformation(c, this);
                Print("Entry Mode is '{0}'", c.EntryParameters.EntryMode.ToString());

                PendingOrders.Filled += PendingOrders_Filled;
            }
        }

        private void PendingOrders_Filled(PendingOrderFilledEventArgs obj)
        {
            RunInfo.Grids.Where(x => x.Status == TradeStatus.Pending && x.RobotPendingOrder.Id == obj.PendingOrder.Id).All(x => {
                x.WhenOrderFilled(obj.Position);
                return true;
            });
        }

        private double LastTempValue = 0;

        protected override void OnTick()
        {
			if (RunInfo.Active)
			{
				RunInfo.EnsureAllClosed();
			}
            //Put your core logic here
            if (c.EntryParameters.EntryMode == EntryMode.Continuous)
            {

            }
            else if (c.EntryParameters.EntryMode == EntryMode.Volume)
            {
                if (!RunInfo.Active && LastTempValue != Bars.TickVolumes.Last(1))
                {
                    LastTempValue = Bars.TickVolumes.Last(1);
                    Print("TickVolume.Last(1): {0} - VolumeThreshold.Max: {1}", LastTempValue.ToString(), c.EntryParameters.TickVolumeThreshold.Max.ToString());
                }
                if (!RunInfo.Active && Bars.TickVolumes.Maximum(3) <= c.EntryParameters.TickVolumeThreshold.Max.FallbackIfZero(TickVolumeThresholdMax))
                {
                    RunInfo.InitializeGrids();
                    Print("Grids: {0}", String.Join("-", RunInfo.Grids.Select(x => x.EstimatedPricing.ToString()).ToArray()));
                    Print("OriginalAsk: {0}", RunInfo.OriginalAsk.ToString());
                    Print("UpperGroundStartingPoint: {0}", RunInfo.UpperGroundStartingPoint.ToString());
                    Print("OriginalBid: {0}", RunInfo.OriginalBid.ToString());
                    Print("UnderGroundStartingPoint: {0}", RunInfo.UnderGroundStartingPoint.ToString());
                    RunInfo.PlaceGridOrders();
                }
                else if (RunInfo.Active)
                {
                    if (TextTotalNetProfit == null)
                    {
                        TextTotalNetProfit = Chart.DrawText("TotalNetProfit", "Total Net Profit: " + RunInfo.TotalNetProfit.ToString("#0.00"), Bars.OpenTimes.LastValue, Bars.HighPrices.LastValue, Color.White);
                    }
                    else
                    {
                        TextTotalNetProfit.Text = "Total Net Profit: " + RunInfo.TotalNetProfit.ToString("#0.00");
                    }
                    RunInfo.AnalyzeConditionalClosure();
                }
            }
			else if (c.EntryParameters.EntryMode == EntryMode.BollingerBandsDistance && RunInfo.Active)
			{
				if (TextTotalNetProfit == null)
				{
					TextTotalNetProfit = Chart.DrawText("TotalNetProfit", "Total Net Profit: " + RunInfo.TotalNetProfit.ToString("#0.00"), Bars.OpenTimes.LastValue, Bars.HighPrices.LastValue, Color.White);
				}
				else
				{
					TextTotalNetProfit.Text = "Total Net Profit: " + RunInfo.TotalNetProfit.ToString("#0.00");
				}
				RunInfo.AnalyzeConditionalClosure();
			}
			else if (c.EntryParameters.EntryMode == EntryMode.TimeRangeOfTheDay)
			{
				var CurrentTime = c.EntryParameters.TimeOfTheDay.TimeMode == DateTimeMode.Local ? Time.ToLocalTime() : Time;
				var IsCycleOkay = c.EntryParameters.TimeOfTheDay.MaximumCycle <= 0 || RunInfo.RunningCycle < c.EntryParameters.TimeOfTheDay.MaximumCycle;
				if (!RunInfo.Active)
				{
					if (CurrentTime > CurrentTime.Date.Add(c.EntryParameters.TimeOfTheDay.StartTimeSpan) && CurrentTime < CurrentTime.Date.Add(c.EntryParameters.TimeOfTheDay.EndTimeSpan))
					{
						RunInfo.InitializeCycle();
						RunInfo.InitializeGrids();
						Print("Grids: {0}", String.Join("-", RunInfo.Grids.Select(x => x.EstimatedPricing.ToString()).ToArray()));
						Print("OriginalAsk: {0}", RunInfo.OriginalAsk.ToString());
						Print("UpperGroundStartingPoint: {0}", RunInfo.UpperGroundStartingPoint.ToString());
						Print("OriginalBid: {0}", RunInfo.OriginalBid.ToString());
						Print("UnderGroundStartingPoint: {0}", RunInfo.UnderGroundStartingPoint.ToString());
						RunInfo.PlaceGridOrders();
					}
				}
				else
				{
					if (TextTotalNetProfit == null)
					{
						TextTotalNetProfit = Chart.DrawText("TotalNetProfit", "Total Net Profit: " + RunInfo.TotalNetProfit.ToString("#0.00"), Bars.OpenTimes.LastValue, Bars.HighPrices.LastValue, Color.White);
					}
					else
					{
						TextTotalNetProfit.Text = "Total Net Profit: " + RunInfo.TotalNetProfit.ToString("#0.00");
					}
					RunInfo.AnalyzeConditionalClosure();
				}
			}
        }

		protected override void OnBar()
		{
            List<int> BBDistances = new List<int>();
			if (c.EntryParameters.EntryMode == EntryMode.BollingerBandsDistance && !RunInfo.Active)
			{
                for (int i = 0; i < c.EntryParameters.BBDistance.BBDistancePeriods; i++)
                {
                    BBDistances.Add(this.DistanceInPips(bb.Top.Last(i + 1), bb.Bottom.Last(i + 1)));
                }

                if (BBDistances.All(x => x <= c.EntryParameters.BBDistance.BBDistanceThreshold))
                {
                    RunInfo.InitializeGrids();
                    Print("Grids: {0}", String.Join("-", RunInfo.Grids.Select(x => x.EstimatedPricing.ToString()).ToArray()));
                    Print("OriginalAsk: {0}", RunInfo.OriginalAsk.ToString());
                    Print("UpperGroundStartingPoint: {0}", RunInfo.UpperGroundStartingPoint.ToString());
                    Print("OriginalBid: {0}", RunInfo.OriginalBid.ToString());
                    Print("UnderGroundStartingPoint: {0}", RunInfo.UnderGroundStartingPoint.ToString());
                    RunInfo.PlaceGridOrders();
                }
            }
        }

		protected override void OnStop()
        {
            // Put your deinitialization logic here
        }

        private bool EnsureConfigFolder()
        {
			if (AutoCreateConfigFolder && !Directory.Exists(Path.GetDirectoryName(ExpandedConfigFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ExpandedConfigFilePath));
                return true;
            }
            else if (Directory.Exists(Path.GetDirectoryName(ExpandedConfigFilePath)))
            {
                return true;
            }
            return false;
        }

    }
}
