using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;


namespace Rdz.cBot
{
	public partial class MeshGrid
	{
		//Enumerations
		public enum enGridType
		{
			Both = 0, //Both
			BuyOnly = 1, //Buy Only
			SellOnly = 2, //Sell Only
		}

		public enum enGridCalculationMode
		{
			Anchor = 0, //From the anchor price
			CurrentPrice = 1, //From the current price
		}

		public enum enTakeProfitMode
		{
			StandardTakeProfit = 0,
			TrailingStopLoss = 1,
		}

		public enum enAsyncStatus
		{
			Ready = 0,
			InProgress = 1,
		}

	}
}
