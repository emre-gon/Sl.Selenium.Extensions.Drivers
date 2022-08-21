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
        protected ChromeDriverParameters ChromeDriverParameters { get; set; }
        protected ChromeDriver(ChromeDriverParameters parameters)
            : base(parameters.DriverArguments, parameters.ProfileName, parameters.Headless)
        {
            this.ChromeDriverParameters = parameters;
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
            var parameters = new ChromeDriverParameters()
            {
                DriverArguments = DriverArguments,
                ExcludedArguments = ExcludedArguments,
                Headless = Headless,
                ProfileName = ProfileName
            };

            return Instance(parameters);
        }

        public static SlDriver Instance(ChromeDriverParameters parameters)
        {
            if (!_openDrivers.IsOpen(SlDriverBrowserType.Chrome, parameters.ProfileName))
            {
                ChromeDriver cDriver = new ChromeDriver(parameters);

                _openDrivers.OpenDriver(cDriver);
            }
            return _openDrivers.GetDriver(SlDriverBrowserType.Chrome, parameters.ProfileName);
        }


        private readonly static string[] ProcessNames = { "chrome", "chromedriver" };
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

            SlDriver.ClearDrivers(SlDriverBrowserType.Chrome);
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


            foreach(var excluded in ChromeDriverParameters.ExcludedArguments ?? Enumerable.Empty<string>())
            {
                options.AddExcludedArgument(excluded);
            }

            AddProfileArgumentToBaseDriver(options);



            if (ChromeDriverParameters.Timeout != default)
            {
                return new OpenQA.Selenium.Chrome.ChromeDriver(service, options, ChromeDriverParameters.Timeout);
            }
            else
            {
                return new OpenQA.Selenium.Chrome.ChromeDriver(service, options);
            }
        }


        static Dictionary<string,DateTime> directoryCopiesDuringCurrentRun = new Dictionary<string,DateTime>();

        /// <summary>
        /// Selenium cannot run multiple chrome profiles with the same user-data-dir
        /// When Enabled, the package will copy the default user-data-dir for each profile into a unique folder
        /// and set user-data-dir to that unique folder.
        /// It might (will) lengthen the startup of the chrome.
        /// </summary>
        public static bool ENABLE_MULTI_PROFILE_DIRECTORY_COPY = false;

        protected void AddProfileArgumentToBaseDriver(ChromeOptions options)
        {
            if (ProfileName == "sl_selenium_chrome")
                return;

            string profileFolderPath = null;

            var installedProfiles = GetInstalledChromeProfiles();

            if (this.ProfileName.Contains("/") || this.ProfileName.Contains("\\"))
            {
                profileFolderPath = this.ProfileName;
            }
            else
            {
                var orderedProfiles = installedProfiles.OrderByDescending(f => f.FriendlyName == this.ProfileName)
                    .ThenByDescending(f => f.ActualName == this.ProfileName)
                    .ThenByDescending(f => f.FullFolderPath == this.ProfileName);

                if(orderedProfiles.Any())
                {
                    var first = orderedProfiles.First();
                    profileFolderPath = first.FullFolderPath;
                }
                else
                {
                    profileFolderPath = DefaultProfilesFolder + this.ProfileName;
                }
            }


            string replaced = profileFolderPath.Replace("\\", "/");

            var lastIndex = replaced.LastIndexOf("/");

            var profileName = replaced.Substring(lastIndex + 1);

            string userDataDir = profileFolderPath.Substring(0, lastIndex + 1);



            if (userDataDir == DefaultProfilesFolder && ENABLE_MULTI_PROFILE_DIRECTORY_COPY)
            {
                userDataDir = userDataDir.Substring(0, userDataDir.Length - 1);

                DirectoryInfo dir = new DirectoryInfo(userDataDir);
                DirectoryInfo dir2 = new DirectoryInfo(userDataDir + " sln " + profileName);

                bool copyDir = false;
                if (dir2.Exists)
                {
                    if (directoryCopiesDuringCurrentRun.ContainsKey(dir2.FullName))
                    {
                        copyDir = (DateTime.Now - directoryCopiesDuringCurrentRun[dir2.FullName]).TotalDays > 1;      
                        //do not copy the folder in the same run
                    }
                    else
                    {
                        try
                        {
                            dir2.Delete(true);
                        }
                        catch(UnauthorizedAccessException acc)
                        {
                            //ignore
                        }
                        copyDir = true;
                    }
                }
                else
                {
                    copyDir = true;
                }

                if (copyDir)
                {
                    //Directory.CreateDirectory(dir2.FullName + "\\" + profileName);


                    //CopyAll(new DirectoryInfo(dir + "\\" + profileName), 
                    //    new DirectoryInfo(dir2 + "\\" + profileName));


                    CopyAll(dir, dir2);
                }

                options.AddArgument($"--user-data-dir={dir2.FullName}");

                options.AddArgument($"--profile-directory={profileName}");
            }
            else
            {
                options.AddArgument($"--user-data-dir={userDataDir}");

                var profileDir = profileFolderPath.Replace(DefaultProfilesFolder, "");
                options.AddArgument($"--profile-directory={profileName}");
            }
        }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            if (source.FullName.ToLower() == target.FullName.ToLower())
            {
                return;
            }

            // Check if the target directory exists, if not, create it.
            if (Directory.Exists(target.FullName) == false)
            {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into it's new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                //Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }


        public new void Dispose()
        {



            base.Dispose();
        }


        private static string DefaultProfilesFolder
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
            if (!Directory.Exists(DefaultProfilesFolder))
            {
                return toBeReturned;
            }

            var allDirs = Directory.GetDirectories(DefaultProfilesFolder);


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
