﻿using cAlgo.API;
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
		const string GroupCommon = "Common";
		const string GroupGrid = "Grid";
		const string GroupSmartStopLoss = "Smart Stop Loss";
		const string GroupStandardTakeProfit = "Standard Take Profit";
		const string GroupTrailingStopLoss = "Trailing Stop Loss";
		const string GroupOthers = "Others";

		#region Parameters
		[Parameter("Lot Size", Group = GroupCommon, DefaultValue = 0.01, MinValue = 0.01, MaxValue = 100000, Step = 0.01)]
		public double LotSize { get; set; }

		[Parameter("Grid Type", Group = GroupGrid, DefaultValue = enGridType.Both)]
		public enGridType gridType { get; set; }

		[Parameter("Grid Interval (pips)", Group = GroupGrid, DefaultValue = 20, MinValue = 0, Step = 1)]
		public int gridInterval { get; set; }

		[Parameter("Preorder Zone (pips)", Group = GroupGrid, DefaultValue = 50, MinValue = 0, Step = 1)]
		public int PreorderZone { get; set; }


		[Parameter("Order Margin Distance (pips)", Group = GroupGrid, DefaultValue = 7, MinValue = 0, Step = 1)]
		public int marginDistance { get; set; }

		[Parameter("Grid Placement Mode", Group = GroupGrid, DefaultValue = enGridCalculationMode.Anchor)]
		public enGridCalculationMode gridCalculationMode { get; set; }

		[Parameter("Anchor Price", Group = GroupGrid, DefaultValue = 0, MinValue = 0, Step = 0.00001)]
		public double anchorPrice { get; set; }

		[Parameter("Take Profit Mode", Group = GroupGrid, DefaultValue = enTakeProfitMode.StandardTakeProfit)]
		public enTakeProfitMode takeProfitMode { get; set; }

		[Parameter("Enable", Group = GroupSmartStopLoss, DefaultValue = true)]
		public bool enableSmartStopLoss { get; set; }

		[Parameter("Grid Distance (pips)", Group = GroupSmartStopLoss, DefaultValue = 50, MinValue = 0, Step = 1)]
		public int smartStopLossDistance { get; set; }


		[Parameter("Initial TP Threshold (lines)", Group = GroupStandardTakeProfit, DefaultValue = 1, MinValue = 0, Step = 0.5)]
		public double InitialTakeProfitThreshold { get; set; }

		[Parameter("Grid Distance (timestamp)", Group = GroupStandardTakeProfit, DefaultValue = "5.00:00:00")]
		public string smartSLMaxDurationText { get; set; }
		private TimeSpan smartSLMaxDuration
		{
			get
			{
				try
				{
					return TimeSpan.Parse(smartSLMaxDurationText);
				}
				catch
				{
					return TimeSpan.Zero;
				}
			}
		}

		[Parameter("Maximum Loss (%)", Group = GroupStandardTakeProfit, DefaultValue = 25, MinValue = 0, MaxValue = 1000)]
		public double AllowedLossPercentage { get; set; }

		[Parameter("Initial SL Threshold (lines)", Group = GroupTrailingStopLoss, DefaultValue = 1, MinValue = 0, Step = 0.5)]
		public double InitialStopLossThreshold { get; set; }

		[Parameter("Trailing SL Threshold (lines)", Group = GroupTrailingStopLoss, DefaultValue = 1, MinValue = 0, Step = 0.5)]
		public double TrailingStopLossThreshold { get; set; }

		[Parameter("Visual Aid", Group = GroupOthers, DefaultValue = true)]
		public bool VisualAid { get; set; }

		[Parameter("Print Logs", Group = GroupOthers, DefaultValue = false)]
		public bool PrintLogs { get; set; }

		#endregion
	}
}
