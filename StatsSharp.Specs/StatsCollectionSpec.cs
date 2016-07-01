using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Cone;

namespace StatsSharp.Specs
{
	public class StatsCollection : IStatsClient
	{
		readonly Dictionary<string, MetricValue> gauges = new Dictionary<string, MetricValue>(); 

		public IEnumerable<Metric> Summarize() {
			return gauges.Select(item => new Metric(item.Key, item.Value));
		}

		void IStatsClient.Send(Metric metric) {
			switch(metric.Value.Type) {
				default: throw new NotImplementedException();
				case MetricType.Gauge:
					gauges[metric.Name] = metric.Value;
					break;
			}
		}

		void IStatsClient.Send(IEnumerable<Metric> metrics) {
			foreach(var item in metrics)
				this.Send(item);
		}
	}

	[Describe(typeof(StatsCollection))]
	public class StatsCollectionSpec
	{
		public void Gauge_is_last_seen_value() {
			var stats = new StatsCollection();

			stats.GaugeAbsoluteValue("MyGauge", 1);
			stats.GaugeAbsoluteValue("MyGauge", 2);

			Check.That(() => stats.Summarize().Single(x => x.Name == "MyGauge").Value.Equals(MetricValue.Gauge(2)));
		}
	}
}
