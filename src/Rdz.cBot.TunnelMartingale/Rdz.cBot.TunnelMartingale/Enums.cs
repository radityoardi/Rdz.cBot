using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rdz.cBot.TunnelMartingale
{
	public enum enOpenCycleMethod
	{
		AlwaysBuy,
		AlwaysSell,
		Random,
		RememberLastProfitable,
		Trap
	}
	public enum enRunningCycleMethod
	{
		Normal,
		TrailingStop,
		NormalSmartBucket,
		MartingaleSmartBucket
	}

	public enum enSmartBucketModel
	{
		InCycleOnly,
		AcrossCycleOnly,
		All
	}
	public enum enProfitCalculation
	{
		Normal,
		WithLossBucket
	}
	public enum enTunnelStatus
	{
		Inactive,
		PendingOrders,
		PendingActivation,
		Running
	}
	public enum enSymbolRetrieval
	{
		UseChart,
		Custom
	}
	public enum enTargetType
	{
		FixedTargetProfit,
		FixedTargetPoints,
		LevelTargetProfit
	}
	public enum enSessionTargetType
	{
		FollowParent,
		FixedTargetProfit,
		FixedTargetPoints
	}
	public enum enSessionMode
	{
		ContinuousWithinDuration,
		Limited
	}

	public enum enTunnelHeightMode
	{
		Fixed,
		BollingerBandsDistance
	}
	public enum enTrapEntryMethod
	{
		Normal,
		BollingerBandsDistance
	}
	public enum enCriteria
	{
		Equal,
		LessThan,
		GreaterThan,
		LessThanOrEqual,
		GreaterThanOrEqual
	}
	public enum enStartingLotSizeType
	{
		Fixed,
		BalancePercentage
	}
}
