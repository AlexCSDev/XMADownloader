using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NLog;
using XMADownloader.Implementation;
using XMADownloader.Implementation.Enums;
using XMADownloader.Implementation.Interfaces;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.Common.Interfaces.Plugins;
using UniversalDownloaderPlatform.DefaultImplementations.Models;
using Ninject;
using System.Runtime;
using System.Threading;
using UniversalDownloaderPlatform.DefaultImplementations.Interfaces;
using XMADownloader.Implementation.Models;
using XMADownloader.Common.Models;

namespace XMADownloader.Engine
{
    /// <summary>
    /// This is the default download/parsing plugin for all files
    /// This plugin is used when no other plugins are available for url
    /// </summary>
    internal sealed class XmaDefaultPlugin : IPlugin
    {
        private readonly IWebDownloader _webDownloader;
        private readonly IRemoteFileInfoRetriever _remoteFileInfoRetriever;

        private readonly Random _random;
        private SemaphoreSlim _requestThrottlerSemaphore;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public string Name => "Default plugin";

        public string Author => "Aleksey Tsutsey";
        public string ContactInformation => "https://github.com/Megalan/XMADownloader";

        private XmaDownloaderSettings _settings;

        public XmaDefaultPlugin(IWebDownloader webDownloader, IRemoteFileInfoRetriever remoteFileInfoRetriever)
        {
            _webDownloader = webDownloader;
            _remoteFileInfoRetriever = remoteFileInfoRetriever;

            _random = new Random();
            _requestThrottlerSemaphore = new SemaphoreSlim(1, 1);
        }

        public void OnLoad(IDependencyResolver dependencyResolver)
        {
            //do nothing
        }

        public async Task<bool> IsSupportedUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            return await Task.FromResult(true);
        }

        public async Task Download(ICrawledUrl crawledUrl)
        {
            if(crawledUrl == null)
                throw new ArgumentNullException(nameof(crawledUrl));

            await _webDownloader.DownloadFile(crawledUrl.Url, Path.Combine(_settings.DownloadDirectory, crawledUrl.DownloadPath), crawledUrl.FileSize, null); //referer is set in XmaWebDownloader
        }

        public async Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            await _webDownloader.BeforeStart(settings);
            await _remoteFileInfoRetriever.BeforeStart(settings);

            _settings = (XmaDownloaderSettings)settings;
        }

        public async Task<List<string>> ExtractSupportedUrls(string htmlContents)
        {
            List<string> retList = new List<string>(); 
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlContents);

            HtmlNodeCollection linkNodeCollection = doc.DocumentNode.SelectNodes("//a");
            if (linkNodeCollection != null)
            {
                foreach (var linkNode in linkNodeCollection)
                {
                    if (linkNode.Attributes.Count == 0 || !linkNode.Attributes.Contains("href"))
                        continue;

                    var url = linkNode.Attributes["href"].Value;

                    url = url.Replace("&amp;", "&"); //sometimes there are broken links with &amp; instead of &

                    if (IsAllowedUrl(url))
                    {
                        retList.Add(url);
                        _logger.Debug($"Parsed by default plugin (direct): {url}");
                    }
                }
            }

            return retList;
        }

        private bool IsAllowedUrl(string url)
        {
            if (url.StartsWith("/user/"))
                return false;

            if (url.StartsWith("https://mega.nz/"))
            {
                //This should never be called if mega plugin is installed
                _logger.Debug($"Mega plugin not installed, file will not be downloaded: {url}");
                return false;
            }

            return true;
        }

        public async Task<bool> ProcessCrawledUrl(ICrawledUrl udpCrawledUrl)
        {
            XmaCrawledUrl crawledUrl = (XmaCrawledUrl)udpCrawledUrl;
            if (crawledUrl.Url.IndexOf("dropbox.com/", StringComparison.Ordinal) != -1)
            {
                if (!crawledUrl.Url.EndsWith("?dl=1"))
                {
                    if (crawledUrl.Url.EndsWith("?dl=0"))
                        crawledUrl.Url = crawledUrl.Url.Replace("?dl=0", "?dl=1");
                    else
                        crawledUrl.Url = $"{crawledUrl.Url}?dl=1";
                }

                _logger.Trace($"Dropbox url found: {crawledUrl.Url}");
            }

            string refererUrl = "https://www.xivmodarchive.com";

            if (crawledUrl.Url.Contains("patreon.com"))
                refererUrl = "https://www.patreon.com";

            bool isXMAurl = crawledUrl.Url.ToLowerInvariant().Contains("xivmodarchive.com");

            try
            {
                //Throttle XMA requests to 1 url at once + delay
                //because of aggressive rate limiting
                if (isXMAurl)
                {
                    await _requestThrottlerSemaphore.WaitAsync();
                    await Task.Delay(1000 * _random.Next(2, 4));
                }

                (string filename, long fileSize) = await _remoteFileInfoRetriever.GetRemoteFileInfo(crawledUrl.Url, _settings.FallbackToContentTypeFilenames, refererUrl);

                if (filename == null)
                {
                    throw new DownloadException(
                        $"[{crawledUrl.ModId}] Unable to retrieve name for {crawledUrl.Url}");
                }

                crawledUrl.Filename = filename;
                crawledUrl.FileSize = fileSize;
            }
            catch (Exception ex)
            {
                if (ex is DownloadException)
                    throw;

                throw new DownloadException(
                    $"[{crawledUrl.ModId}] Unable to retrieve file info for {crawledUrl.Url} because of exception: {ex}", ex);
            }
            finally
            {
                if (isXMAurl)
                    _requestThrottlerSemaphore.Release();
            }

            return false; //we still want full processing for all crawled urls passed here 
        }
    }
}
