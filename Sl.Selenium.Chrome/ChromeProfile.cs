using System;
using System.Collections.Generic;
using System.Text;

namespace Sl.Selenium.Extensions.Chrome
{
    public class ChromeProfile
    {
        public ChromeProfile(string FriendlyName, string ActualName, string FullFolderPath)
        {
            this.FriendlyName = FriendlyName;
            this.ActualName = ActualName;
            this.FullFolderPath = FullFolderPath;
        }
        public string FriendlyName { get; }

        public string ActualName { get; }
        public string FullFolderPath { get; }
    }

    /// <summary>
    /// json model of chrome preferences
    /// </summary>
    public class ChromePreferencesModel
    {
        public ChromeProfileModel profile { get; set; }
    }

    public class ChromeProfileModel
    {
        public string name { get; set; }
    }
}
