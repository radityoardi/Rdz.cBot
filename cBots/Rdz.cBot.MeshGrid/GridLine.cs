using cAlgo.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rdz.cTrader.Library;
using Rdz.cBot;

namespace Rdz.cBot
{
	public class GridLine
	{
		const string LineBaseName = "GridLine";
		const string TextBaseName = "TextInfo";

		private readonly Color buyColor = Color.Salmon;
		private readonly Color sellColor = Color.LawnGreen;

		public enum enStatus
		{
			Error = -1,
			Ready = 0,
			Pending = 1,
			NoTradeAllowed = 100,
		}


		public GridLine(double price, Position position)
		{
			ID = Guid.NewGuid();
			Price = price;
			Position = position;
			Status = enStatus.Pending;
			LineGraphicsName = $"{LineBaseName}-{position.TradeType}-{Guid.NewGuid().ToString("D")}";
			TextGraphicsName = $"{TextBaseName}-{position.TradeType}-{Guid.NewGuid().ToString("D")}";
		}
		public GridLine(double price, PendingOrder pendingOrder)
		{
			ID = Guid.NewGuid();
			Price = price;
			PendingOrder = pendingOrder;
			Status = enStatus.Pending;
			LineGraphicsName = $"{LineBaseName}-{pendingOrder.TradeType}-{Guid.NewGuid().ToString("D")}";
			TextGraphicsName = $"{TextBaseName}-{pendingOrder.TradeType}-{Guid.NewGuid().ToString("D")}";
		}
		public GridLine(double price)
		{
			ID = Guid.NewGuid();
			Price = price;
			Status = enStatus.Pending;
			LineGraphicsName = $"{LineBaseName}-{Guid.NewGuid().ToString("D")}";
			TextGraphicsName = $"{TextBaseName}-{Guid.NewGuid().ToString("D")}";
		}

		public Guid ID { get; set; }
		public string ShortID
		{
			get
			{
				return ID.ToString("D").Substring(9, 4).ToUpper();
			}
		}
		public double Price { get; set; }
		public Position Position { get; set; }
		public PendingOrder PendingOrder { get; set; }
		public string LineGraphicsName { get; private set; }
		public string TextGraphicsName { get; private set; }
		public enStatus Status { get; set; }

		public ChartTrendLine Line { get; set; }
		public ChartText Text { get; set; }

		public TradeType GridTradeType
		{
			get
			{
				return PendingOrder.TradeType;
			}
		}

		public bool IsOrdered
		{
			get
			{
				return PendingOrder != null && Position == null;
			}
		}

		public bool IsEmpty
		{
			get
			{
				return PendingOrder == null && Position == null;
			}
		}

		public bool IsFilled
		{
			get
			{
				return Position != null;
			}
		}

		public void ClosePosition()
		{
			Position.Close();
			Clear();
		}

		public void CancelOrder()
		{
			PendingOrder.Cancel();
			Clear();
		}

		public void Clear()
		{
			Position = null;
			PendingOrder = null;
		}


		public void ShowLine(Chart chart)
		{
			ShowLine(chart, Color.WhiteSmoke);
		}
		public void ShowLine(Chart chart, Color lineColor)
		{
			Line = chart.DrawTrendLine(LineGraphicsName, chart.BarsTotal - 40, Price, chart.BarsTotal, Price, lineColor, 5, LineStyle.Lines);
		}

		public void ShowText(Chart chart, Color textColor)
		{
			ShowText(chart, textColor, string.Empty);
		}
		public void ShowText(Chart chart, Color textColor, String customText)
		{
			var textInfo = string.Empty;
			int Shifter = 0;
			double Spread = chart.Symbol.Spread / 2;
			if (PendingOrder != null)
			{
				textInfo = $"GRID {ShortID} {PendingOrder.TradeType.ToString().ToUpper()} - {Price.ToString(chart.Symbol.Digits.DigitFormat())}";
				Shifter = PendingOrder.TradeType == TradeType.Buy ? 10 : 0;
			}
			else
			{
				textInfo = $"PRICE at {Price.ToString(chart.Symbol.Digits.DigitFormat())}";
			}
			
			Text = chart.DrawText(TextGraphicsName, string.IsNullOrEmpty(customText) ? textInfo : customText, chart.BarsTotal - 40, Price + (chart.Symbol.PipSize * Shifter), textColor);
			Text.IsBold = true;
			Text.FontSize = 10;
			
			Text.HorizontalAlignment = HorizontalAlignment.Left;
		}

		public void RefreshLine(Chart chart, Color lineColor)
		{
			RemoveLine(chart);
			ShowLine(chart, lineColor);
		}

		public void RefreshText(Chart chart, Color textColor)
		{
			RemoveText(chart);
			ShowText(chart, textColor);
		}

		public void RemoveLine(Chart chart)
		{
			chart.RemoveObject(LineGraphicsName);
		}
		public void RemoveText(Chart chart)
		{
			chart.RemoveObject(TextGraphicsName);
		}

		public void UpdateAsyncOrderResult(TradeResult result, Chart chart, bool VisualAid)
		{
			if (result.IsSuccessful)
			{
				PendingOrder = result.PendingOrder;
				Status = GridLine.enStatus.Ready;
				if (VisualAid) ShowText(chart, result.PendingOrder.TradeType == TradeType.Buy ? buyColor : sellColor);
			}
			else
				Status = GridLine.enStatus.Error;

		}
	}

	public static class GridLinesExtension
	{

	}
}
