﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Helpers;
using XMADownloader.Implementation.Models;

namespace XMADownloader.Implementation
{
    /// <summary>
    /// Helper used to generate name for post subdirectories
    /// </summary>
    internal class PostSubdirectoryHelper
    {
        /// <summary>
        /// Create a sanitized directory name based on supplied name pattern
        /// </summary>
        /// <param name="crawledUrl">Crawled url with published date, post title and post id</param>
        /// <param name="pattern">Pattern for directory name</param>
        /// <param name="lengthLimit">Limit the directory name length to this amount of characters</param>
        /// <returns></returns>
        public static string CreateNameFromPattern(XmaCrawledUrl crawledUrl, string pattern, int lengthLimit)
        {
            string postTitle = crawledUrl.Name?.Trim() ?? "No Title";
            while (postTitle.Length > 1 && postTitle[^1] == '.')
                postTitle = postTitle.Remove(postTitle.Length - 1).Trim();

            string retString = pattern.ToLowerInvariant()
                .Replace("%publishedat%", crawledUrl.PublishedAt.ToString("yyyy-MM-dd"))
                .Replace("%posttitle%", postTitle)
                .Replace("%modid%", crawledUrl.ModId.ToString());

            if (retString.Length > lengthLimit)
                retString = retString.Substring(0, lengthLimit);

            return PathSanitizer.SanitizePath(retString);
        }
    }
}
