using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.Common.Interfaces;
using XMADownloader.Implementation.Models;
using XMADownloader.Implementation.Models.Export;

namespace XMADownloader.Implementation
{
    internal class XmaCrawlResultsExporter : ICrawlResultsExporter
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private string _downloadDirectory = null;

        public Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _downloadDirectory = settings.DownloadDirectory;
            return Task.CompletedTask;
        }

        public async Task ExportCrawlResults(ICrawlTargetInfo crawlTargetInfo, List<ICrawledUrl> crawledUrls)
        {
            if (crawledUrls.Count == 0)
                return;
            XmaCrawlTargetInfo xmaCrawlTargetInfo = (XmaCrawlTargetInfo)crawlTargetInfo;

            CrawlResult crawlResults = new CrawlResult();
            crawlResults.UserId = xmaCrawlTargetInfo.Id;
            crawlResults.UserName = xmaCrawlTargetInfo.Name;
            crawlResults.CrawledOn = DateTime.UtcNow;
            crawlResults.Mods = xmaCrawlTargetInfo.CrawledMods;

            Dictionary<string, CrawledMod> modsDictionary = new Dictionary<string, CrawledMod>(crawlResults.Mods.Count);
            foreach (CrawledMod mod in crawlResults.Mods)
                if (!modsDictionary.ContainsKey(mod.Id))
                    modsDictionary.Add(mod.Id, mod);

            _logger.Debug("XMA export:");
            _logger.Debug($"{xmaCrawlTargetInfo.Id} - {xmaCrawlTargetInfo.Name} - {xmaCrawlTargetInfo.SaveDirectory}");

            foreach (ICrawledUrl crawledUrl in crawledUrls)
            {
                _logger.Debug($"Parsing crawled url {crawledUrl.Url}");

                if (string.IsNullOrWhiteSpace(crawledUrl.DownloadPath))
                {
                    _logger.Warn($"{crawledUrl.Url} has empty download path and will not be added to results export");
                    continue;
                }

                XmaCrawledUrl xmaCrawledUrl = (XmaCrawledUrl)crawledUrl;
                if (!xmaCrawledUrl.IsDownloaded)
                {
                    _logger.Warn($"{crawledUrl.Url} was not downloaded and will not be added to results export");
                    continue;
                }

                if (!modsDictionary.ContainsKey(xmaCrawledUrl.ModId))
                {
                    _logger.Fatal($"{xmaCrawledUrl.Url} refers to unknown mod id: {xmaCrawledUrl.ModId}");
                    continue;
                }

                CrawledFile crawledFile = new CrawledFile();
                crawledFile.Url = xmaCrawledUrl.Url;
                crawledFile.Path = xmaCrawledUrl.DownloadPath.Trim(Path.DirectorySeparatorChar); //todo: googledrive/mega
                if (modsDictionary[xmaCrawledUrl.ModId].Files == null)
                    modsDictionary[xmaCrawledUrl.ModId].Files = new List<CrawledFile>();
                modsDictionary[xmaCrawledUrl.ModId].Files.Add(crawledFile);
            }

            string crawlResultsPath = Path.Combine(_downloadDirectory, xmaCrawlTargetInfo.Id.ToString(), "CrawlResults.json");
            if (File.Exists(crawlResultsPath))
            {
                string backupFilename =
                    $"{Path.GetFileNameWithoutExtension(crawlResultsPath)}_old_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{Path.GetExtension(crawlResultsPath)}";
                _logger.Warn($"CrawlResults.json already exists, backing up old file to {backupFilename}");
                File.Move(crawlResultsPath, Path.Combine(_downloadDirectory, backupFilename));
            }

            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            using (StreamWriter sw =
                new StreamWriter(crawlResultsPath))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, crawlResults);
                }
            }
        }
    }
}
