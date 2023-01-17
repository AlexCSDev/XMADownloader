using System;
using System.Collections.Generic;
using System.Text;
using Ninject;
using Ninject.Modules;
using XMADownloader.Engine;
using XMADownloader.Implementation.Interfaces;
using XMADownloader.Implementation.Models;
using XMADownloader.PuppeteerEngine;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.Common.Interfaces.Plugins;
using UniversalDownloaderPlatform.DefaultImplementations;
using UniversalDownloaderPlatform.DefaultImplementations.Interfaces;
using UniversalDownloaderPlatform.DefaultImplementations.Models;

namespace XMADownloader.Implementation
{
    public class XMADownloaderModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IRemoteFileSizeChecker>().To<RemoteFileSizeChecker>().InSingletonScope();
            Bind<IWebDownloader>().To<XmaWebDownloader>().InSingletonScope();
            Bind<IRemoteFilenameRetriever>().To<XmaRemoteFilenameRetriever>().InSingletonScope();
            Bind<ICrawlTargetInfoRetriever>().To<XmaCrawlTargetInfoRetriever>().InSingletonScope();
            Bind<ICrawledUrlProcessor>().To<XmaCrawledUrlProcessor>().InSingletonScope();
            Bind<IPageCrawler>().To<XmaPageCrawler>().InSingletonScope();
            Bind<IPlugin>().To<XmaDefaultPlugin>().WhenInjectedInto<IPluginManager>();
            Bind<IUniversalDownloaderPlatformSettings>().To<XMADownloaderSettings>();
            Bind<ICookieValidator>().To<XmaCookieValidator>().InSingletonScope();
        }
    }
}
