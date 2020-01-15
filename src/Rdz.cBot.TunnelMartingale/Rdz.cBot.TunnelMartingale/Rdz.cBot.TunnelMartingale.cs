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
		[Parameter("Configuration Path", DefaultValue = @"%USERPROFILE%\Documents\Rdz.cBot.TunnelMartingale\configuration.json")]
		public string ConfigurationFilePath { get; set; }
		[Parameter("Auto-refresh", DefaultValue = false)]
		public bool AutoRefreshConfiguration { get; set; }

		internal TunnelMartingaleConfiguration config { get; set; }
		internal Tunnel tm { get; set; }
		internal string ExpandedConfigFilePath {
			get
			{
				return Environment.ExpandEnvironmentVariables(ConfigurationFilePath);
			}
		}



		protected override void OnStart()
        {
			PendingOrders.Filled += PendingOrders_Filled;
			config = LoadConfiguration<TunnelMartingaleConfiguration>(ExpandedConfigFilePath);
			tm = new Tunnel(this);
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
