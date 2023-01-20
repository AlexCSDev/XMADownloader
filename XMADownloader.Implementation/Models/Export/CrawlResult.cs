using System;
using System.Collections.Generic;

namespace XMADownloader.Implementation.Models.Export
{
    public class CrawlResult
    {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public DateTime CrawledOn { get; set; }

        public List<CrawledMod> Mods { get; set; }
    }
}
