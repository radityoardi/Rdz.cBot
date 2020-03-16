using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rdz.cBot.Library
{
	public enum enCodeType
	{
		Robot,
		Indicator
	}
	public enum enDirection
	{
		/// <summary>
		/// Bullish is the buy position (green candlestick) where it closes above the opening value.
		/// </summary>
		Bullish,
		/// <summary>
		/// Bearish is the sell position (red candlestick) where it closes below the opening value.
		/// </summary>
		Bearish,
		/// <summary>
		/// Neutral is where the open and close value are exactly the same.
		/// </summary>
		Neutral
	}

	public enum enCandlestickPart
	{
		All,
		RealBody,
		UpperShadow,
		LowerShadow,
		High,
		Low,
		RealBodyHigh,
		RealBodyLow
	}

	public enum enRecommendation
	{
		None,
		Buy,
		Sell
	}
}
