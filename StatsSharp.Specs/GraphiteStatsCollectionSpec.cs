using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Cone;

namespace StatsSharp.Specs
{
	public class GraphiteStatsCollection : IStatsClient
	{
		readonly Dictionary<string, ulong> gauges = new Dictionary<string, ulong>();
		readonly Dictionary<string, List<ulong>> timers = new Dictionary<string, List<ulong>>();
		readonly Dictionary<string, long> counts = new Dictionary<string, long>(); 

		public IEnumerable<GraphiteValue> Summarize() {
			return Summarize(TimeSpan.FromSeconds(10));
		}

		public IEnumerable<GraphiteValue> Summarize(TimeSpan flushInterval) {
			var ts = DateTime.Now;
			return gauges.Select(item => new GraphiteValue("stats.gauges." + item.Key, item.Value, ts))
				.Concat(SummarizeTimers(ts))
				.Concat(SummarizeCounts(flushInterval, ts));
		}

		private IEnumerable<GraphiteValue> SummarizeCounts(TimeSpan flushInterval, DateTime ts)
		{
			foreach(var counter in counts) {
				yield return new GraphiteValue("stats_counts." + counter.Key, counter.Value, ts);
				yield return new GraphiteValue("stats." + counter.Key, counter.Value / flushInterval.TotalSeconds, ts);
			}
		}

		IEnumerable<GraphiteValue> SummarizeTimers(DateTime ts) {
			foreach(var timer in timers) {
				var items = timer.Value;
				items.Sort();

				var sum = items.Sum(x => (double)x);
				yield return new GraphiteValue("stats.timers." + timer.Key + ".upper", items[items.Count - 1], ts);
				yield return new GraphiteValue("stats.timers." + timer.Key + ".lower", items[0], ts);
				yield return new GraphiteValue("stats.timers." + timer.Key + ".count", items.Count, ts);
				yield return new GraphiteValue("stats.timers." + timer.Key + ".sum", sum, ts);
				yield return new GraphiteValue("stats.timers." + timer.Key + ".mean", sum / items.Count, ts);
			}
		} 

		void IStatsClient.Send(Metric metric) {
			switch(metric.Value.Type) {
				default: throw new NotImplementedException();
				case MetricType.Gauge:
					gauges[metric.Name] = metric.Value.Bits;
					break;
				case MetricType.Counter:
					long count;
					counts.TryGetValue(metric.Name, out count);
					counts[metric.Name] = count + (long)metric.Value.Bits;
					break;
				case MetricType.Time:
					List<ulong> items;
					if(!timers.TryGetValue(metric.Name, out items))
						timers.Add(metric.Name, items = new List<ulong>());
					items.Add(metric.Value.Bits);
					break;
			}
		}

		void IStatsClient.Send(IEnumerable<Metric> metrics) {
			foreach(var item in metrics)
				this.Send(item);
		}
	}

	[Describe(typeof(GraphiteStatsCollection))]
	public class GraphiteStatsCollectionSpec
	{
		public void Gauge_is_last_seen_value() {
			var stats = new GraphiteStatsCollection();

			stats.GaugeAbsoluteValue("MyGauge", 1);
			stats.GaugeAbsoluteValue("MyGauge", 2);

			Check.That(() => stats.Summarize().Single(x => x.Name == "stats.gauges.MyGauge").Value == 2);
		}

		public void Timer_upper_lower_count_sum_mean() {
			var stats = new GraphiteStatsCollection();

			stats.Timer("MyTimer", TimeSpan.FromMilliseconds(10));
			stats.Timer("MyTimer", TimeSpan.Zero);
			stats.Timer("MyTimer", TimeSpan.FromMilliseconds(5));

			var summary = stats.Summarize();
			Check.That(
				() => summary.Single(x => x.Name == "stats.timers.MyTimer.upper").Value == 10,
				() => summary.Single(x => x.Name == "stats.timers.MyTimer.lower").Value == 0,
				() => summary.Single(x => x.Name == "stats.timers.MyTimer.count").Value == 3,
				() => summary.Single(x => x.Name == "stats.timers.MyTimer.sum").Value == 15,
				() => summary.Single(x => x.Name == "stats.timers.MyTimer.mean").Value == 5
			);
		}

		public void Counters_they_are_per_second() {
			var stats = new GraphiteStatsCollection();

			stats.Counter("MyCount");
			stats.Counter("MyCount", 4);

			Check.That(
				() => stats.Summarize(TimeSpan.FromSeconds(10)).Single(x => x.Name == "stats_counts.MyCount").Value == 5,
				() => stats.Summarize(TimeSpan.FromSeconds(10)).Single(x => x.Name == "stats.MyCount").Value == 0.5);

		}
	}
}
