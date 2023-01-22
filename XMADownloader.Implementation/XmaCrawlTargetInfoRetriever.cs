using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.DefaultImplementations.Interfaces;
using XMADownloader.Implementation.Models;
using XMADownloader.Implementation.Models.Export;

namespace XMADownloader.Implementation
{
    internal sealed class XmaCrawlTargetInfoRetriever : ICrawlTargetInfoRetriever
    {
        private readonly IWebDownloader _webDownloader;
        private readonly static Regex _urlValidationRegex = new Regex("https:\\/\\/(?>www\\.)?xivmodarchive\\.com\\/user\\/([0-9]+)");

        public XmaCrawlTargetInfoRetriever(IWebDownloader webDownloader)
        {
            _webDownloader = webDownloader ?? throw new ArgumentNullException(nameof(webDownloader));
        }

        public async Task<ICrawlTargetInfo> RetrieveCrawlTargetInfo(string url)
        {
            Match match = _urlValidationRegex.Match(url);
            if (!match.Success)
                throw new Exception("Invalid user page url");

            string html = await _webDownloader.DownloadString(url);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            HtmlNode usernameNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,\"row\")]/div[@class=\"col-10\"]/div/h1[@class=\"py-0 my-0\"]");
            if (usernameNode == null)
                throw new Exception("Username node not found!");

            return new XmaCrawlTargetInfo
            {
                Name = usernameNode.InnerText.Trim(),
                Id = Convert.ToInt64(match.Groups[1].Value),
                CrawledMods = new List<CrawledMod>()
            };
        }
    }
}
