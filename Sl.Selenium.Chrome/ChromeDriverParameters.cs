using System;
using System.Collections.Generic;
using System.Text;

namespace Sl.Selenium.Extensions.Chrome
{
    public class ChromeDriverParameters
    {
        public ISet<string> DriverArguments { get; set; }
        public ISet<string> ExcludedArguments { get; set; }
        public string ProfileName { get; set; }
        public bool Headless { get; set; }
        public TimeSpan Timeout { get; set; }
    }
}
