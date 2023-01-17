using System.Collections.Generic;
using XMADownloader.Implementation.Models;

namespace XMADownloader.Implementation
{
    /// <summary>
    /// Represents one crawled page with all results and link to the next page
    /// </summary>
    internal class ParsingResult
    {
        public List<XmaCrawledUrl> CrawledUrls { get; set; }
        public string NextPage { get; set; }
    }
}
