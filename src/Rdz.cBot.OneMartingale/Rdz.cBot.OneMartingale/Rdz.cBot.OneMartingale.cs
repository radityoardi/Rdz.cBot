using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using Rdz.cBot.Library;
using Rdz.cBot.Library.Extensions;

namespace Rdz.cBot
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class OneMartingalecBot : Robot
    {
        [Parameter("Initial Quantity (Lots)", Group = "Volume", DefaultValue = 1, MinValue = 0.01, Step = 0.01)]
        public double InitialQuantity { get; set; }

        [Parameter("Stop Loss", Group = "Protection", DefaultValue = 40)]
        public int StopLoss { get; set; }

        [Parameter("Take Profit", Group = "Protection", DefaultValue = 40)]
        public int TakeProfit { get; set; }

        [Parameter("Multiplier", Group = "Protection", DefaultValue = 2)]
        public double Multiplier { get; set; }

        private Random random = new Random();
        private int losingStreak = 0;
        private TradeType LastTrade = TradeType.Buy;

        protected override void OnStart()
        {
            Positions.Closed += OnPositionsClosed;

            ExecuteOrder(InitialQuantity, RandomTradeType);
        }

        private void ExecuteOrder(double quantity, TradeType tradeType)
        {
            var volumeInUnits = Symbol.NormalizeVolumeInUnits(Symbol.QuantityToVolumeInUnits(quantity), RoundingMode.Up);
            Print("{0}", volumeInUnits);
            LastTrade = tradeType;
            var result = ExecuteMarketOrder(tradeType, SymbolName, volumeInUnits, "Martingale", StopLoss, TakeProfit);

            if (result.Error == ErrorCode.NoMoney)
                Stop();
        }

        private void OnPositionsClosed(PositionClosedEventArgs args)
        {
            Print("Closed");
            var position = args.Position;

            if (position.Label != "Martingale" || position.SymbolName != SymbolName)
                return;

            if (position.GrossProfit.IsPositive())
            {
                losingStreak.Reset();
                ExecuteOrder(InitialQuantity, position.TradeType);
                //ExecuteOrder(InitialQuantity, RandomTradeType);
                //ExecuteOrder(InitialQuantity, position.TradeType == TradeType.Buy ? TradeType.Sell : TradeType.Buy);
            }
            else
            {
                losingStreak.StepUp();
                ExecuteOrder(position.Quantity * Multiplier, position.TradeType.Reverse());
            }
        }

        private TradeType RandomTradeType
        {
            get
            {
                return random.Next(2) == 0 ? TradeType.Buy : TradeType.Sell;
            }
        }

        protected override void OnTick()
        {
            // Put your core logic here
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }
    }
}
