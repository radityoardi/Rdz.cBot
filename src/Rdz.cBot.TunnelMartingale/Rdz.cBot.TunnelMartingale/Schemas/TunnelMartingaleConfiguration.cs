using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using Newtonsoft.Json;

namespace Rdz.cBot.TunnelMartingale.Schemas
{
	public class SessionDatesConfig
	{
		public SessionDatesConfig()
		{
			Sessions = new List<SessionInfo>();
			SessionMode = enSessionMode.Limited;
		}
		public bool Enabled { get; set; }
		public List<SessionInfo> Sessions { get; set; }
		public string ParseFormat { get; set; }
		public TimeSpan Interval { get; set; }
		public string CultureParser { get; set; }
		public string SymbolName { get; set; }
		public enSessionMode SessionMode { get; set; }
		public int MaxCyclePerSession { get; set; }
	}
	public class SessionInfo
	{
		public SessionInfo()
		{
			Enabled = true;
			SymbolRetrieval = enSymbolRetrieval.UseChart;
			Target = new SessionTargetInfo();
		}
		public string Date { get; set; }
		[JsonIgnore]
		public DateTime ActualDate { get; set; }
		public TradeType SessionTradeType { get; set; }
		public enSymbolRetrieval SymbolRetrieval { get; set; }
		public string SymbolName { get; set; }
		public bool Enabled { get; set; }
		public SessionTargetInfo Target { get; set; }
	}

	public abstract class TargetInfo
	{
		public TargetInfo()
		{
			EnableStopLoss = false;
			EnableTargetProfit = true;
			FixedStopLoss = 0;
		}
		public double FixedTargetProfit { get; set; }
		public double FixedTargetPoints { get; set; }
		public double FixedStopLoss { get; set; }
		public bool EnableTargetProfit { get; set; }
		public bool EnableStopLoss { get; set; }
	}

	public class GlobalTargetInfo : TargetInfo
	{
		public GlobalTargetInfo() : base()
		{
			TargetType = enTargetType.FixedTargetProfit;
		}
		public enTargetType TargetType { get; set; }
	}
	public class SessionTargetInfo : TargetInfo
	{
		public SessionTargetInfo() : base()
		{
			TargetType = enSessionTargetType.FollowParent;
		}
		public enSessionTargetType TargetType { get; set; }
	}

	public class TunnelMartingaleConfiguration
	{
		public TunnelMartingaleConfiguration()
		{
			SessionDates = new SessionDatesConfig();
			Target = new GlobalTargetInfo();
			CloseAllOrdersOnStop = true;
			LevelTargetProfits = new List<double>();
			OpenCycleMethod = enOpenCycleMethod.Trap;
			RunningCycleMethod = enRunningCycleMethod.Normal;
		}
		public enOpenCycleMethod OpenCycleMethod { get; set; }
		public enRunningCycleMethod RunningCycleMethod { get; set; }
		public int TunnelHeight { get; set; }
		public double StartingLotSize { get; set; }
		public double LotMultiplier { get; set; }
		public List<double> LevelTargetProfits { get; set; }

		public bool CloseAllOrdersOnStop { get; set; }
		public GlobalTargetInfo Target { get; set; }

		public SessionDatesConfig SessionDates { get; set; }
		public string Key { get; set; }
	}
}
