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
		public int MaxTrades { get; set; }
	}
	public class SessionInfo
	{
		public SessionInfo()
		{
			Enabled = true;
			SymbolRetrieval = enSymbolRetrieval.UseChart;
			TargetType = enSessionTargetType.FollowParent;
		}
		public string Date { get; set; }
		[JsonIgnore]
		public DateTime ActualDate { get; set; }
		public TradeType SessionTradeType { get; set; }
		public enSymbolRetrieval SymbolRetrieval { get; set; }
		public string SymbolName { get; set; }
		public enSessionTargetType TargetType { get; set; }
		public bool Enabled { get; set; }
		public double FixedTargetProfit { get; set; }
		public double FixedTargetPoints { get; set; }
	}
	public class TunnelMartingaleConfiguration
	{
		public TunnelMartingaleConfiguration()
		{
			SessionDates = new SessionDatesConfig();
			CloseAllOrdersOnStop = true;
			TargetType = enTargetType.FixedTargetProfit;
			LevelTargetProfits = new List<double>();
		}
		public enOpenCycleMethod OpenCycleMethod { get; set; }
		public int TunnelHeight { get; set; }
		public double StartingLotSize { get; set; }
		public double LotMultiplier { get; set; }
		public List<double> LevelTargetProfits { get; set; }

		public double FixedTargetProfit { get; set; }
		public double FixedTargetPoints { get; set; }
		public double FixedStopLoss { get; set; }
		public bool CloseAllOrdersOnStop { get; set; }
		public enTargetType TargetType { get; set; }

		public SessionDatesConfig SessionDates { get; set; }
		public string Key { get; set; }
	}
}
