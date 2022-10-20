using cAlgo.API;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rdz.cBot
{
	public partial class MeshGrid
	{
		#region Parameters
		[Parameter("Lot Size", Group = "Common", DefaultValue = 0.01, MinValue = 0.01, MaxValue = 100000, Step = 0.01)]
		public double LotSize { get; set; }

		[Parameter("Grid Type", Group = "Grid", DefaultValue = enGridType.Both)]
		public enGridType gridType { get; set; }

		[Parameter("Grid Interval (pips)", Group = "Grid", DefaultValue = 20, MinValue = 0, Step = 1)]
		public int gridInterval { get; set; }

		[Parameter("Preorder Zone (pips)", Group = "Grid", DefaultValue = 50, MinValue = 0, Step = 1)]
		public int PreorderZone { get; set; }


		[Parameter("Order Margin Distance (pips)", Group = "Grid", DefaultValue = 7, MinValue = 0, Step = 1)]
		public int marginDistance { get; set; }

		[Parameter("Grid Calculation Mode", Group = "Grid", DefaultValue = enGridCalculationMode.Anchor)]
		public enGridCalculationMode gridCalculationMode { get; set; }

		[Parameter("Anchor Price", Group = "Grid", DefaultValue = 0, MinValue = 0, Step = 0.00001)]
		public double anchorPrice { get; set; }

		[Parameter("Enable", Group = "Smart Stop Loss", DefaultValue = true)]
		public bool enableSmartStopLoss { get; set; }
		
		[Parameter("Grid Distance (pips)", Group = "Smart Stop Loss", DefaultValue = 50, MinValue = 0, Step = 1)]
		public int smartStopLossDistance { get; set; }

		[Parameter("Grid Distance (timestamp)", Group = "Smart Stop Loss", DefaultValue = "5.00:00:00")]
		public string smartSLMaxDurationText { get; set; }
		private TimeSpan smartSLMaxDuration {
			get {
				return TimeSpan.Parse(smartSLMaxDurationText);
			}
		}

		[Parameter("Maximum Loss (%)", Group = "Smart Stop Loss", DefaultValue = 25, MinValue = 0, MaxValue = 1000)]
		public double AllowedLossPercentage { get; set; }

		[Parameter("Visual Aid", Group = "Others", DefaultValue = true)]
		public bool VisualAid { get; set; }

		[Parameter("Print Logs", Group = "Others", DefaultValue = false)]
		public bool PrintLogs { get; set; }

		#endregion
	}
}
