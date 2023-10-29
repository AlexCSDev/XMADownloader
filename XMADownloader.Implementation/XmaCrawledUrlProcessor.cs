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
using System.Net;
using System.Threading;
using UniversalDownloaderPlatform.DefaultImplementations.Interfaces;
using UniversalDownloaderPlatform.DefaultImplementations;
using XMADownloader.Common.Models;

namespace XMADownloader.Implementation
{
    class XmaCrawledUrlProcessor : ICrawledUrlProcessor
    {
        private static readonly HashSet<char> _invalidFilenameCharacters;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private ConcurrentDictionary<string, int> _fileCountDict; //file counter for duplicate check
        private XmaDownloaderSettings _xmaDownloaderSettings;

        static XmaCrawledUrlProcessor()
        {
            _invalidFilenameCharacters = new HashSet<char>(Path.GetInvalidFileNameChars());
            _invalidFilenameCharacters.Add(':');
        }

        public XmaCrawledUrlProcessor()
        {

        }

        public async Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _fileCountDict = new ConcurrentDictionary<string, int>();
            _xmaDownloaderSettings = (XmaDownloaderSettings) settings;
        }

        public async Task<bool> ProcessCrawledUrl(ICrawledUrl udpCrawledUrl)
        {
            XmaCrawledUrl crawledUrl = (XmaCrawledUrl)udpCrawledUrl;
            _logger.Info("HERR "+crawledUrl.Name);

            string filename = "";

            if (!crawledUrl.IsProcessedByPlugin)
            {
                if (!_xmaDownloaderSettings.IsUseSubDirectories)
                    filename = $"{crawledUrl.ModId}_";
                else
                    filename = "";

                if (crawledUrl.Filename == null)
                    throw new DownloadException($"[{crawledUrl.ModId}] No filename for {crawledUrl.Url}!");

                
                string extension = Path.GetExtension(crawledUrl.Filename);
                _logger.Info("HERR " + extension);

                //If the downloaded file is an image rename it to the mods name
                if (extension == ".jpg" || extension == ".png" || extension == ".jpeg")
                {
                    filename = crawledUrl.Name + extension;
                }
                else //Else we set it to the orginal filename
                    filename = crawledUrl.Filename;

                _logger.Debug($"Sanitizing filename: {filename}");
                filename = PathSanitizer.SanitizePath(filename);
                _logger.Debug($"Sanitized filename: {filename}");


                if (filename.Length > _xmaDownloaderSettings.MaxFilenameLength)
                {
                    _logger.Debug($"Filename is too long, will be truncated: {filename}");
                    if (extension.Length > 4)
                    {
                        _logger.Warn($"File extension for file {filename} is longer 4 characters and won't be appended to truncated filename!");
                        extension = "";
                    }
                    filename = filename.Substring(0, _xmaDownloaderSettings.MaxFilenameLength) + extension;
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

            //if (_xmaDownloaderSettings.IsUseSubDirectories)
            //    downloadDirectory = Path.Combine(downloadDirectory, PostSubdirectoryHelper.CreateNameFromPattern(crawledUrl, _xmaDownloaderSettings.SubDirectoryPattern, _xmaDownloaderSettings.MaxSubdirectoryNameLength));
            //_logger.Debug(crawledUrl.DownloadPath);
            crawledUrl.DownloadPath = !crawledUrl.IsProcessedByPlugin ? Path.Combine(downloadDirectory, filename) : downloadDirectory + Path.DirectorySeparatorChar;

            _logger.Info("END " + filename);
            return true;
        }
    }
}
