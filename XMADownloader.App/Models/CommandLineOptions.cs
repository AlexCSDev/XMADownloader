using CommandLine;
using XMADownloader.App.Enums;
using UniversalDownloaderPlatform.Common.Enums;
using System.Collections.Generic;

namespace XMADownloader.App.Models
{
    class CommandLineOptions
    {
        [Option("url", Required = true, HelpText = "Url of the user page to download")]
        public string Url { get; set; }

        [Option("descriptions", Required = false, HelpText = "Save mod descriptions into a separate html files", Default = false)]
        public bool SaveDescriptions { get; set; }

        [Option("html", Required = false, HelpText = "Save raw html files for search pages and mod pages", Default = false)]
        public bool SaveHtml { get; set; }

        [Option("export-results-json", Required = false, HelpText = "Export crawl/download results into json file", Default = false)]
        public bool ExportCrawlJson { get; set; }

        [Option("download-directory", Required = false, HelpText = "Directory to save all downloaded files in, default: #AppDirectory#/downloads.")]
        public string DownloadDirectory { get; set; }

        [Option("log-level", Required = false, HelpText = "Logging level. Possible options: Default, Debug, Trace. Affects both console and file logging.", Default = LogLevel.Default)]
        public LogLevel LogLevel { get; set; }

        [Option("log-save", Required = false, HelpText = "Create log files in the \"logs\" directory.", Default = false)]
        public bool SaveLogs { get; set; }

        [Option("file-exists-action", Required = false, HelpText = 
            "What to do with files already existing on the disk.\r\nPossible options:\r\n" +
            "BackupIfDifferent: Check remote file size if enabled and available. If it's different, disabled or not available then download remote file and compare it with existing file, create a backup copy of old file if they are different.\r\n" +
            "ReplaceIfDifferent: Same as BackupIfDifferent, but the backup copy of the file will not be created.\r\n" +
            "AlwaysReplace: Always replace existing file. Warning: will result in increased bandwidth usage.\r\n" +
            "KeepExisting: Always keep existing file. The most bandwidth-friendly option.",
            Default = FileExistsAction.BackupIfDifferent)]
        public FileExistsAction FileExistsAction { get; set; }

        [Option("disable-remote-file-size-check", Required = false, 
            HelpText = "Do not ask the server for the file size (if it's available) and do not use it in various pre-download and post-download checks if the file already exists on the disk. Can help get around aggressive rate limits. Warning: will result in increased bandwidth usage if used with --file-exists-action BackupIfDifferent, ReplaceIfDifferent, AlwaysReplace.", 
            Default = false)]
        public bool IsDisableRemoteFileSizeCheck { get; set; }

        [Option("remote-browser-address", Required = false, HelpText = "Advanced users only. Address of the browser with remote debugging enabled. Refer to documentation for more details.")]
        public string RemoteBrowserAddress { get; set; }

        [Option("use-sub-directories", Required = false, HelpText = "Create a new directory inside of the download directory for every post instead of placing all files into a single directory.", Default = true)]
        public bool UseSubDirectories { get; set; }

        [Option("sub-directory-pattern", Required = false, HelpText = "Pattern which will be used to create a name for the sub directories if --use-sub-directories is used. Supported parameters: %ModId%, %PublishedAt%, %PostTitle%.", Default = "[%ModId%] %PublishedAt% %PostTitle%")]
        public string SubDirectoryPattern { get; set; }

        [Option("max-sub-directory-name-length", Required = false, HelpText = "Limits the length of the name for the subdirectories created when --use-sub-directories is used.", Default = 100)]
        public int MaxSubdirectoryNameLength { get; set; }

        [Option("max-filename-length", Required = false, HelpText = "All names of downloaded files will be truncated so their length won't be more than specified value (excluding file extension)", Default = 100)]
        public int MaxFilenameLength { get; set; }

        [Option("filenames-fallback-to-content-type", Required = false, HelpText = "Fallback to using filename generated from url hash if the server returns file content type (extension) and all other methods have failed. Use with caution, this might result in unwanted files being created or the same files being downloaded on every run under different names.", Default = false)]
        public bool FilenamesFallbackToContentType { get; set; }

        [Option("proxy-server-address", Required = false, HelpText = "The address of proxy server to use in the following format: [<proxy-scheme>://]<proxy-host>[:<proxy-port>]. Supported protocols: http(s), socks4, socks4a, socks5.")]
        public string ProxyServerAddress { get; set; }

        [Option("download-urls-in-description", Required = false, HelpText = "Scrapes the description text for urls and downloads the files found in that url", Default = false)]
        public bool DownloadUrlsInDescription { get; set; }

        [Option("download-urls-in-filestab", Required = false, HelpText = "Download all of the files in the filetab", Default = false)]
        public bool DownloadUrlsInFilesTab { get; set; }

        [Option("download-mod-image", Required = false, HelpText = "Download the cover image for the mod", Default = true)]
        public bool DownloadModImage { get; set; }

        [Option("content-type", Required = false, HelpText = "1 = Both, 2 = SFW only, 3 = NSFW only", Default = 1)]
        public int ContentType { get; set; }

        /// <summary>
        /// 1 = Gear mods
        /// 2 = Body replacement mods
        /// 3 = Face mods
        /// 4 = Hair mods
        /// 5 = Shaders
        /// 6 = Other mods
        /// 7 = Minion mods
        /// 8 = Mount mods
        /// 10 = Skin mods
        /// 11 = Concept matrix pose
        /// 12 = Racial scaling mods
        /// 13 = Anamnesis pose
        /// 14 = VFX
        /// 15 = Animation
        /// 16 = Sound
        /// </summary>
        [Option("types", Required = false, HelpText = "Choose the modtypes you want to search for\r\nExample: --types 1 6 16\r\n1 = Gear mods\r\n2 = Body replacement mods\r\n3 = Face mods\r\n4 = Hair mods\r\n5 = Shaders\r\n6 = Other mods\r\n7 = Minion mods\r\n8 = Mount mods\r\n10 = Skin mods\r\n11 = Concept matrix pose\r\n12 = Racial scaling mods\r\n13 = Anamnesis pose\r\n14 = VFX\r\n15 = Animation\r\n16 = Sound")]
        public IEnumerable<int> ModTypes { get; set; }
    }
}
