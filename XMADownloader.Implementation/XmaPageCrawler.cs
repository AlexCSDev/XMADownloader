using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using XMADownloader.Implementation.Enums;
using XMADownloader.Implementation.Models;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Events;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.DefaultImplementations.Models;
using HtmlAgilityPack;
using PuppeteerSharp;
using System.Net.Mail;
using System.Web;
using System.Text.RegularExpressions;
using ConcurrentCollections;
using UniversalDownloaderPlatform.Common.Exceptions;
using System.Net;

namespace XMADownloader.Implementation
{
    internal sealed class XmaPageCrawler : IPageCrawler
    {       
        private const string CrawlStartUrl = "https://xivmodarchive.com/search?sortby=time_posted&sortorder=desc&types=1%2C3%2C7%2C9%2C12%2C15%2C2%2C4%2C8%2C10%2C14%2C11%2C5%2C13%2C6";
        private static Regex _modPageUrlMatchRegex = new Regex("https:\\/\\/(?>www\\.)?xivmodarchive\\.com\\/(modid|private)\\/([a-z\\-0-9]+)(\\/.+)?");

        private readonly XmaWebDownloader _webDownloader;
        private readonly IPluginManager _pluginManager;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private XMADownloaderSettings _XMADownloaderSettings;

        //represents "global" hash list of already parsed urls because we can't have ref variables in async methods
        //we need it to not end up in situation with endless loop of parsing mods referencing each other
        private ConcurrentHashSet<string> _parsedUrls;

        public event EventHandler<PostCrawlEventArgs> PostCrawlStart;
        public event EventHandler<PostCrawlEventArgs> PostCrawlEnd; 
        public event EventHandler<NewCrawledUrlEventArgs> NewCrawledUrl;
        public event EventHandler<CrawlerMessageEventArgs> CrawlerMessage;

        public XmaPageCrawler(IWebDownloader webDownloader, IPluginManager pluginManager)
        {
            _webDownloader = (XmaWebDownloader)webDownloader ?? throw new ArgumentNullException(nameof(webDownloader));
            _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
        }

        public async Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _XMADownloaderSettings = (XMADownloaderSettings) settings;

            _parsedUrls = new ConcurrentHashSet<string>();
        }

        public async Task<List<ICrawledUrl>> Crawl(ICrawlTargetInfo crawlTargetInfo)
        {
            XmaCrawlTargetInfo xmaCrawlTargetInfo = (XmaCrawlTargetInfo)crawlTargetInfo;
            if (xmaCrawlTargetInfo.Id < 1)
                throw new ArgumentException("Campaign ID cannot be less than 1");
            if (string.IsNullOrEmpty(xmaCrawlTargetInfo.Name))
                throw new ArgumentException("Campaign name cannot be null or empty");

            _logger.Debug($"Starting crawling campaign {xmaCrawlTargetInfo.Name}");
            List<ICrawledUrl> crawledUrls = new List<ICrawledUrl>();
            Random rnd = new Random(Guid.NewGuid().GetHashCode());

            string basePageUrl = CrawlStartUrl + $"&author=id-{xmaCrawlTargetInfo.Id}&page=";

            int page = 0;
            while (true)
            {
                page++;
                _logger.Debug($"Page #{page}");
                string searchPageHtml = await _webDownloader.DownloadString(basePageUrl + page);

                if(_XMADownloaderSettings.SaveHtml)
                {
                    string path = Path.Combine(_XMADownloaderSettings.DownloadDirectory, xmaCrawlTargetInfo.Id.ToString(), $"page_{page}.html");
                    if(!Directory.Exists(path))
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                    await File.WriteAllTextAsync(path, searchPageHtml);
                }

                List<XmaCrawledUrl> foundUrls = await ParseSearchPage(searchPageHtml);

                if (foundUrls.Count > 0)
                    crawledUrls.AddRange(foundUrls);
                else
                    break;

                await Task.Delay(500 * rnd.Next(1, 3)); //0.5 - 1 second delay
            }

            _logger.Debug("Finished crawl");

            return crawledUrls;
        }

        private async Task<List<XmaCrawledUrl>> ParseSearchPage(string html)
        {
            List<XmaCrawledUrl> crawledUrls = new List<XmaCrawledUrl>();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNodeCollection modCardUrlNodeCollection = doc.DocumentNode.SelectNodes("//div[contains(@class,\"mod-card\")]/a");
            if (modCardUrlNodeCollection != null)
            {
                foreach (var modCardUrlNode in modCardUrlNodeCollection)
                {
                    if (modCardUrlNode.Attributes.Count == 0 || !modCardUrlNode.Attributes.Contains("href"))
                        continue;

                    string id = modCardUrlNode.Attributes["href"].Value.Replace("/modid/","");
                    crawledUrls.AddRange(await ParseModPage(id));
                }
            }

            return crawledUrls;
        }

        /// <summary>
        /// Parse mod page, does recursive parsing of all mod pages found in the description
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<List<XmaCrawledUrl>> ParseModPage(string id, bool isPrivate = false)
        {
            List<XmaCrawledUrl> crawledUrls = new List<XmaCrawledUrl>();
            HashSet<string> parsedUrlsForThisMod = new HashSet<string>();

            OnPostCrawlStart(new PostCrawlEventArgs(id, true));

            string html = null;

            try
            {
                string url = $"https://xivmodarchive.com/{(isPrivate ? "private" : "modid")}/{id}";
                _parsedUrls.Add(url); //add mod to parsed urls list so other mods referencing it won't try to parse it again

                html = await _webDownloader.DownloadString(url);
            }
            catch(DownloadException ex)
            {
                if(ex.StatusCode == HttpStatusCode.NotFound)
                {
                    OnPostCrawlEnd(new PostCrawlEventArgs(id, false, $"Mod with id {id} ({(isPrivate ? "private" : "public")}) was not found (deleted or wrong url)"));
                    return crawledUrls;
                }
                else if(ex.StatusCode == HttpStatusCode.Forbidden)
                {
                    OnPostCrawlEnd(new PostCrawlEventArgs(id, false, $"Access denied to mod with id {id} ({(isPrivate ? "private" : "public")})"));
                    return crawledUrls;
                }

                throw;
            }

            //todo
            /*if (_XMADownloaderSettings.SaveHtml)
            {
                string path = Path.Combine(_XMADownloaderSettings.DownloadDirectory, xmaCrawlTargetInfo.Id.ToString(), $"page_{page}.html");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                await File.WriteAllTextAsync(Path.Combine(path, html);
            }*/

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//div[contains(@id,\"modpage-frame-left\")]/div/div[contains(@class,\"row\")]/div[contains(@class,\"col-9\")]/h1[contains(@class,\"display-5\")]");
            if (titleNode == null)
                throw new Exception("Title node not found!");

            string modName = HttpUtility.HtmlDecode(titleNode.InnerText.Trim());

            HtmlNode userLinkNode = doc.DocumentNode.SelectSingleNode("//a[contains(@class,\"user-card-link\")]");
            if (userLinkNode == null)
                throw new Exception("User link node not found!");
            long userId = Convert.ToInt64(userLinkNode.Attributes["href"].Value.Replace("/user/", ""));

            HtmlNode filesListNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,\"tab-content\")]/div[@id=\"files\"]/div");
            if (filesListNode == null)
                throw new Exception("Files list node not found!");

            HtmlNode primaryUrlNode = filesListNode.SelectSingleNode("div[contains(@class,\"primary-download-listing\")]/a");
            if (primaryUrlNode == null)
                throw new Exception("Primary download node not found!");

            HtmlNodeCollection additionalUrlNodes = filesListNode.SelectNodes("ul/li");
            if (additionalUrlNodes == null)
                _logger.Debug($"[{id}] No additional download nodes");

            HtmlNode descriptionNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,\"tab-content\")]/div[@id=\"info\"]");
            if (descriptionNode == null)
                throw new Exception($"[{id}] Description node not found!");

            string currentUrl = await _webDownloader.GetActualUrl(primaryUrlNode.Attributes["href"].Value);

            if (!_parsedUrls.Contains(currentUrl))
            {
                _parsedUrls.Add(currentUrl);
                parsedUrlsForThisMod.Add(currentUrl);
                _logger.Debug($"[{id}] New primary url: {currentUrl}");
            }

            if(additionalUrlNodes != null)
            {
                foreach (HtmlNode node in additionalUrlNodes)
                {
                    HtmlNode urlNode = node.SelectSingleNode("a");
                    if (urlNode == null)
                        throw new Exception($"Unable to get url node for one of the additional download nodes");

                    currentUrl = await _webDownloader.GetActualUrl(urlNode.Attributes["href"].Value);
                    if (!_parsedUrls.Contains(currentUrl))
                    {
                        _parsedUrls.Add(currentUrl);
                        parsedUrlsForThisMod.Add(currentUrl);
                        _logger.Debug($"[{id}] New additional url: {currentUrl}");
                    }
                }
            }

            //External urls via plugins (including direct via default plugin)
            List<string> pluginUrls = await _pluginManager.ExtractSupportedUrls(descriptionNode.InnerHtml);
            foreach (string url in pluginUrls)
            {
                currentUrl = await _webDownloader.GetActualUrl(url);
                if (!_parsedUrls.Contains(currentUrl))
                {
                    _parsedUrls.Add(currentUrl);
                    parsedUrlsForThisMod.Add(currentUrl);
                    _logger.Debug($"[{id}] New external entry: {currentUrl}");
                }
            }

            foreach(string url in parsedUrlsForThisMod)
            {
                Match modPageMatch = _modPageUrlMatchRegex.Match(url);
                if (modPageMatch.Success && !modPageMatch.Groups[3].Success)
                {
                    _logger.Debug($"{url} is a mod page");
                    crawledUrls.AddRange(await ParseModPage(modPageMatch.Groups[2].Value, modPageMatch.Groups[1].Value == "private"));
                    continue;
                }

                XmaCrawledUrl entry = new XmaCrawledUrl
                {
                    ModId = id,
                    Name = modName,
                    UserId = userId,
                    PublishedAt = DateTime.MinValue, //todo
                    UpdatedAt = DateTime.MinValue, //todo
                };

                entry.Url = url;
                crawledUrls.Add(entry);
                OnNewCrawledUrl(new NewCrawledUrlEventArgs((CrawledUrl)entry.Clone()));
            }

            OnPostCrawlEnd(new PostCrawlEventArgs(id, true));

            return crawledUrls;
        }

        private void OnPostCrawlStart(PostCrawlEventArgs e)
        {
            EventHandler<PostCrawlEventArgs> handler = PostCrawlStart;
            handler?.Invoke(this, e);
        }

        private void OnPostCrawlEnd(PostCrawlEventArgs e)
        {
            EventHandler<PostCrawlEventArgs> handler = PostCrawlEnd;
            handler?.Invoke(this, e);
        }

        private void OnNewCrawledUrl(NewCrawledUrlEventArgs e)
        {
            EventHandler<NewCrawledUrlEventArgs> handler = NewCrawledUrl;
            handler?.Invoke(this, e);
        }

        private void OnCrawlerMessage(CrawlerMessageEventArgs e)
        {
            EventHandler<CrawlerMessageEventArgs> handler = CrawlerMessage;
            handler?.Invoke(this, e);
        }
    }
}
