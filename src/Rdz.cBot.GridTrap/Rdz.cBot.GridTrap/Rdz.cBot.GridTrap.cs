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
using Rdz.Indi.BollingerBandDistance;

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

        internal Configuration c;
        internal ChartText TextTotalNetProfit;

        private RuntimeInformation RunInfo { get; set; }
		public override bool AutoRefreshConfiguration { get; set; }

		private BollingerBandDistanceIndicator bb { get; set; }

		protected override void OnStart()
        {

            Print("Reading '{0}'", ExpandedConfigFilePath);
            c = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(ExpandedConfigFilePath));
			if (c.EntryParameters == null) { throw new Exception("Configuration is not loaded properly"); }
			//JsonConvert.PopulateObject(File.ReadAllText(ConfigurationFilePath), c);
			//c = Configuration.LoadConfiguration<Configuration>(ExpandedConfigFilePath);
			bb = Indicators.GetIndicator<BollingerBandDistanceIndicator>(Bars.ClosePrices, c.EntryParameters.BBDistance.MaType, c.EntryParameters.BBDistance.Periods, c.EntryParameters.BBDistance.StandardDeviation);

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
            // Put your core logic here
            if (c.EntryParameters.EntryMode == EntryMode.Continuous)
            {

            }
            else if (c.EntryParameters.EntryMode == EntryMode.Volume)
            {
                if (!RunInfo.Active && LastTempValue != MarketSeries.TickVolume.Last(1))
                {
                    LastTempValue = MarketSeries.TickVolume.Last(1);
                    Print("TickVolume.Last(1): {0} - VolumeThreshold.Max: {1}", LastTempValue.ToString(), c.EntryParameters.TickVolumeThreshold.Max.ToString());
                }
                if (!RunInfo.Active && MarketSeries.TickVolume.Maximum(3) <= c.EntryParameters.TickVolumeThreshold.Max.FallbackIfZero(TickVolumeThresholdMax))
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
                        TextTotalNetProfit = Chart.DrawText("TotalNetProfit", "Total Net Profit: " + RunInfo.TotalNetProfit.ToString("#0.00"), MarketSeries.OpenTime.LastValue, MarketSeries.High.LastValue, Color.White);
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
					TextTotalNetProfit = Chart.DrawText("TotalNetProfit", "Total Net Profit: " + RunInfo.TotalNetProfit.ToString("#0.00"), MarketSeries.OpenTime.LastValue, MarketSeries.High.LastValue, Color.White);
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
				var IsCycleOkay = c.EntryParameters.TimeOfTheDay.MaximumCycle > 0 ? RunInfo.RunningCycle < c.EntryParameters.TimeOfTheDay.MaximumCycle : true;
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
						TextTotalNetProfit = Chart.DrawText("TotalNetProfit", "Total Net Profit: " + RunInfo.TotalNetProfit.ToString("#0.00"), MarketSeries.OpenTime.LastValue, MarketSeries.High.LastValue, Color.White);
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
			if (c.EntryParameters.EntryMode == EntryMode.BollingerBandsDistance && !RunInfo.Active)
			{
				if (bb.Result.Reverse().Skip(1).Take(c.EntryParameters.BBDistance.BBDistancePeriods).All(x => x <= c.EntryParameters.BBDistance.BBDistanceThreshold))
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
