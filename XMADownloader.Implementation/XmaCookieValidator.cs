using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NLog;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.DefaultImplementations.Interfaces;

namespace XMADownloader.Implementation
{
    internal class XmaCookieValidator : ICookieValidator
    {
        private readonly IWebDownloader _webDownloader;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public XmaCookieValidator(IWebDownloader webDownloader)
        {
            _webDownloader = webDownloader ?? throw new ArgumentNullException(nameof(webDownloader));
        }

        public async Task ValidateCookies(CookieContainer cookieContainer)
        {
            if (cookieContainer == null)
                throw new ArgumentNullException(nameof(cookieContainer));

            CookieCollection cookies = cookieContainer.GetCookies(new Uri("https://www.xivmodarchive.com"));

            if (cookies["connect.sid"] == null)
                throw new CookieValidationException("connect.sid cookie not found");
            /*if (cookies["cf_clearance"] == null)
                throw new CookieValidationException("cf_clearance cookie not found");*/

            string dashboardResponse = await _webDownloader.DownloadString("https://www.xivmodarchive.com/dashboard");

            if (!dashboardResponse.ToLower(CultureInfo.InvariantCulture).Contains("log out"))
                throw new CookieValidationException("User not authorized");
        }
    }
}
