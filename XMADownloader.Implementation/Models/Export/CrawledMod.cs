using System;
using System.Collections.Generic;

namespace XMADownloader.Implementation.Models.Export
{
    public class CrawledMod
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public long UserId { get; set; }
        public DateTime PublishedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<CrawledFile> Files { get; set; }
    }
}
