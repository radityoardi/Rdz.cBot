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

namespace Rdz.cBot.GridTrap
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class GridTrapBot : Robot
    {
        [Parameter("Auto-create Config Folder", DefaultValue = true)]
        public bool AutoCreateConfigFolder { get; set; }
        [Parameter("Config File Path", DefaultValue = @"C:\config.json")]
        public string ConfigurationFilePath { get; set; }
        [Parameter("TickVolumeThreshold.Max", DefaultValue = 0)]
        public double TickVolumeThresholdMax { get; set; }
        [Parameter("Closure.Fixed.TP", DefaultValue = 0)]
        public double ClosureFixedTP { get; set; }

        internal Configuration c;
        internal ChartText TextTotalNetProfit;

        private RuntimeInformation RunInfo { get; set; }

        protected override void OnStart()
        {
            // Put your initialization logic here
            if (!EnsureConfigFolder())
            {
                throw new FileNotFoundException(String.Format("File '{0}' is not found!", ConfigurationFilePath));
            }
            Print("Reading '{0}'", ConfigurationFilePath);
            c = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(ConfigurationFilePath));
            //JsonConvert.PopulateObject(File.ReadAllText(ConfigurationFilePath), c);

            if (RunInfo == null)
            {

                RunInfo = new RuntimeInformation(c, this);
                var entryMode = EntryMode.Volume;

                RunInfo.EntryMode = entryMode;
                Print("Entry Mode is '{0}'", RunInfo.EntryMode.ToString());

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
            // Put your core logic here
            if (RunInfo.EntryMode == EntryMode.Continuous)
            {

            }
            else if (RunInfo.EntryMode == EntryMode.Volume)
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
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }

        private bool EnsureConfigFolder()
        {
            if (AutoCreateConfigFolder && !Directory.Exists(Path.GetDirectoryName(ConfigurationFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigurationFilePath));
                return true;
            }
            else if (Directory.Exists(Path.GetDirectoryName(ConfigurationFilePath)))
            {
                return true;
            }
            return false;
        }

    }
}
