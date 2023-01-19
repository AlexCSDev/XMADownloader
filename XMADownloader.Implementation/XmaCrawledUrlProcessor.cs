using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using XMADownloader.Implementation.Enums;
using XMADownloader.Implementation.Interfaces;
using XMADownloader.Implementation.Models;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Helpers;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace XMADownloader.Implementation
{
    class XmaCrawledUrlProcessor : ICrawledUrlProcessor
    {
        private static readonly HashSet<char> _invalidFilenameCharacters;

        private readonly IRemoteFilenameRetriever _remoteFilenameRetriever;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private ConcurrentDictionary<string, int> _fileCountDict; //file counter for duplicate check
        private XMADownloaderSettings _XMADownloaderSettings;
        private static readonly Regex _googleDriveRegex = new Regex("https:\\/\\/drive\\.google\\.com\\/(?:file\\/d\\/|open\\?id\\=|drive\\/folders\\/|folderview\\?id=|drive\\/u\\/[0-9]+\\/folders\\/)([A-Za-z0-9_-]+)");
        private static readonly Regex _googleDocsRegex = new Regex("https:\\/\\/docs\\.google\\.com\\/(?>document|spreadsheets)\\/d\\/([a-zA-Z0-9-_]+)");

        static XmaCrawledUrlProcessor()
        {
            _invalidFilenameCharacters = new HashSet<char>(Path.GetInvalidFileNameChars());
            _invalidFilenameCharacters.Add(':');
        }

        public XmaCrawledUrlProcessor(IRemoteFilenameRetriever remoteFilenameRetriever)
        {
            _remoteFilenameRetriever = remoteFilenameRetriever ??
                                       throw new ArgumentNullException(nameof(remoteFilenameRetriever));

            _logger.Debug("KemonoCrawledUrlProcessor initialized");
        }

        public async Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _fileCountDict = new ConcurrentDictionary<string, int>();
            _XMADownloaderSettings = (XMADownloaderSettings) settings;
            await _remoteFilenameRetriever.BeforeStart(settings);
        }

        public async Task<bool> ProcessCrawledUrl(ICrawledUrl udpCrawledUrl)
        {
            XmaCrawledUrl crawledUrl = (XmaCrawledUrl)udpCrawledUrl;

            string filename = "";

            if (!crawledUrl.IsProcessedByPlugin)
            {
                /*if (!_XMADownloaderSettings.IsUseSubDirectories)
                    filename = $"{crawledUrl.ModId}_";
                else
                    filename = "";*/

                /*switch (crawledUrl.UrlType)
                {
                    case XmaCrawledUrlType.PostFile:
                        filename += "post";
                        break;
                    case XmaCrawledUrlType.PostAttachment:
                        filename += $"attachment";
                        if (!_XMADownloaderSettings.IsUseLegacyFilenaming)
                            filename += $"_{crawledUrl.FileId}";
                        break;
                    case XmaCrawledUrlType.PostMedia:
                        filename += $"media";
                        if (!_XMADownloaderSettings.IsUseLegacyFilenaming)
                            filename += $"_{crawledUrl.FileId}";
                        break;
                    case XmaCrawledUrlType.AvatarFile:
                        filename += "avatar";
                        break;
                    case XmaCrawledUrlType.CoverFile:
                        filename += "cover";
                        break;
                    case XmaCrawledUrlType.ExternalUrl:
                        filename += "external";
                        break;
                    default:
                        throw new ArgumentException($"Invalid url type: {crawledUrl.UrlType}");
                }*/

                if (crawledUrl.Filename == null)
                {
                    _logger.Debug($"No filename for {crawledUrl.Url}, trying to retrieve...");
                    string remoteFilename =
                        await _remoteFilenameRetriever.GetRemoteFileName(crawledUrl.Url, "https://www.xivmodarchive.com");

                    if (remoteFilename == null)
                    {
                        throw new DownloadException(
                            $"[{crawledUrl.ModId}] Unable to retrieve name for {crawledUrl.Url}");
                    }

                    filename = remoteFilename;
                }
                else
                {
                    filename = crawledUrl.Filename;
                }

                _logger.Debug($"Filename for {crawledUrl.Url} is {filename}");

                _logger.Debug($"Sanitizing filename: {filename}");
                filename = PathSanitizer.SanitizePath(filename);
                _logger.Debug($"Sanitized filename: {filename}");

                if (filename.Length > _XMADownloaderSettings.MaxFilenameLength)
                {
                    _logger.Debug($"Filename is too long, will be truncated: {filename}");
                    string extension = Path.GetExtension(filename);
                    if (extension.Length > 4)
                    {
                        _logger.Warn($"File extension for file {filename} is longer 4 characters and won't be appended to truncated filename!");
                        extension = "";
                    }
                    filename = filename.Substring(0, _XMADownloaderSettings.MaxFilenameLength) + extension;
                    _logger.Debug($"Truncated filename: {filename}");
                }

                string key = $"{crawledUrl.ModId}_{filename.ToLowerInvariant()}";

                _fileCountDict.AddOrUpdate(key, 0, (key, oldValue) => oldValue + 1);

                if (_fileCountDict[key] > 1)
                {
                    _logger.Warn($"Found more than a single file with the name {filename} in the same folder in mod {crawledUrl.ModId}, sequential number will be appended to its name.");

                    string appendStr = _fileCountDict[key].ToString();

                    /*if (crawledUrl.UrlType != XmaCrawledUrlType.ExternalUrl)
                    {
                        MatchCollection matches = _fileIdRegex.Matches(crawledUrl.Url);

                        if (matches.Count == 0)
                            throw new DownloadException($"[{crawledUrl.ModId}] Unable to retrieve file id for {crawledUrl.Url}, contact developer!");
                        if (matches.Count > 1)
                            throw new DownloadException($"[{crawledUrl.ModId}] More than 1 media found in URL {crawledUrl.Url}");

                        appendStr = matches[0].Groups[4].Value;
                    }*/

                    filename = $"{Path.GetFileNameWithoutExtension(filename)}_{appendStr}{Path.GetExtension(filename)}";
                }
            }

            string downloadDirectory = crawledUrl.UserId.ToString();

            if (/*_XMADownloaderSettings.IsUseSubDirectories*/true)
                downloadDirectory = Path.Combine(downloadDirectory, PostSubdirectoryHelper.CreateNameFromPattern(crawledUrl, _XMADownloaderSettings.SubDirectoryPattern, _XMADownloaderSettings.MaxSubdirectoryNameLength));

            crawledUrl.DownloadPath = !crawledUrl.IsProcessedByPlugin ? Path.Combine(downloadDirectory, filename) : downloadDirectory + Path.DirectorySeparatorChar;

            return true;
        }
    }
}
