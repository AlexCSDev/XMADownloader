using System;
using System.Net;
using System.Threading.Tasks;

namespace XMADownloader.Common.Interfaces
{
    /// <summary>
    /// Interface for additional implementations of cookie retrievers
    /// </summary>
    public interface ICookieRetriever
    {
        Task<string> GetUserAgent();
        Task<CookieContainer> RetrieveCookies();
    }
}
