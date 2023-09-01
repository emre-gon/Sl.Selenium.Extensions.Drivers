using System.Collections.Generic;

namespace Sl.Selenium.Extensions.Chrome.DriverDownloader
{
	public class Download
	{
		public string platform { get; set; }
		public string url { get; set; }
	}

	public class VersionInfo
	{
		public string version { get; set; }
		public string Revision { get; set; }
		public Dictionary<string, List<Download>> downloads { get; set; }
	}

	public class KnownGoodVersionsWithDownloads
	{
		public string Timestamp { get; set; }
		public List<VersionInfo> Versions { get; set; }
	}
}
