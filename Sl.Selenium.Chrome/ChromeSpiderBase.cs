﻿using Microsoft.Extensions.Logging;
using Selenium.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sl.Selenium.Extensions.Chrome
{
    public abstract class ChromeSpiderBase : SpiderBase
    {
        protected ChromeSpiderBase(ILogger Logger, string BrowserProfile, bool Headless)
            : base(SlDriverBrowserType.Chrome, Logger, new List<string>() { BrowserProfile }, Headless)
        {

        }

        protected ChromeSpiderBase(ILogger Logger, IEnumerable<string> BrowserProfiles, bool Headless)
        : base(SlDriverBrowserType.Chrome, Logger, BrowserProfiles, Headless)
        {
        }


        protected override SlDriver Driver(string Profile)
        {
            return ChromeDriver.Instance(Profile, this.Headless);
        }

    }
}
