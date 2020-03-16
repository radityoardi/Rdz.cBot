using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;

namespace Rdz.cBot.Library
{
	public interface IRdzRobot
	{
		string ConfigurationFilePath { get; set; }
		bool AutoRefreshConfiguration { get; set; }

		string ExpandedConfigFilePath { get; }
	}
}
