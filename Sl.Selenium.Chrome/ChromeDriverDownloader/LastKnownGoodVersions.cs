using System;

namespace Sl.Selenium.Extensions.Chrome.DriverDownloader
{
	public class ChannelInfo
	{
		public string channel { get; set; }
		public string version { get; set; }
		public string revision { get; set; }
	}

	public class Channels
	{
		public ChannelInfo Stable { get; set; }
		public ChannelInfo Beta { get; set; }
		public ChannelInfo Dev { get; set; }
		public ChannelInfo Canary { get; set; }
	}

	public class LastKnownGoodVersions
	{
		public DateTime timestamp { get; set; }
		public Channels channels { get; set; }
	}
}
