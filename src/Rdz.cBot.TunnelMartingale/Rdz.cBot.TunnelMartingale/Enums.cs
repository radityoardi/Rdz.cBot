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
		RememberLastProfitable
	}
	public enum enTunnelStatus
	{
		Inactive,
		Pending,
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
}
