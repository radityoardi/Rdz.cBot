using cAlgo.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rdz.cBot.BackToSquareOne
{
	internal class UnitRecommendation
	{
		internal UnitRecommendation()
		{
			Recommendation = TradeRecommendation.Nothing;
			Description = string.Empty;
		}
		public TradeRecommendation Recommendation { get; set; }
		public string Description { get; set; }
	}
	internal class BotRecommendation
	{
		private int Total { get; set; }
		private bool Inverse { get; set; }
		internal BotRecommendation(int NumberOfRecommendation = 0, bool Inverse = false)
		{
			this.Total = NumberOfRecommendation;
			this.Inverse = Inverse;
			Reset();
		}
		internal List<UnitRecommendation> SmallUnits { get; set; }
		internal TradeRecommendation Recommmendation
		{
			get
			{
				if (SmallUnits.All(x => x.Recommendation == TradeRecommendation.Buy))
				{
					return Inverse ? TradeRecommendation.Sell : TradeRecommendation.Buy;
				}
				else if (SmallUnits.All(x => x.Recommendation == TradeRecommendation.Sell))
				{
					return Inverse ? TradeRecommendation.Buy : TradeRecommendation.Sell;
				}
				else
					return TradeRecommendation.Nothing;
			}
		}
		internal void Reset()
		{
			if (SmallUnits != null) SmallUnits.Clear();
			else SmallUnits = new List<UnitRecommendation>();
			if (Total > 0)
			{
				for (int i = 0; i < Total; i++)
				{
					SmallUnits.Add(new UnitRecommendation());
				}
			}
		}
	}
}
