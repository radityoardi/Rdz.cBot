using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using cAlgo.API;
using Newtonsoft.Json;
using Rdz.cBot.Library;
using Rdz.cBot.Library.Extensions;

namespace Rdz.cBot.TunnelMartingale.Schemas
{
	public class TradeSessionDates : ATradeSessionDates
	{
		public TradeSessionDates()
		{
			Sessions = new List<TradeSession>();
			SessionMode = enSessionMode.Limited;
			CloseAllOrdersWhenIntervalEnds = false;
		}
		public List<TradeSession> Sessions { get; set; }
		public string ParseFormat { get; set; }
		public TimeSpan Interval { get; set; }
		public string CultureParser { get; set; }
		public string SymbolName { get; set; }
		public enSessionMode SessionMode { get; set; }
		public int MaxOpenPositions { get; set; }
		public bool CloseAllOrdersWhenIntervalEnds { get; set; }
	}

	public abstract class ATradeSessionDates
    {
		public ATradeSessionDates()
        {
			Enabled = false;
        }
		public bool Enabled { get; set; }
	}

	public class TradeSession
	{
		public TradeSession()
		{
			Enabled = true;
			SymbolRetrieval = enSymbolRetrieval.UseChart;
			Target = new SessionTargetInfo();
		}
		public string Date { get; set; }
		[JsonIgnore]
		public DateTime ActualStartDate { get; set; }
		[JsonIgnore]
		public DateTime ActualEndDate { get; set; }
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
	public class TrapConfiguration
	{
		public TrapConfiguration()
		{
			Validity = TimeSpan.Zero;
			TrapEntryMethod = enTrapEntryMethod.Normal;
			BollingerBandsDistanceCriteria = enCriteria.LessThanOrEqual;
		}
		public TimeSpan Validity { get; set; }
		public enTrapEntryMethod TrapEntryMethod { get; set; }
		public enCriteria BollingerBandsDistanceCriteria { get; set; }
		public int BollingerBandsDistance { get; set; }
		public bool IsTrapEntryTime(int input)
		{
			switch (BollingerBandsDistanceCriteria)
			{
				case enCriteria.Equal:
					return input == BollingerBandsDistance;
				case enCriteria.LessThan:
					return input < BollingerBandsDistance;
				case enCriteria.GreaterThan:
					return input > BollingerBandsDistance;
				case enCriteria.LessThanOrEqual:
					return input <= BollingerBandsDistance;
				case enCriteria.GreaterThanOrEqual:
					return input >= BollingerBandsDistance;
				default:
					return false;
			}
		}
	}

	public class TunnelMartingaleConfiguration
	{
		public TunnelMartingaleConfiguration()
		{
			SessionDates = new TradeSessionDates();
			Target = new GlobalTargetInfo();
			Trap = new TrapConfiguration();
			CloseAllOrdersOnStop = true;
			LevelTargetProfits = new List<double>();
			OpenCycleMethod = enOpenCycleMethod.Trap;
			RunningCycleMethod = enRunningCycleMethod.Normal;
			SmartBucketModel = enSmartBucketModel.All;
			TunnelHeightMode = enTunnelHeightMode.Fixed;
			StartingLotSizeType = enStartingLotSizeType.Fixed;
			InitialBucket = 0;
			StartingLotSize = 0.01;
		}
		public enOpenCycleMethod OpenCycleMethod { get; set; }
		public enRunningCycleMethod RunningCycleMethod { get; set; }
		/// <summary>
		/// Compounding the Net Loss Bucket across sessions.
		/// </summary>
		public enSmartBucketModel SmartBucketModel { get; set; }
		public enTunnelHeightMode TunnelHeightMode { get; set; }
		public int TunnelHeight { get; set; }
		public enStartingLotSizeType StartingLotSizeType { get; set; }
		public double StartingLotSize { get; set; }
		public double LotMultiplier { get; set; }
		public List<double> LevelTargetProfits { get; set; }

		public bool CloseAllOrdersOnStop { get; set; }
		public GlobalTargetInfo Target { get; set; }

		public TradeSessionDates SessionDates { get; set; }
		public string Key { get; set; }

		public double InitialBucket { get; set; }

		public TrapConfiguration Trap { get; set; }

	}
}
