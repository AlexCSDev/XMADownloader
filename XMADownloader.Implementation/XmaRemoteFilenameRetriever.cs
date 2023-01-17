using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HeyRed.Mime;
using NLog;
using XMADownloader.Implementation.Helpers;
using XMADownloader.Implementation.Interfaces;
using XMADownloader.Implementation.Models;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using System.Net;
using System.IO;
using System.Net.Sockets;
using PuppeteerSharp;

namespace XMADownloader.Implementation
{
    internal class XmaRemoteFilenameRetriever : IRemoteFilenameRetriever
    {
        private HttpClient _httpClient;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private bool _isUseMediaType;
        private int _maxRetries;
        private int _retryMultiplier;

        private readonly Version _httpVersion = HttpVersion.Version20;

        public XmaRemoteFilenameRetriever()
        {

        }

        public async Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            XMADownloaderSettings XMADownloaderSettings = (XMADownloaderSettings)settings;
            _isUseMediaType = XMADownloaderSettings.FallbackToContentTypeFilenames;

            _maxRetries = settings.MaxDownloadRetries;
            _retryMultiplier = settings.RetryMultiplier;

            HttpClientHandler httpClientHandler = new HttpClientHandler();
            if (settings.CookieContainer != null)
            {
                httpClientHandler.UseCookies = true;
                httpClientHandler.CookieContainer = settings.CookieContainer;
            }

            _httpClient = new HttpClient(httpClientHandler);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);
        }

        /// <summary>
        /// Retrieve remote file name
        /// </summary>
        /// <param name="url">File name url</param>
        /// <returns>File name if url is valid, null if url is invalid</returns>
        public async Task<string> GetRemoteFileName(string url, string refererUrl = null)
        {
            return await GetRemoteFileNameInternal(url, refererUrl);
        }

        private async Task<string> GetRemoteFileNameInternal(string url, string refererUrl, int retry = 0, int retryTooManyRequests = 0)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            if (retry > 0)
            {
                if (retry >= _maxRetries)
                {
                    throw new Exception("Retries limit reached");
                }

                await Task.Delay(retry * _retryMultiplier * 1000);
            }

            if (retryTooManyRequests > 0)
                await Task.Delay(retryTooManyRequests * _retryMultiplier * 1000);

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Head, url) { Version = _httpVersion })
                {
                    if (!string.IsNullOrWhiteSpace(refererUrl))
                    {
                        try
                        {
                            request.Headers.Referrer = new Uri(refererUrl);
                        }
                        catch (UriFormatException ex)
                        {
                            _logger.Error(ex, $"[Remote size check] Invalid referer url: {refererUrl}. Error: {ex}");
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
                                    throw new WebException(
                                        $"[Remote size check] Unable to get remote file size as status code is {responseMessage.StatusCode}");
                                case HttpStatusCode.Moved:
                                    string newLocation = responseMessage.Headers.Location.ToString();
                                    _logger.Debug(
                                        $"[Remote size check] {url} has been moved to: {newLocation}, retrying using new url");
                                    return await GetRemoteFileNameInternal(newLocation, refererUrl);
                                case HttpStatusCode.TooManyRequests:
                                    retryTooManyRequests++;
                                    _logger.Debug($"[Remote size check] Too many requests for {url}, waiting for {retryTooManyRequests * _retryMultiplier} seconds...");
                                    return await GetRemoteFileNameInternal(url, refererUrl, 0, retryTooManyRequests);
                            }

                            retry++;

                            _logger.Debug(
                                $"Remote file size check: {url} returned status code {responseMessage.StatusCode}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)...");
                            return await GetRemoteFileNameInternal(url, refererUrl, retry);
                        }

                        string mediaType = null;
                        string filename = null;

                        if (!string.IsNullOrWhiteSpace(responseMessage.Content.Headers.ContentDisposition?.FileName))
                        {
                            filename = responseMessage.Content.Headers.ContentDisposition.FileName.Replace("\"", "");
                            _logger.Debug($"Content-Disposition returned: {filename}");
                        }
                        else if (!string.IsNullOrWhiteSpace(responseMessage.Content.Headers.ContentType?.MediaType) && _isUseMediaType)
                        {
                            mediaType = responseMessage.Content.Headers.ContentType?.MediaType;
                        }

                        if (!string.IsNullOrWhiteSpace(mediaType) && string.IsNullOrWhiteSpace(filename))
                        {
                            filename =
                                $"gen_{HashHelper.ComputeSha256Hash(url).Substring(0, 20)}.{MimeTypesMap.GetExtension(mediaType)}";

                            _logger.Debug($"Content-Disposition and url extraction failed, fallback to Content-Type + hash based name: {filename}");
                        }

                        return filename;
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                retry++;
                _logger.Debug(ex, $"Encountered error while trying to download {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                return await GetRemoteFileNameInternal(url, refererUrl, retry);
            }
            catch (IOException ex)
            {
                retry++;
                _logger.Debug(ex,
                    $"Encountered IO error while trying to access {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                return await GetRemoteFileNameInternal(url, refererUrl, retry);
            }
            catch (SocketException ex)
            {
                retry++;
                _logger.Debug(ex,
                    $"Encountered connection error while trying to access {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                return await GetRemoteFileNameInternal(url, refererUrl, retry);
            }
            catch (HttpRequestException ex)
            {
                retry++;
                _logger.Debug(ex,
                    $"Encountered http request exception while trying to access {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                return await GetRemoteFileNameInternal(url, refererUrl, retry);
            }
            catch (Exception ex)
            {
                throw new WebException($"Unable to download from {url}: {ex.Message}", ex);
            }
        }
    }
}
