using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;

namespace Rdz.cBot.GridTrap.Extensions
{
    public static class Common
    {
        public static int CalculateInterval(this Robot robot, double price1, double price2)
        {
            int gap = (int)((price1 - price2) / robot.Symbol.TickSize);
            return (gap < 0 ? gap * -1 : gap); //returns to always positive number
        }

        public static double ShiftPrice(this Robot robot, double fromPrice, int points)
        {
            var priceDiff = robot.Symbol.TickSize * points;
            return fromPrice + priceDiff;
        }

        public static double LotToVolume(this Robot robot, double LotSize)
        {
            return robot.Symbol.NormalizeVolumeInUnits(robot.Symbol.LotSize * LotSize);
        }
        public static double FallbackIfZero(this double mainInput, double fallbackInput)
        {
            return (mainInput == 0 ? fallbackInput : mainInput);
        }
        public static double TakeMainInputFirst(this double fallbackInput, double mainInput)
        {
            return (fallbackInput == 0 ? mainInput : fallbackInput);
        }
        public static int TakeMainInputFirst(this int fallbackInput, int mainInput)
        {
            return (fallbackInput == 0 ? mainInput : fallbackInput);
        }
        public static int Fibonacci(int n)
        {
            int a = 0;
            int b = 1;
            // In N steps compute Fibonacci sequence iteratively.
            for (int i = 0; i < n; i++)
            {
                int temp = a;
                a = b;
                b = temp + b;
            }
            return a;
        }
    }
}
