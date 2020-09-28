using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using Rdz.cBot.Library;
using Rdz.cBot.Library.Extensions;


namespace Rdz.cBot.SimpleScalping.Types
{
    internal class SimpleScalpingTrade
    {
        internal SimpleScalpingTrade(SimpleScalpingcBot robot)
        {
            ID = GenerateNewID();
            T1Label = robot.FullT1Label + "-" + ID;
            T2Label = robot.FullT2Label + "-" + ID;
            this.robot = robot;
            Highest = 0;
            Lowest = 0;
            Status = State.Inactive;
        }
        internal string GenerateNewID()
        {
            return new Guid().ToString("N").Substring(0, 5);
        }
        internal string T1Label { get; private set; }
        internal string T2Label { get; private set; }
        internal string ID { get; private set; }

        private SimpleScalpingcBot robot { get; set; }

        internal double HighestBuffer
        {
            get
            {
                return robot.ShiftPriceInPips(Highest, robot.BufferInPips);
            }
        }
        internal double Highest { get; set; }
        internal double Lowest { get; set; }
        internal double LowestBuffer
        {
            get
            {
                return robot.ShiftPriceInPips(Lowest, -robot.BufferInPips);
            }
        }
        internal int Risk
        {
            get
            {
                return robot.DistanceInPips(HighestBuffer, LowestBuffer);
            }
        }

        internal enum State
        {
            Inactive,
            InProgress,
            Pending,
            Active,
            AdvancedActive,
            Finished
        }
        internal State Status { get; set; }

        internal TradeType? TradeType { get; set; }

    }
}
