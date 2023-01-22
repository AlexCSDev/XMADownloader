using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Ninject;
using NLog;
using System;
using System.Text.RegularExpressions;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.Common.Interfaces.Plugins;
using UniversalDownloaderPlatform.DefaultImplementations;
using UniversalDownloaderPlatform.DefaultImplementations.Interfaces;
using UniversalDownloaderPlatform.DefaultImplementations.Models;
using XMADownloader.Common.Models;
using XMADownloader.PatreonDownloader.Models;

namespace XMADownloader.PatreonDownloader
{
    public class Plugin : IPlugin
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public string Name => "Patreon Downloader (anonymous)";
        public string Author => "Aleksey Tsutsey";
        public string ContactInformation => "https://github.com/AlexCSDev/XMADownloader";

        private readonly static Regex _postPageRegex = new Regex("https:\\/\\/(?>www\\.)patreon.com\\/posts\\/([0-9]+)");

        private XmaDownloaderSettings _settings;

        private IWebDownloader _webDownloader;
        private IRemoteFileInfoRetriever _remoteFileInfoRetriever;

        public void OnLoad(IDependencyResolver dependencyResolver)
        {
            _webDownloader = dependencyResolver.Get<IWebDownloader>();
            _remoteFileInfoRetriever = dependencyResolver.Get<IRemoteFileInfoRetriever>();
        }

        public Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _settings = (XmaDownloaderSettings)settings;

            return Task.CompletedTask;
        }

        public async Task Download(ICrawledUrl crawledUrl)
        {
            try
            {
                Match match = _postPageRegex.Match(crawledUrl.Url);

                long postId = Convert.ToInt64(match.Groups[1].Value);
                string downloadPath = Path.Combine(_settings.DownloadDirectory, crawledUrl.DownloadPath);

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

                    (string _, long fileSize) = await _remoteFileInfoRetriever.GetRemoteFileInfo(jsonRoot.Data.Attributes.PostFile.Url, _settings.FallbackToContentTypeFilenames, url);

                    await _webDownloader.DownloadFile(jsonRoot.Data.Attributes.PostFile.Url, Path.Combine(downloadPath, $"{postId}_main_{jsonRoot.Data.Attributes.PostFile.Name}"), fileSize, url);
                }

                foreach (Included attachment in attachments)
                {
                    _logger.Info($"[Patreon] Downloading {postId} -> {attachment.Attributes.Name}");

                    (string _, long fileSize) = await _remoteFileInfoRetriever.GetRemoteFileInfo(attachment.Attributes.Url, _settings.FallbackToContentTypeFilenames, url);

                    await _webDownloader.DownloadFile(attachment.Attributes.Url, Path.Combine(downloadPath, $"{postId}_{attachment.Id}_{attachment.Attributes.Name}"), fileSize, url);
                }
            }
            catch (DownloadException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DownloadException($"Unable to download {crawledUrl.Url}: {ex}", ex);
            }
        }

        public Task<List<string>> ExtractSupportedUrls(string htmlContents)
        {
            //Let default plugin do this
            return Task.FromResult((List<string>)null);
        }

        public Task<bool> IsSupportedUrl(string url)
        {
            Match match = _postPageRegex.Match(url);
            return Task.FromResult(match.Success);
        }

        public Task<bool> ProcessCrawledUrl(ICrawledUrl crawledUrl)
        {
            if (_postPageRegex.Match(crawledUrl.Url).Success)
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}