using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using XMADownloader.PuppeteerEngine.Wrappers.Browser;

namespace XMADownloader.PuppeteerEngine
{
    public interface IPuppeteerEngine : IDisposable
    {
        bool IsHeadless { get; }
        Task<IWebBrowser> GetBrowser();
        Task CloseBrowser();
    }
}
