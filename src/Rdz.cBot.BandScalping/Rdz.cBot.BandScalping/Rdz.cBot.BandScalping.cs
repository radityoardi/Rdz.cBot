using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using Rdz.cBot.Library;
using Rdz.cBot.BandScalping;
using Rdz.cBot.BandScalping.Extensions;
using System.Xml.Linq;

namespace Rdz.cBot
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class BandScalpingcBot : RdzRobot, IRdzRobot
    {
#if DEBUG
        [Parameter("Configuration Path", Group = "Basic", DefaultValue = @"%USERPROFILE%\Documents\Git\radityoardi\Rdz.cBot\src\Rdz.cBot.BandScalping\Rdz.cBot.BandScalping\Config\dev.json")]
#else
		[Parameter("Configuration Path", DefaultValue = @"%USERPROFILE%\Documents\Rdz.cBot.BandScalping\prod.json")]

#endif
        public override string ConfigurationFilePath { get; set; }

        #region Run Config
        [Parameter("Label Prefix", Group = "Main", DefaultValue = "BS-")]
        public string LabelPrefix { get; set; }
        [Parameter("Aggressive", Group = "Main", DefaultValue = true)]
        public bool Aggressive { get; set; }
        [Parameter("Lot Size", Group = "Main", DefaultValue = 0.01, MinValue = 0.01, Step = 0.01)]
        public double LotSize { get; set; }
        [Parameter("Min. Duration", Group = "Main", DefaultValue = "00:10:00")]
        public string MinimumDuration { get; set; }
        internal TimeSpan MinimumDurationTimeSpan
        {
            get
            {
                return TimeSpan.Parse(MinimumDuration);
            }
        }
        [Parameter("Max. Volume", Group = "Main", DefaultValue = 250)]
        public double MaxVolume { get; set; }
        #endregion

        #region Bollinger Bands Config
        [Parameter("Enable", Group = "Indie: Bollinger Bands", DefaultValue = true)]
        public bool EnableBB { get; set; }
        [Parameter("Periods", Group = "Indie: Bollinger Bands", DefaultValue = 20)]
        public int BBPeriods { get; set; }
        [Parameter("Standard Deviation", Group = "Indie: Bollinger Bands", DefaultValue = 2)]
        public double BBStandardDev { get; set; }
        [Parameter("Moving Average Type", Group = "Indie: Bollinger Bands", DefaultValue = MovingAverageType.Exponential)]
        public MovingAverageType BBMAType { get; set; }
        #endregion

        #region Relative Strength Index Config
        [Parameter("Enable", Group = "Indie: Relative Strength Index", DefaultValue = true)]
        public bool EnableRSI { get; set; }
        [Parameter("Periods", Group = "Indie: Relative Strength Index", DefaultValue = 7)]
        public int RSIPeriods { get; set; }
        [Parameter("Upper Level", Group = "Indie: Relative Strength Index", DefaultValue = 70)]
        public double RSIUpperLevel { get; set; }
        [Parameter("Lower Level", Group = "Indie: Relative Strength Index", DefaultValue = 30)]
        public double RSILowerLevel { get; set; }
        #endregion

        #region Directional Movement System Config
        [Parameter("Enable", Group = "Indie: ADX", DefaultValue = true)]
        public bool EnableADX { get; set; }
        [Parameter("Periods", Group = "Indie: ADX", DefaultValue = 14)]
        public int DMSPeriod { get; set; }
        [Parameter("Level", Group = "Indie: ADX", DefaultValue = 30)]
        public double DMSLevel { get; set; }
        #endregion

        #region Not parameter properties
        private BandScalpingEngine bs { get; set; }
        internal Configuration config { get; set; }
        #endregion

        protected override void OnStart()
        {
            PendingOrders.Filled += PendingOrders_Filled;
            Positions.Closed += Positions_Closed;
            config = LoadConfiguration<Configuration>(ExpandedConfigFilePath);
            bs = new BandScalpingEngine(this);
        }

        private void Positions_Closed(PositionClosedEventArgs obj)
        {
            if (bs != null) bs.PositionsClosed(obj);
        }

        private void PendingOrders_Filled(PendingOrderFilledEventArgs obj)
        {
            if (bs != null) bs.PendingOrdersFilled(obj);
        }

        protected override void OnBar()
        {
            if (bs != null) bs.Bar();
        }

        protected override void OnTick()
        {
            if (bs != null) bs.Tick();
        }

        protected override void OnStop()
        {
            if (bs != null) bs.Stop();
        }
    }
}
