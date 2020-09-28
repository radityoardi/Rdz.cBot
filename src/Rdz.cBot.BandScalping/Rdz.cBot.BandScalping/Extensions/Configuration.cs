using Newtonsoft.Json;
using Rdz.cBot.Library;
using Rdz.cBot.Library.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;



namespace Rdz.cBot.BandScalping.Extensions
{
    public class Configuration
    {
        public QuietZones QuietZones { get; set; }
    }


    public class QuietZones
    {
        public QuietZones()
        {
            Zones = new List<QuietZone>();
        }
        public string DateFormat { get; set; }
        public List<QuietZone> Zones { get; set; }
        [JsonIgnore]
        public IEnumerable<QuietZone> EnabledZones
        {
            get
            {
                return Zones.Where(x => x.Enabled == true);
            }
        }

        public IEnumerable<QuietZone> FutureZones(BandScalpingcBot bsBot)
        {
            return EnabledZones.Where(x => x.FromDate.ToUniversalTime() > bsBot.TimeInUtc).OrderBy(x => x.FromDate);
        }
        public void ParseAll()
        {
            if (Zones.Count > 0)
            {
                Zones.ForEach(x =>
                {
                    x.Parse(DateFormat);
                });
            }
        }
    }

    public class QuietZone
    {
        public QuietZone()
        {
            Enabled = true;
        }

        public bool Enabled { get; set; }
        public string From { get; set; }
        [JsonIgnore]
        public DateTime FromDate { get; private set; }
        public string To { get; set; }
        [JsonIgnore]
        public DateTime ToDate { get; private set; }
        public void Parse(string DateFormat)
        {
            if (From.IsNotEmpty())
            {
                FromDate = From.ParseDateTime(DateFormat);
            }
            if (To.IsNotEmpty())
            {
                ToDate = To.ParseDateTime(DateFormat);
            }
        }

        public bool IsInQuietZone
        {
            get
            {
                return DateTime.Now > FromDate && DateTime.Now < ToDate;
            }
        }
    }
}
