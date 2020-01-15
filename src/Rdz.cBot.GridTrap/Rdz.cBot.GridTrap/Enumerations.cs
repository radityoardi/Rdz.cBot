using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rdz.cBot.GridTrap
{
    internal enum EntryMode
    {
        Volume,
        Continuous
    }
    internal enum GridSide
    {
        Undefined,
        UpperGround,
        UnderGround
    }
    internal enum RuntimeStatus
    {
        Inactive,
        Active
    }
    internal enum OrderType
    {
        STOP,
        LIMIT
    }
    internal enum ClosureMode
    {
        Fixed,
        Dynamic
    }
    internal enum TradeStatus
    {
        Inactive,
        Pending,
        Active
    }
    internal enum GridLotSizeMode
    {
        Fixed,
        Multiplier,
        Fibonacci
    }
    internal enum GridIntervalMode
    {
        Fixed,
        Fibonacci
    }
}
