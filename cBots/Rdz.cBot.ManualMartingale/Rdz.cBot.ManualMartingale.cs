using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using Rdz.cTrader.Library;

namespace Rdz.cBot
{
    [Robot(AccessRights = AccessRights.None)]
    public partial class ManualMartingale : Robot
    {

        protected override void OnStart()
        {
            Positions.Opened += Positions_Opened;
            Positions.Modified += Positions_Modified;
        }

        protected override void OnTick()
        {
            // Handle price updates here
        }

        protected override void OnStop()
        {
            // Handle cBot stop here
        }

		private void Positions_Modified(PositionModifiedEventArgs posargs)
		{
            posargs.Position.mo
		}

		private void Positions_Opened(PositionOpenedEventArgs posargs)
		{
		}


	}
}