using Rdz.cBot.Library.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rdz.cBot.Library
{
	public class RdzConfiguration
	{
		public static bool IsConfigurationFolderPrepared(string ConfigurationFilePath, bool AutoCreate = false)
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

		public static T LoadConfiguration<T>(string expandedFilePath)
		{
			if (IsConfigurationFolderPrepared(expandedFilePath))
			{
				if (File.Exists(expandedFilePath))
				{
					string fileContent = File.ReadAllText(expandedFilePath);
					return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(fileContent);
				}
				else
				{
					throw new FileNotFoundException(String.Format("File '{0}' is not found!", expandedFilePath));
				}
			}
			return default(T);
		}
	}
}
