﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo;
using cAlgo.API;
using Rdz.cBot.Library.Extensions;
using System.IO;
using Newtonsoft.Json;
using Rdz.cBot.Library.Chart;

namespace Rdz.cBot.Library
{
	public class RdzRobot : Robot, IRdzRobot
	{
		public virtual string ConfigurationFilePath { get; set; }
		public virtual bool AutoRefreshConfiguration { get; set; }

		public string ExpandedConfigFilePath { get { return Environment.ExpandEnvironmentVariables(ConfigurationFilePath); } }

		protected bool IsConfigurationFolderPrepared(string ConfigurationFilePath, bool AutoCreate = false)
		{
			ConfigurationFilePath = Environment.ExpandEnvironmentVariables(ConfigurationFilePath);
			if (ConfigurationFilePath.IsNotEmpty())
			{
				if (!Directory.Exists(Path.GetDirectoryName(ConfigurationFilePath)))
				{
					if (AutoCreate)
					{
						Directory.CreateDirectory(Path.GetDirectoryName(ConfigurationFilePath));
					}
					else { return false; }
					return true;
				}
				else return true;
			}
			return false;
		}
		protected T LoadConfiguration<T>(string expandedFilePath)
		{
			if (IsConfigurationFolderPrepared(expandedFilePath))
			{
				if (File.Exists(expandedFilePath))
				{
					string fileContent = File.ReadAllText(expandedFilePath);
					return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(fileContent);
				}
				else
					throw new FileNotFoundException("File '" + expandedFilePath + "' could not be found.");
			}
			else
				throw new FileNotFoundException("Folder for file '" + expandedFilePath + "' could not be found.");
		}
		protected string ExpandConfigFilePath(string ConfigurationFilePath)
		{
			return Environment.ExpandEnvironmentVariables(ConfigurationFilePath);
		}
	}
}
