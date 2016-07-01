using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace StatsSharp
{
	public class GraphiteStatsCollection : IStatsClient
	{
		class BucketCollection
		{
			public readonly ConcurrentDictionary<string, ulong> Gauges = new ConcurrentDictionary<string, ulong>();
			public readonly ConcurrentDictionary<string, ConcurrentBag<ulong>> Timers = new ConcurrentDictionary<string, ConcurrentBag<ulong>>();
			public readonly ConcurrentDictionary<string, long> Counts = new ConcurrentDictionary<string, long>();
			public readonly List<double> Percentiles;

			public BucketCollection(List<double> percentiles) {
				this.Percentiles = percentiles;	
			} 

			public IEnumerable<GraphiteValue> Summarize(TimeSpan flushInterval) {
				var ts = GraphiteValue.ToUnixTime(DateTime.UtcNow);
				return Gauges.Select(item => new GraphiteValue("stats.gauges." + item.Key, item.Value, ts))
					.Concat(SummarizeTimers(ts))
					.Concat(SummarizeCounts(flushInterval, ts));
			}

			private IEnumerable<GraphiteValue> SummarizeCounts(TimeSpan flushInterval, int ts)
			{
				foreach(var counter in Counts) {
					yield return new GraphiteValue("stats_counts." + counter.Key, counter.Value, ts);
					yield return new GraphiteValue("stats." + counter.Key, counter.Value / flushInterval.TotalSeconds, ts);
				}
			}

			IEnumerable<GraphiteValue> SummarizeTimers(int ts) {
				foreach(var timer in Timers) {
					var items = timer.Value.ToList();
					items.Sort();

					if(Percentiles.Count > 0) {
						var cumulativeValues = new double[items.Count];
						cumulativeValues[0] = items[0];
						for (var i = 1; i < items.Count; ++i)
							cumulativeValues[i]= items[i] + cumulativeValues[i-1];

						var sumAtThreshold = (double)items[items.Count - 1];
						var mean = (double)items[0];
						var maxAtThreshold = (double)items[items.Count - 1];
						foreach(var pct in Percentiles) {
							if(items.Count > 1) {
								var thresholdIndex = ((100.0 - pct) / 100.0) * items.Count;
								var numInThreshold = Math.Round(items.Count - thresholdIndex);

								maxAtThreshold = items[(int)numInThreshold - 1];
								sumAtThreshold = cumulativeValues[(int)numInThreshold - 1];
								mean = sumAtThreshold / numInThreshold;
							}
							var suffix = pct.ToString(CultureInfo.InvariantCulture).Replace(".", "_");
							yield return new GraphiteValue("stats.timers." + timer.Key + ".mean_"  + suffix, mean, ts);
							yield return new GraphiteValue("stats.timers." + timer.Key + ".upper_" + suffix, maxAtThreshold, ts);
							yield return new GraphiteValue("stats.timers." + timer.Key + ".sum_" + suffix, sumAtThreshold, ts);
						}
					}

					var sum = items.Sum(x => (double)x);

					yield return new GraphiteValue("stats.timers." + timer.Key + ".upper", items[items.Count - 1], ts);
					yield return new GraphiteValue("stats.timers." + timer.Key + ".lower", items[0], ts);
					yield return new GraphiteValue("stats.timers." + timer.Key + ".count", items.Count, ts);
					yield return new GraphiteValue("stats.timers." + timer.Key + ".sum", sum, ts);
					yield return new GraphiteValue("stats.timers." + timer.Key + ".mean", sum / items.Count, ts);
				}
			} 
		}

		BucketCollection buckets = new BucketCollection(new List<double>());

		public List<double> Percentiles => buckets.Percentiles;

		public IEnumerable<GraphiteValue> Summarize() => Summarize(TimeSpan.FromSeconds(10));

		public IEnumerable<GraphiteValue> Summarize(TimeSpan flushInterval) =>
			buckets.Summarize(TimeSpan.FromSeconds(10));

		public IEnumerable<GraphiteValue> Flush(TimeSpan flushInterval) => 
			Interlocked.Exchange(ref buckets, new BucketCollection(buckets.Percentiles)).Summarize(flushInterval);

		void IStatsClient.Send(Metric metric) {
			switch(metric.Value.Type) {
				default: throw new NotImplementedException();
				case MetricType.Gauge:
					buckets.Gauges[metric.Name] = metric.Value.Bits;
					break;
				case MetricType.Counter:
					buckets.Counts.AddOrUpdate(metric.Name, _ => 1, (_, x) => x + (long)metric.Value.Bits);
					break;
				case MetricType.Time:
					buckets.Timers
						.GetOrAdd(metric.Name, _ => new ConcurrentBag<ulong>())
						.Add(metric.Value.Bits);
					break;
			}
		}

		void IStatsClient.Send(IEnumerable<Metric> metrics) {
			foreach(var item in metrics)
				this.Send(item);
		}
	}
}