using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rdz.cBot.GridTrap
{
	public enum FallbackClosureMode
	{
		Disabled,
		MaxDuration
	}
	public enum LotSizeMode
	{
		Fixed,
		PercentageFromBalance
	}
	public enum DateTimeMode
	{
		Local,
		UTC
	}
    public enum EntryMode
    {
        Volume,
        Continuous,
		TimeRangeOfTheDay,
		BollingerBandsDistance
    }
    public enum GridSide
    {
        Undefined,
        UpperGround,
        UnderGround
    }
    public enum RuntimeStatus
    {
        Inactive,
        Active
    }
    public enum OrderType
    {
        STOP,
        LIMIT
    }
    public enum ClosureMode
    {
        Fixed,
        Dynamic
    }
    public enum TradeStatus
    {
        Inactive,
        Pending,
        Active
    }
    public enum GridLotSizeMode
    {
        Fixed,
        Multiplier,
        Fibonacci
    }
    public enum GridIntervalMode
    {
        Fixed,
        Fibonacci
    }
}
