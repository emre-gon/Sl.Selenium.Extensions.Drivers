using Microsoft.Extensions.Logging;
using Selenium.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sl.Selenium.Extensions.Firefox
{
    public abstract class FFSpiderBase : SpiderBase
    {
        protected FFSpiderBase(ILogger Logger, string BrowserProfile, bool Headless)
            : base(SlDriverBrowserType.Firefox, Logger, new List<string>() { BrowserProfile }, Headless)
        {

        }

        protected FFSpiderBase(ILogger Logger, IEnumerable<string> BrowserProfiles, bool Headless)
        : base(SlDriverBrowserType.Firefox, Logger, BrowserProfiles, Headless)
        {
        }


        protected override SlDriver Driver(string Profile)
        {
            return FirefoxDriver.Instance(Profile, this.Headless);
        }

    }
}
