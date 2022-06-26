using Rdz.cBot.Library.Chart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rdz.cBot
{
    class MATrendIndicator
    {
        public int Index { get; set; }
        public double FasterMA { get; set; }
        public double SlowerMA { get; set; }
        public int FasterSlowerMADistance { get; set; }
        public Candlestick Candlestick { get; set; }
        public enSentiment Sentiment { get; set; }
        public enLocation Location { get; set; }
        public bool GoodSentiment { get; set; }
        public bool GoodLocation { get; set; }
    }
}
