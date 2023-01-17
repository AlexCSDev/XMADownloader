﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NLog;
using XMADownloader.Common.Interfaces;
using XMADownloader.PuppeteerEngine.Wrappers.Browser;
using PuppeteerSharp;

namespace XMADownloader.PuppeteerEngine
{
    public class PuppeteerCookieRetriever : ICookieRetriever, IDisposable
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private IPuppeteerEngine _puppeteerEngine;
        private bool _isHeadlessBrowser;
        private bool _isRemoteBrowser;
        private string _proxyServerAddress;

        /// <summary>
        /// Create new instance of PuppeteerCookieRetriever using remote browser
        /// </summary>
        /// <param name="remoteBrowserAddress">Remote browser address</param>
        public PuppeteerCookieRetriever(Uri remoteBrowserAddress)
        {
            _puppeteerEngine = new PuppeteerEngine(remoteBrowserAddress);
            _isHeadlessBrowser = true;
            _isRemoteBrowser = true;
        }

        /// <summary>
        /// Create new instance of PuppeteerCookieRetriever using internal browser
        /// </summary>
        /// <param name="headlessBrowser">If set to false then the internal browser will be visible</param>
        /// <param name="proxyServerAddress">Address of the proxy server to use (null for no proxy server)</param>
        public PuppeteerCookieRetriever(bool headlessBrowser = true, string proxyServerAddress = null)
        {
            _puppeteerEngine = new PuppeteerEngine(headlessBrowser, proxyServerAddress);
            _isHeadlessBrowser = headlessBrowser;
            _isRemoteBrowser = false;
            _proxyServerAddress = proxyServerAddress;
        }

        private async Task<IWebBrowser> RestartBrowser(bool headless)
        {
            await _puppeteerEngine.CloseBrowser();
            await Task.Delay(1000); //safety first

            _puppeteerEngine = new PuppeteerEngine(headless, _proxyServerAddress);
            return await _puppeteerEngine.GetBrowser();
        }

        private async Task Login()
        {
            _logger.Debug("Retrieving browser");
            IWebBrowser browser = await _puppeteerEngine.GetBrowser();

            IWebPage page = null;
            bool loggedIn = false;
            do
            {
                if (page == null || page.IsClosed)
                    page = await browser.NewPageAsync();

                _logger.Debug("Checking login status");
                IWebResponse response = await page.GoToAsync("https://www.xivmodarchive.com/dashboard");
                if (response.Status == HttpStatusCode.Unauthorized || response.Status == HttpStatusCode.Forbidden)
                {
                    _logger.Debug("We are NOT logged in, opening login page");
                    if (_isRemoteBrowser)
                    {
                        await page.CloseAsync();
                        throw new Exception("You are not logged in into your XMA account in remote browser. Please login and restart XMADownloader.");
                    }
                    if (_puppeteerEngine.IsHeadless)
                    {
                        _logger.Debug("Puppeteer is in headless mode, restarting in full mode");
                        browser = await RestartBrowser(false);
                        page = await browser.NewPageAsync();
                    }

                    await page.GoToAsync("https://www.xivmodarchive.com/login");

                    //todo: use another page? home page loading is pretty slow
                    await page.WaitForRequestAsync(request => request.Url == "https://www.xivmodarchive.com/dashboard");
                }
                else
                {
                    _logger.Debug("We are logged in");
                    if (_puppeteerEngine.IsHeadless != _isHeadlessBrowser)
                    {
                        browser = await RestartBrowser(_isHeadlessBrowser);
                        page = await browser.NewPageAsync();
                    }

                    loggedIn = true;
                }
            } while (!loggedIn);

            await page.CloseAsync();
        }

        public async Task<CookieContainer> RetrieveCookies()
        {
            try
            {
                CookieContainer cookieContainer = new CookieContainer(1000, 100, CookieContainer.DefaultCookieLengthLimit);

                _logger.Debug("Calling login check");
                try
                {
                    await Login();
                }
                catch (Exception ex)
                {
                    _logger.Fatal($"Login error: {ex.Message}");
                    return null;
                }

                _logger.Debug("Retrieving browser");
                IWebBrowser browser = await _puppeteerEngine.GetBrowser();

                _logger.Debug("Retrieving cookies");
                IWebPage page = await browser.NewPageAsync();
                await page.GoToAsync("https://xivmodarchive.com/dashboard");

                CookieParam[] browserCookies = await page.GetCookiesAsync();

                if (browserCookies != null && browserCookies.Length > 0)
                {
                    foreach (CookieParam browserCookie in browserCookies)
                    {
                        _logger.Debug($"Adding cookie: {browserCookie.Name}");
                        Cookie cookie = new Cookie(browserCookie.Name, browserCookie.Value, browserCookie.Path, browserCookie.Domain);
                        cookieContainer.Add(cookie);
                    }
                }
                else
                {
                    _logger.Fatal("No cookies were extracted from browser");
                    return null;
                }

                await page.CloseAsync();

                return cookieContainer;
            }
            catch (TimeoutException ex)
            {
                _logger.Fatal($"Internal operation timed out. Exception: {ex}");
                return null;
            }
        }

        public async Task<string> GetUserAgent()
        {
            IWebBrowser browser = await _puppeteerEngine.GetBrowser();
            return await browser.GetUserAgentAsync();
        }

        public void Dispose()
        {
            _puppeteerEngine?.Dispose();
        }
    }
}
