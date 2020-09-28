using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using Newtonsoft.Json;
using Rdz.cBot.Library;

namespace Rdz.cBot.GridTrap.Schema
{
    public class Configuration : RdzConfiguration
    {
		public Configuration()
        {
			Enabled = true;
        }
		public bool Enabled { get; set; }
        public EntryParameters EntryParameters { get; set; }
        public ClosureParameters ClosureParameters { get; set; }
        public GridParameters GridParameters { get; set; }
    }

    public class GridParameters
    {
        public OrderType OrderType { get; set; }
        public double Size { get; set; }
        public double LotSize { get; set; }
        public Intervals Intervals { get; set; }
    }

    public class Intervals
    {
        public int Grid { get; set; }
        public int Starting { get; set; }
    }

    public class EntryParameters
    {
        public EntryMode EntryMode { get; set; }
        public TickVolumeThreshold TickVolumeThreshold { get; set; }
		public TimeOfTheDayParameters TimeOfTheDay { get; set; }
		public BollingerBandsDistanceParameters BBDistance { get; set; }
		public TradingDateParameters NoTradingDateParameter { get; set; }
    }
    public class TickVolumeThreshold
    {
        public double Max { get; set; }
        public double Min { get; set; }
    }
	public class TimeOfTheDayParameters
	{
		public DateTimeMode TimeMode { get; set; }
		[JsonIgnore]
		public TimeSpan StartTimeSpan { get { return TimeSpan.Parse(this.StartTime); } }
		public string StartTime { get; set; }
		[JsonIgnore]
		public TimeSpan EndTimeSpan { get { return TimeSpan.Parse(this.EndTime); } }
		public string EndTime { get; set; }
		public int MaximumCycle { get; set; }
	}

	public class BollingerBandsDistanceParameters
	{
		public BollingerBandsDistanceParameters()
		{
			MaType = MovingAverageType.Simple;
			StandardDeviation = 2;
			Periods = 14;
			BBDistanceThreshold = 50;
			BBDistancePeriods = 3;
		}
		[DefaultValue(MovingAverageType.Exponential)]
		public MovingAverageType MaType { get; set; }
		public int StandardDeviation { get; set; }

		public int Periods { get; set; }
		public int BBDistanceThreshold { get; set; }
		public int BBDistancePeriods { get; set; }
	}
	public class ClosureParameters
    {
        public ClosureMode ClosureMode { get; set; }
        public ClosureModeFixed Fixed { get; set; }
		public FallbackClosureMode FallbackClosureMode { get; set; }
		[JsonIgnore]
		public TimeSpan MaxDurationSpan { get { return TimeSpan.Parse(this.MaxDuration); } }
		public string MaxDuration { get; set; }
	}

	public class TradingDateParameters
	{

	}
	public class ClosureModeFixed
    {
        public double TakeProfit { get; set; }
    }
}
