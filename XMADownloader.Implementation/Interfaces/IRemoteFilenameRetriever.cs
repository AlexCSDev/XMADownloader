using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace XMADownloader.Implementation.Interfaces
{
    interface IRemoteFilenameRetriever
    {
        /// <summary>
        /// Initialization function, called on every XMADownloader.Download call
        /// </summary>
        /// <returns></returns>
        Task BeforeStart(IUniversalDownloaderPlatformSettings settings);
        Task<string> GetRemoteFileName(string url, string refererUrl = null);
    }
}
