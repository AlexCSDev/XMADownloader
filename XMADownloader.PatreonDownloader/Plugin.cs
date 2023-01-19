using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Ninject;
using NLog;
using System;
using System.Text.RegularExpressions;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.Common.Interfaces.Plugins;
using UniversalDownloaderPlatform.DefaultImplementations.Models;

namespace XMADownloader.PatreonDownloader
{
    public class Plugin : IPlugin
    {
        public string Name => "Patreon Downloader (anonymous)";
        public string Author => "Aleksey Tsutsey";
        public string ContactInformation => "https://github.com/AlexCSDev/XMADownloader";

        private readonly static Regex _postPageRegex = new Regex("https:\\/\\/(?>www\\.)patreon.com\\/posts\\/([0-9]+)");

        private IUniversalDownloaderPlatformSettings _settings;

        private PatreonDownloader _patreonDownloader;

        public void OnLoad(IDependencyResolver dependencyResolver)
        {
            IWebDownloader webDownloader = dependencyResolver.Get<IWebDownloader>();
            _patreonDownloader = new PatreonDownloader(webDownloader);
        }

        public Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _settings = settings;

            return Task.CompletedTask;
        }

        public async Task Download(ICrawledUrl crawledUrl)
        {
            try
            {
                Match match = _postPageRegex.Match(crawledUrl.Url);
                await _patreonDownloader.DownloadUrlAsync(Convert.ToInt64(match.Groups[1].Value), Path.Combine(_settings.DownloadDirectory, crawledUrl.DownloadPath));
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