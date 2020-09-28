using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using Rdz.cBot.Library;
using Rdz.cBot.Library.Extensions;
using Rdz.cBot.TunnelMartingale;
using Rdz.cBot.TunnelMartingale.Schemas;
using System.IO;


namespace Rdz.cBot
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
	public class TunnelMartingaleBot : RdzRobot, IRdzRobot
	{
#if DEBUG
		[Parameter("Configuration Path", Group = "Basic", DefaultValue = @"%USERPROFILE%\Documents\Git\radityoardi\Rdz.cBot\src\Rdz.cBot.TunnelMartingale\Rdz.cBot.TunnelMartingale\Configuration\config.json")]
#else
		[Parameter("Configuration Path", DefaultValue = @"%USERPROFILE%\Documents\Rdz.cBot.TunnelMartingale\config.json")]

#endif
		public override string ConfigurationFilePath { get; set; }
		[Parameter("Auto-refresh", Group = "Basic", DefaultValue = false)]
		public override bool AutoRefreshConfiguration { get; set; }

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

        #region Bollinger Bands Config
		[Parameter("Periods", Group = "Indie: Bollinger Bands", DefaultValue = 50)]
		public int BBPeriods { get; set; }
		[Parameter("MA Type", Group = "Indie: Bollinger Bands", DefaultValue = MovingAverageType.Exponential)]
		public MovingAverageType BBMAType { get; set; }
        #endregion

        internal TunnelMartingaleConfiguration config { get; set; }
		internal TunnelMartingaleEngine tm { get; set; }


		protected override void OnStart()
        {
			PendingOrders.Filled += PendingOrders_Filled;
			Positions.Closed += Positions_Closed;
			config = LoadConfiguration<TunnelMartingaleConfiguration>(ExpandedConfigFilePath);
			tm = new TunnelMartingaleEngine(this);
		}

		private void Positions_Closed(PositionClosedEventArgs result)
		{
			if (tm != null) tm.PositionsClosed(result);
		}

		private void PendingOrders_Filled(PendingOrderFilledEventArgs result)
		{
			if (tm != null) tm.PendingOrderFilled(result);
		}

		protected override void OnTick()
        {
			// Put your core logic here
			if (tm != null) tm.TickCheck();
        }

		protected override void OnStop()
        {
			// Put your deinitialization logic here
			if (tm != null)
			{
				if (config.CloseAllOrdersOnStop)
					tm.EndTunnel(); //optional

				tm.EndSessions();
			}
        }



	}
}
