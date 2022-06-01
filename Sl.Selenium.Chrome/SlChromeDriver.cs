using Newtonsoft.Json;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using Selenium.Extensions;
using Sl.Selenium.Extensions.Chrome;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Sl.Selenium.Extensions
{
    public class ChromeDriver : GenSlDriver<OpenQA.Selenium.Chrome.ChromeDriver>
    {
        public ISet<string> ExcludedArguments { get; }
        protected ChromeDriver(ISet<string> DriverArguments, ISet<string> ExcludedArguments, string ProfileName, bool Headless)
            : base(DriverArguments, ProfileName, Headless)
        {
            this.ExcludedArguments = ExcludedArguments ?? new HashSet<string>();
        }

        public static SlDriver Instance(bool Headless = false)
        {
            return Instance("sl_selenium_chrome", Headless);
        }

        public static SlDriver Instance(String ProfileName, bool Headless = false)
        {
            return Instance(new HashSet<string>(), ProfileName, Headless);
        }

        public static SlDriver Instance(ISet<string> DriverArguments, String ProfileName, bool Headless = false)
        {
            return Instance(new HashSet<string>(), new HashSet<string>(), ProfileName, Headless);
        }

        public static SlDriver Instance(ISet<string> DriverArguments, ISet<string> ExcludedArguments, String ProfileName, bool Headless = false)
        {
            if (!_openDrivers.IsOpen(SlDriverBrowserType.Chrome, ProfileName))
            {
                ChromeDriver cDriver = new ChromeDriver(DriverArguments, ExcludedArguments, ProfileName, Headless);

                _openDrivers.OpenDriver(cDriver);
            }

            return _openDrivers.GetDriver(SlDriverBrowserType.Chrome, ProfileName);
        }


        private readonly static string[] ProcessNames = { "chrome" };
        /// <summary>
        /// Use with caution. It will kill all running spiders
        /// </summary>
        public static void KillAllChromeProcesses()
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

        public override string DriversFolderName()
        {
            return "ChromeDrivers";
        }

        public override string DriverName()
        {
            switch (Platform.CurrentOS)
            {
                case OperatingSystemType.Windows:
                    return "chromedriver.exe";
                case OperatingSystemType.OSX:
                    return "chromedriver_mac";
                case OperatingSystemType.Linux:
                    return "chromedriver_linux";
                default:
                    throw new Exception("Unknown OS");
            }
        }

        protected override OpenQA.Selenium.Chrome.ChromeDriver CreateBaseDriver()
        {
            ChromeDriverService service = ChromeDriverService.CreateDefaultService(DriversFolderPath(), DriverName());

            
            service.HostName = "127.0.0.1";

            service.SuppressInitialDiagnosticInformation = true;


            ChromeOptions options = new ChromeOptions();
            if (this.Headless)
            {
                DriverArguments.Add("headless");
            }
            else
            {
                DriverArguments.Remove("headless");
            }

            DriverArguments.Add("--no-default-browser-check");
            DriverArguments.Add("--no-first-run");


            HashSet<string> argumentKeys = new HashSet<string>(DriverArguments.Select(f => f.Split('=')[0]));

            if (!argumentKeys.Contains("--log-level"))
            {
                DriverArguments.Add("--log-level=0");
            }

            foreach(var arg in DriverArguments)
            {
                options.AddArgument(arg);
            }


            foreach(var excluded in ExcludedArguments)
            {
                options.AddExcludedArgument(excluded);
            }

            AddProfileArgumentToBaseDriver(options);

            var driver = new OpenQA.Selenium.Chrome.ChromeDriver(service, options);

            return driver;
        }


        protected void AddProfileArgumentToBaseDriver(ChromeOptions options)
        {
            if (ProfileName == "sl_selenium_chrome")
                return;

            string chromeProfilesFolder = null;
            string chromeProfileName = this.ProfileName;
            if (this.ProfileName.Contains("/") || this.ProfileName.Contains("\\"))
            {
                int lastIndex;
                if (this.ProfileName.Contains("/"))
                {
                    lastIndex = this.ProfileName.LastIndexOf("/");
                }
                else
                {
                    lastIndex = this.ProfileName.LastIndexOf("\\");
                }

                chromeProfilesFolder = this.ProfileName.Substring(0, lastIndex);

                chromeProfileName = this.ProfileName.Substring(lastIndex + 1);


            }
            else
            {
                var profiles = GetInstalledChromeProfiles();

                foreach (var p in profiles)
                {
                    if (p.FriendlyName == this.ProfileName)
                    {
                        chromeProfilesFolder = p.FullFolderPath;
                        chromeProfileName = p.ActualName;
                        break;
                    }
                    else if (p.ActualName == this.ProfileName)
                    {
                        chromeProfilesFolder = p.FullFolderPath;
                        chromeProfileName = p.ActualName;
                        break;
                    }
                    else if (p.FullFolderPath == this.ProfileName)
                    {
                        chromeProfilesFolder = p.FullFolderPath;
                        chromeProfileName = p.ActualName;
                        break;
                    }
                }

                if (chromeProfilesFolder == null)
                {
                    chromeProfilesFolder = ProfilesFolder + this.ProfileName;
                }
            }

            options.AddArgument($"user-data-dir={ProfilesFolder}");

            options.AddArgument($"profile-directory={chromeProfileName}");
            
        }


        private static string ProfilesFolder
        {
            get
            {
                if (Platform.CurrentOS == OperatingSystemType.Windows)
                {
                    var uProfile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    return $@"{uProfile}\Google\Chrome\User Data\";
                }
                else if (Platform.CurrentOS == OperatingSystemType.OSX)
                {
                    var uProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    return $"{uProfile}/Library/Application Support/Google/Chrome/";
                }
                else if (Platform.CurrentOS == OperatingSystemType.Linux)
                {
                    var uProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    return $"{uProfile}/.config/google-chrome/";
                }
                else
                {
                    throw new Exception("Unknown OS");
                }
            }
        }


        public static IList<ChromeProfile> GetInstalledChromeProfiles()
        {
            var toBeReturned = new List<ChromeProfile>();
            if (!Directory.Exists(ProfilesFolder))
            {
                return toBeReturned;
            }

            var allDirs = Directory.GetDirectories(ProfilesFolder);


            foreach(var dir in allDirs)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(dir);
                var prefFile = dirInfo.GetFiles("Preferences", SearchOption.TopDirectoryOnly)
                    .Where(f => f.Name == "Preferences").FirstOrDefault();

                if(prefFile == null)
                {
                    continue;
                }

                var allText = File.ReadAllText(prefFile.FullName);

                try
                {
                    
                    var model = JsonConvert.DeserializeObject<ChromePreferencesModel>(allText);

                    toBeReturned.Add(new ChromeProfile(model.profile.name, dirInfo.Name, dirInfo.FullName));

                }
                catch(Exception exc)
                {
                    continue;
                }
            }

            return toBeReturned;
        }

        protected override void DownloadLatestDriver()
        {
            Console.WriteLine("Downloading chrome driver");
            string chromeRepo = "https://chromedriver.storage.googleapis.com";

            using (WebClient client = new WebClient())
            {
                string latestVersion = client.DownloadString(chromeRepo + "/LATEST_RELEASE");


                string downloadPath;
                switch (Platform.CurrentOS)
                {
                    case OperatingSystemType.Windows:
                        downloadPath = "chromedriver_win32.zip";
                        break;
                    case OperatingSystemType.OSX:
                        downloadPath = "chromedriver_mac64.zip";
                        break;
                    case OperatingSystemType.Linux:
                        downloadPath = "chromedriver_linux64.zip";
                        break;
                    default:
                        throw new Exception("Unknown OS");
                }

                Directory.CreateDirectory(DriversFolderPath());
                string compressedFilePath = DriverPath() + ".zip";


                client.DownloadFile($"{chromeRepo}/{latestVersion}/{downloadPath}", compressedFilePath);



                var filesBefore = Directory.GetFiles(DriversFolderPath());

                ZipFile.ExtractToDirectory(compressedFilePath, DriversFolderPath());


                var filesAfter = Directory.GetFiles(DriversFolderPath());


                var extractedChromeDriver = filesAfter.First(f => !filesBefore.Contains(f));


                File.Delete(compressedFilePath);
                File.Move(extractedChromeDriver, DriverPath());
            }



        }

    }
}
