using Newtonsoft.Json;
using Selenium.Extensions;
using System.IO.Compression;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System;
using System.Linq;

namespace Sl.Selenium.Extensions.Chrome.DriverDownloader
{
	public class ChromeDriverDownloader
	{
		private readonly ChromeDriverDownloaderOptions options;

		public ChromeDriverDownloader(ChromeDriverDownloaderOptions options)
		{
			this.options = options;
		}

		public void DownloadLatestDriver()
		{
			Console.WriteLine("Downloading chrome driver");

			using (WebClient client = new WebClient())
			{
				var version = GetLatestStableVersionNumber(client);
				var installer = GetInstallerPathForVersion(version, client);
				DownloadInstaller(installer, client);
			}
		}

		protected string GetLatestStableVersionNumber(WebClient client)
		{
			const string url = "https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions.json";

			string jsonData = client.DownloadString(url);

			var chromeVersions = JsonConvert.DeserializeObject<LastKnownGoodVersions>(jsonData);

			var version = chromeVersions.channels.Stable.version;

			return version;
		}

		protected string GetInstallerPathForVersion(string version, WebClient client)
		{
			const string url = "https://googlechromelabs.github.io/chrome-for-testing/known-good-versions-with-downloads.json";

			string jsonData = client.DownloadString(url);

			var response = JsonConvert.DeserializeObject<KnownGoodVersionsWithDownloads>(jsonData);

			var versionInfo = response.Versions.FirstOrDefault(v => v.version == version);
			if (versionInfo == null)
			{
				throw new Exception($"Version {version} not found on endpoint {url}");
			}

			string requiredPlatform = "";
			switch (Platform.CurrentOS)
			{
				case OperatingSystemType.Windows:
					requiredPlatform = RuntimeInformation.OSArchitecture == Architecture.X64 ? "win64" : "win32";
					break;
				case OperatingSystemType.OSX:
					requiredPlatform = RuntimeInformation.OSArchitecture == Architecture.X64 ? "mac-x64" : "mac-arm64";
					break;
				case OperatingSystemType.Linux:
					requiredPlatform = "linux64";
					break;
				default:
					throw new Exception("Unknown OS: " + Platform.CurrentOS);
			}

			var platformInfo = versionInfo.downloads["chromedriver"].FirstOrDefault(p => p.platform == requiredPlatform);

			if (platformInfo == null)
			{
				throw new Exception($"Could not find installer for version: {version} and platform: {requiredPlatform}");
			}

			return platformInfo.url;
		}

		protected void DownloadInstaller(string installer, WebClient client)
		{
			Directory.CreateDirectory(options.DriversFolderPath);
			string compressedFilePath = options.DriverPath + ".zip";

			client.DownloadFile(installer, compressedFilePath);

			var filesBefore = Directory.GetFiles(options.DriversFolderPath);

			ZipFile.ExtractToDirectory(compressedFilePath, options.DriversFolderPath);

			var directoryAfter = Directory.GetDirectories(options.DriversFolderPath).First();
			var filesAfter = Directory.GetFiles(directoryAfter);

			var extractedChromeDriver = filesAfter.First(f => !filesBefore.Contains(f));

			File.Move(extractedChromeDriver, options.DriverPath);
			File.Delete(compressedFilePath);
			Directory.Delete(directoryAfter, true);
		}
	}
}
