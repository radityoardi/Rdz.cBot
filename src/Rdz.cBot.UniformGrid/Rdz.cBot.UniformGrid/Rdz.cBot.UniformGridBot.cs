using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using Rdz.cBot.Library;


namespace Rdz.cBot
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class UniformGridBot : RdzRobot, IRdzRobot
    {
		[Parameter("Configuration Path", DefaultValue = @"%USERPROFILE%\Documents\Rdz.cBot.UniformGrid\configuration.json")]
		public string ConfigurationFilePath { get; set; }

		[Parameter("Auto-refresh", DefaultValue = false)]
		public bool AutoRefreshConfiguration { get; set; }

		[Parameter("Grid Trade Type", DefaultValue = TradeType.Buy)]
		public TradeType GridTradeType { get; set; }

		[Parameter("Take Net Profit", DefaultValue = 0.05)]
		public double TakeNetProfit { get; set; }

		[Parameter("Lot Size", DefaultValue = 0.01)]
		public double LotSize { get; set; }

		[Parameter("Row Heights (Points)", DefaultValue = 25)]
		public int RowHeights { get; set; }


		protected override void OnStart()
        {
            // Put your initialization logic here
        }

        protected override void OnTick()
        {
            // Put your core logic here
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }
    }
}
