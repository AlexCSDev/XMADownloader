using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NLog;
using XMADownloader.Common.Interfaces;
using XMADownloader.PuppeteerEngine;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.DefaultImplementations;
using UniversalDownloaderPlatform.DefaultImplementations.Interfaces;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;

namespace XMADownloader.Implementation
{
    internal class XmaWebDownloader : WebDownloader
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public XmaWebDownloader(IRemoteFileSizeChecker remoteFileSizeChecker) : base(remoteFileSizeChecker)
        {

        }

        public override async Task DownloadFile(string url, string path, string refererUrl = null)
        {
            if (string.IsNullOrWhiteSpace(refererUrl))
                refererUrl = "https://www.xivmodarchive.com"; //set referrer to xivmodarchive.com just in case

            await base.DownloadFile(url, path, refererUrl);
        }

        public override async Task<string> DownloadString(string url, string refererUrl = null)
        {
            if (string.IsNullOrWhiteSpace(refererUrl))
                refererUrl = "https://www.xivmodarchive.com"; //set referrer to xivmodarchive.com just in case

            return await base.DownloadString(url, refererUrl);
        }

        /// <summary>
        /// Send HEAD request and get the actual url if some kind of shortening service is used. Original url will be returned if no shortening was performed. Also append XMA url to urls without domain.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<string> GetActualUrl(string url, string refererUrl = null)
        {
            if (string.IsNullOrWhiteSpace(refererUrl))
                refererUrl = "https://www.xivmodarchive.com"; //set referrer to xivmodarchive.com just in case

            //This is XMA url, fix it and return without additional checks
            if (url.StartsWith("/"))
                return $"https://www.xivmodarchive.com{url}";

            //it would be too slow if we checked every single url, easier to hardcode
            if (!url.ToLowerInvariant().Contains("bit.ly"))
                return url;

            return await GetActualUrlInternal(url, refererUrl);
        }

        private async Task<string> GetActualUrlInternal(string url, string refererUrl, int retry = 0, int retryTooManyRequests = 0)
        {
            if (retry > 0)
            {
                if (retry >= _maxRetries)
                {
                    throw new DownloadException("Retries limit reached");
                }

                await Task.Delay(retry * _retryMultiplier * 1000);
            }

            if (retryTooManyRequests > 0)
                await Task.Delay(retryTooManyRequests * _retryMultiplier * 1000);

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Head, url) { Version = _httpVersion })
                {
                    //Add some additional headers to better mimic a real browser
                    request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
                    request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
                    request.Headers.Add("Cache-Control", "no-cache");
                    request.Headers.Add("DNT", "1");

                    if (!string.IsNullOrWhiteSpace(refererUrl))
                    {
                        try
                        {
                            request.Headers.Referrer = new Uri(refererUrl);
                        }
                        catch (UriFormatException ex)
                        {
                            _logger.Error($"Invalid referer url: {refererUrl}");
                        }
                    }

                    using (HttpResponseMessage responseMessage =
                        await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (!responseMessage.IsSuccessStatusCode)
                        {
                            switch (responseMessage.StatusCode)
                            {
                                case HttpStatusCode.BadRequest:
                                case HttpStatusCode.Unauthorized:
                                case HttpStatusCode.Forbidden:
                                case HttpStatusCode.NotFound:
                                case HttpStatusCode.MethodNotAllowed:
                                case HttpStatusCode.Gone:
                                    throw new DownloadException($"Error status code returned: {responseMessage.StatusCode}",
                                        responseMessage.StatusCode, await responseMessage.Content.ReadAsStringAsync());
                                case HttpStatusCode.Moved:
                                case HttpStatusCode.Found:
                                case HttpStatusCode.SeeOther:
                                case HttpStatusCode.TemporaryRedirect:
                                case HttpStatusCode.PermanentRedirect:
                                    return responseMessage.Headers.Location.ToString();
                                case HttpStatusCode.TooManyRequests:
                                    retryTooManyRequests++;
                                    _logger.Debug(
                                        $"Too many requests for {url}, waiting for {retryTooManyRequests * _retryMultiplier} seconds...");
                                    return await GetActualUrlInternal(url, refererUrl, 0, retryTooManyRequests);
                            }

                            retry++;

                            _logger.Debug(
                                $"{url} returned status code {responseMessage.StatusCode}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)...");
                            return await GetActualUrlInternal(url, refererUrl, retry);
                        }

                        return responseMessage.RequestMessage.RequestUri.ToString();
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                retry++;
                _logger.Debug(ex,
                    $"Encountered timeout error while trying to access {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                return await GetActualUrlInternal(url, refererUrl, retry);
            }
            catch (IOException ex)
            {
                retry++;
                _logger.Debug(ex,
                    $"Encountered IO error while trying to access {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                return await GetActualUrlInternal(url, refererUrl, retry);
            }
            catch (SocketException ex)
            {
                retry++;
                _logger.Debug(ex,
                    $"Encountered connection error while trying to access {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                return await GetActualUrlInternal(url, refererUrl, retry);
            }
            catch (DownloadException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DownloadException($"Unable to retrieve data from {url}: {ex.Message}", ex);
            }
        }
    }
}
