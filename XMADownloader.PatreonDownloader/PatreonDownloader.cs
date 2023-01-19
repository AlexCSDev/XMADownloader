using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using XMADownloader.PatreonDownloader.Models;

namespace XMADownloader.PatreonDownloader
{
    /// <summary>
    /// This is a very simplified version of PatreonDownloader from https://github.com/AlexCSDev/PatreonDownloader
    /// </summary>
    internal class PatreonDownloader
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly IWebDownloader _webDownloader;

        public PatreonDownloader(IWebDownloader webDownloader)
        {
            _webDownloader = webDownloader;
        }

        public async Task DownloadUrlAsync(long postId, string downloadPath)
        {
            _logger.Debug($"[Patreon {postId}] Downloading");

            string url = $"https://www.patreon.com/posts/{postId}";
            string apiUrl = $"https://www.patreon.com/api/posts/{postId}";

            string json = await _webDownloader.DownloadString(apiUrl);

            _logger.Debug($"[Patreon {postId}] Parsing json");
            PatreonPostRoot jsonRoot = JsonConvert.DeserializeObject<PatreonPostRoot>(json);
            List<Included> attachments = new List<Included>();
            if (jsonRoot.Included != null)
                attachments = jsonRoot.Included.Where(x => x.Type.ToLowerInvariant() == "attachment").ToList();

            _logger.Debug($"[Patreon {postId}] Post file exists: {(jsonRoot.Data.Attributes.PostFile != null)}");
            _logger.Debug($"[Patreon {postId}] Attachments: {attachments.Count}");

            if (jsonRoot.Data.Attributes.PostFile != null)
            {
                _logger.Info($"[Patreon] Downloading {postId} -> {jsonRoot.Data.Attributes.PostFile.Name}");
                await _webDownloader.DownloadFile(jsonRoot.Data.Attributes.PostFile.Url, Path.Combine(downloadPath, jsonRoot.Data.Attributes.PostFile.Name), url);
            }

            foreach(Included attachment in attachments)
            {
                _logger.Info($"[Patreon] Downloading {postId} -> {attachment.Attributes.Name}");
                await _webDownloader.DownloadFile(attachment.Attributes.Url, Path.Combine(downloadPath, $"{attachment.Id}_{attachment.Attributes.Name}"), url);
            }
        }
    }
}
