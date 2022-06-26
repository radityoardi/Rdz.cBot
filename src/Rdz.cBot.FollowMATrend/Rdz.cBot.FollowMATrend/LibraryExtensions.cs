using Rdz.cBot.Library.Chart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rdz.cBot.Library;
using Rdz.cBot.Library.Extensions;

namespace Rdz.cBot
{
    internal static class LibraryExtensions
    {
        internal static enSentiment IdentifySentiment(this Candlestick candlestick, double against)
        {
            return candlestick.Close.IsAbove(against) ? enSentiment.Bullish : candlestick.Close.IsBelow(against) ? enSentiment.Bearish : enSentiment.Neutral;
        }
        internal static int IdentifySigns(this enSentiment sentiment)
        {
            return sentiment == enSentiment.Bullish ? 1 : sentiment == enSentiment.Bearish ? -1 : 0;
        }

        internal static enLocation IdentifyLocation(this Candlestick candlestick, double against)
        {
            return candlestick.IsAbove(against) ? enLocation.Above : candlestick.IsBelow(against) ? enLocation.Below : enLocation.Neutral;
        }
    }
}
