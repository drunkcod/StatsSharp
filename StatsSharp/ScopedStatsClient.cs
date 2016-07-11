using System.Collections.Generic;
using System.Linq;

namespace StatsSharp
{
	public class ScopedStatsClient : IStatsClient
	{
		readonly IStatsClient inner;
		readonly string prefix;

		public ScopedStatsClient(IStatsClient inner, string prefix) {
			this.inner = inner;
			this.prefix = prefix.EndsWith(".") ? prefix : prefix + '.';
		}

		public void Send(Metric metric) => inner.Send(new Metric(prefix + metric.Name, metric.Value));
		public void Send(IEnumerable<Metric> metrics) => inner.Send(metrics.Select(x => new Metric(prefix + x.Name, x.Value)));
	}
}