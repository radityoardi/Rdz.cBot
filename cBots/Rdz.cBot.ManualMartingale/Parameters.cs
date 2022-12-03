using cAlgo.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rdz.cBot
{
	public partial class ManualMartingale
	{
		[Parameter("Initial Lots", Group = gMartingale, DefaultValue = 0.01, MinValue = 0.01, Step = 0.01)]
		public double InitialLots { get; set; }

		[Parameter("Heights (pips)", Group = gMartingale, DefaultValue = 10, MinValue = 0.1, Step = 1)]
		public double Heights { get; set; }
	}
}
