using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using Selenium.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Sl.Selenium.Extensions.Firefox
{
    public class SlFirefoxDriver : SlDriver
    {
        private SlFirefoxDriver(string ProfileName, bool Headless)
            : base(SlDriverBrowserType.Firefox, ProfileName, Headless)
        {

        }



        public static SlDriver Instance(String ProfileName, bool Headless = false)
        {
            if (!_openDrivers.IsOpen(SlDriverBrowserType.Firefox, ProfileName))
            {
                SlFirefoxDriver ffDriver = new SlFirefoxDriver(ProfileName, Headless);

                _openDrivers.OpenDriver(ffDriver);
            }

            return _openDrivers.GetDriver(SlDriverBrowserType.Firefox, ProfileName);
        }


        public override string DriversFolderName()
        {
            return "GeckoDrivers";
        }

        public override string DriverName()
        {
            string gd = "geckodriver" + GeckoDriverGithubKey();
            if (Platform.CurrentOS == OperatingSystemType.Windows)
                return gd + ".exe";

            return gd;
        }


        private readonly static string[] ProcessNames = { "geckodriver", "firefox" };
        /// <summary>
        /// Use with caution. It will kill all running spiders
        /// </summary>
        public static void KillAllFirefoxProcesses()
        {
            foreach (var name in ProcessNames)
            {
                foreach (var process in Process.GetProcessesByName(name))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        //ignore errors
                    }
                }
            }

            SlDriver.ClearDrivers(SlDriverBrowserType.Firefox);
        }


        private static string GeckoDriverGithubKey()
        {
            switch (Platform.CurrentOS)
            {
                case OperatingSystemType.Windows:
                    return Environment.Is64BitOperatingSystem ? "win64" : "win32";
                case OperatingSystemType.OSX:
                    return "macos";
                case OperatingSystemType.Linux:
                    return Environment.Is64BitOperatingSystem ? "linux64" : "linux32";
                default:
                    throw new Exception("Cannot download geckodriver. Unknown operating system.");
            }

        }

        protected override void DownloadLatestDriver()
        {
            using (WebClient client = new WebClient())
            {
                string htmlStr = client.DownloadString("https://github.com/mozilla/geckodriver/releases/latest");

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlStr);

                string browserKey = GeckoDriverGithubKey();



                var anchor = htmlDoc.QuerySelector($"a[href*='/releases/download'][href*='{browserKey}']:not([href*='.asc'])");

                string href = "https://github.com" + anchor.GetAttributeValue("href", null);

                

                string extension = href.EndsWith(".zip") ? ".zip" : ".tar.gz";

                Directory.CreateDirectory(DriversFolderPath());

                string compressedFilePath = DriverPath() + extension;

                client.DownloadFile(href, compressedFilePath);


                var filesBefore = Directory.GetFiles(DriversFolderPath());

                if (extension == ".zip")
                {
                    ZipFile.ExtractToDirectory(compressedFilePath, DriversFolderPath());
                }
                else
                {

                    Stream inStream = System.IO.File.OpenRead(compressedFilePath);
                    Stream gzipStream = new GZipInputStream(inStream);

                    TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream);

                    tarArchive.ExtractContents(DriversFolderPath());
                    tarArchive.Close();
                }



                var filesAfter = Directory.GetFiles(DriversFolderPath());


                var extractedGeckoDriver = filesAfter.First(f => !filesBefore.Contains(f));


                File.Delete(compressedFilePath);
                File.Move(extractedGeckoDriver, DriverPath());

            }

        }

        protected override RemoteWebDriver createBaseDriver()
        {
            FirefoxDriverService service = FirefoxDriverService
                .CreateDefaultService(DriversFolderPath(), DriverName());

            service.Host = "127.0.0.1";

            service.SuppressInitialDiagnosticInformation = true;

            FirefoxOptions options = new FirefoxOptions()
            {
                Profile = GetFirefoxProfile(this.ProfileName),
            };


            options.Profile.SetPreference("dom.webdriver.enabled", false);
            options.Profile.SetPreference("dom.navigator.webdriver", false);
            options.Profile.SetPreference("useAutomationExtension", false);

            if (this.Headless)
            {
                options.AddArguments("--headless");
                //TODO: disable images
            }

            try
            {
                var driver = new FirefoxDriver(service, options, new TimeSpan(0, 1, 0));
                return driver;
            }
            catch (Exception exc)
            {
                throw new DriverCreationException("Error creating driver. See inner exception for details: ", exc);
            }
        }



        private static FirefoxProfile GetProfileRaw(string ProfileName)
        {
            if (Platform.CurrentOS == OperatingSystemType.Windows)
            {
                return new FirefoxProfileManager().GetProfile(ProfileName);
            }

            var profileDirs = Directory.GetDirectories(UnixProfilesFolder);

            foreach (var dir in profileDirs)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(dir);

                if (!dirInfo.Name.Contains("."))
                {
                    //firefox profile folders should contain a dot
                    continue;
                }


                string name = string.Join(".", dirInfo.Name.Split('.').Skip(1));

                if (name == ProfileName)
                {
                    if(Platform.CurrentOS == OperatingSystemType.Linux)
                    {
                        var lockFile = dirInfo.GetFiles("lock").FirstOrDefault();

                        if(lockFile != null && lockFile.Exists)
                        {
                            lockFile.Delete();
                        }
                    }
                    
                    return new FirefoxProfile(dir);
                }
            }

            return new FirefoxProfileManager().GetProfile(ProfileName);
        }


        public static FirefoxProfile GetFirefoxProfile(string ProfileName)
        {
            var profile = GetProfileRaw(ProfileName);

            if(profile == null)
            {
                profile = new FirefoxProfile();
            }

            if (GetAllowedMimeTypesForDownload().Any())
            {

                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);


                profile.SetPreference("browser.download.folderList", 2);
                //profile.SetPreference("browser.download.manager.showWhenStarting", false);
                profile.SetPreference("browser.download.dir", desktopPath);


                var str = string.Join(",", GetAllowedMimeTypesForDownload());
                profile.SetPreference("browser.helperApps.neverAsk.saveToDisk", str);
            }

            profile.SetPreference("dom.webdriver.enabled", false);

            profile.SetPreference("devtools.jsonview.enabled", false);
            profile.SetPreference("useAutomationExtension", false);


            return profile;
        }

        private static string UnixProfilesFolder
        {
            get
            {
                var uProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (Platform.CurrentOS == OperatingSystemType.OSX)
                {
                    return $"{uProfile}/Library/Application Support/Firefox/Profiles/";
                }
                else if (Platform.CurrentOS == OperatingSystemType.Linux)
                {
                    return $"{uProfile}/.mozilla/firefox/";
                }
                else
                {
                    return "";
                }
            }
        }

        public static IList<string> GetInstalledFirefoxProfiles()
        {

            if (Platform.CurrentOS == OperatingSystemType.Windows)
            {
                var pManager = new FirefoxProfileManager();
                return pManager.ExistingProfiles;
            }
            else
            {                
                var profileDirs = Directory.GetDirectories(UnixProfilesFolder);

                
                var toBeReturned = new List<string>();
                foreach (var dir in profileDirs)
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(dir);

                    if(!dirInfo.Name.Contains("."))
                    {
                        //firefox profile folders should contain a dot
                        continue;
                    }

                    string name = string.Join(".", dirInfo.Name.Split('.').Skip(1));
                    toBeReturned.Add(name);
                }

                return toBeReturned;
            }
        }
    }
}
