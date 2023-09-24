using System;
using System.Collections.Generic;
using System.Text;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Helpers;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.DefaultImplementations.Models;
using UniversalDownloaderPlatform.PuppeteerEngine.Interfaces;

namespace XMADownloader.Common.Models
{
    public record XmaDownloaderSettings : UniversalDownloaderPlatformSettings, IPuppeteerSettings
    {
        public bool SaveDescriptions { get; init; }

        public bool SaveHtml { get; init; }

        /// <summary>
        /// Create a new directory for every post and store files of said post in that directory
        /// </summary>
        public bool IsUseSubDirectories { get; init; }

        /// <summary>
        /// Pattern used to generate directory name if UseSubDirectories is enabled
        /// </summary>
        public string SubDirectoryPattern { get; init; }

        /// <summary>
        /// Subdirectory names will be truncated to this length
        /// </summary>
        public int MaxSubdirectoryNameLength { get; init; }

        /// <summary>
        /// Filenames will be truncated to this length
        /// </summary>
        public int MaxFilenameLength { get; init; } //todo: move this into UDP?

        /// <summary>
        /// Fallback to using sha256 hash and Content-Type for filenames if Content-Disposition fails
        /// </summary>
        public bool FallbackToContentTypeFilenames { get; init; }
        public string LoginPageAddress { get { return "https://www.xivmodarchive.com/login"; } }
        public string LoginCheckAddress { get { return "https://www.xivmodarchive.com/dashboard"; } }
        public string CaptchaCookieRetrievalAddress { get { return null; } }
        public Uri RemoteBrowserAddress { get; init; }
        public bool IsHeadlessBrowser { get; init; }
        public bool ExportCrawlResults { get; set; }

        public XmaDownloaderSettings()
        {
            SaveDescriptions = true;
            SaveHtml = true;
            IsUseSubDirectories = false;
            SubDirectoryPattern = "[%ModId%] %PublishedAt% %PostTitle%";
            FallbackToContentTypeFilenames = false;
            MaxFilenameLength = 100;
            MaxSubdirectoryNameLength = 100;
            IsHeadlessBrowser = true;
        }
    }
}
