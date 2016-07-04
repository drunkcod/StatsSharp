using System.Collections.Generic;

namespace StatsSharp
{
	public class NullStatsClient : IStatsClient
	{
		NullStatsClient() { }
		public void Send(Metric metric) { }
		public void Send(IEnumerable<Metric> metrics) { } 

		public static NullStatsClient Instance = new NullStatsClient();
	}
}