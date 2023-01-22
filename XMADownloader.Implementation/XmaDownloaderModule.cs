using System;
using System.Collections.Generic;
using System.Text;
using Ninject;
using Ninject.Modules;
using XMADownloader.Engine;
using XMADownloader.Implementation.Interfaces;
using XMADownloader.Implementation.Models;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.Common.Interfaces.Plugins;
using UniversalDownloaderPlatform.DefaultImplementations;
using UniversalDownloaderPlatform.DefaultImplementations.Interfaces;
using UniversalDownloaderPlatform.DefaultImplementations.Models;
using UniversalDownloaderPlatform.PuppeteerEngine;
using XMADownloader.Common.Models;

namespace XMADownloader.Implementation
{
    public class XMADownloaderModule : NinjectModule
    {
        public override void Load()
        {
            Kernel.Load(new PuppeteerEngineModule());

            Bind<IRemoteFileInfoRetriever>().To<RemoteFileInfoRetriever>().InSingletonScope();
            Bind<IWebDownloader>().To<XmaWebDownloader>().InSingletonScope();
            Bind<ICrawlTargetInfoRetriever>().To<XmaCrawlTargetInfoRetriever>().InSingletonScope();
            Bind<ICrawledUrlProcessor>().To<XmaCrawledUrlProcessor>().InSingletonScope();
            Bind<IPageCrawler>().To<XmaPageCrawler>().InSingletonScope();
            Bind<IPlugin>().To<XmaDefaultPlugin>().WhenInjectedInto<IPluginManager>();
            Bind<IUniversalDownloaderPlatformSettings>().To<XmaDownloaderSettings>();

            Rebind<ICookieValidator>().To<XmaCookieValidator>().InSingletonScope();
            Rebind<ICrawlResultsExporter>().To<XmaCrawlResultsExporter>().InSingletonScope();
        }
    }
}
