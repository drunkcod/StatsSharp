using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace StatsSharp
{
	public class StatsCollection : IStatsClient
	{
		class BucketCollection
		{
			public readonly ConcurrentDictionary<string, MetricValue> Gauges = new ConcurrentDictionary<string, MetricValue>();
			public readonly ConcurrentDictionary<string, ConcurrentBag<MetricValue>> Timers = new ConcurrentDictionary<string, ConcurrentBag<MetricValue>>();
			public readonly ConcurrentDictionary<string, long> Counts = new ConcurrentDictionary<string, long>();
			readonly StatsCollectionConfig config;

			public BucketCollection(StatsCollectionConfig config) {
				this.config = config;
			} 

			public StatsSummary Summarize(DateTime timeStamp, TimeSpan flushInterval) {
				
				return new StatsSummary(timeStamp,
					SummarizeGauges()
					.Concat(SummarizeTimers())
					.Concat(SummarizeCounts(flushInterval)).ToArray());
			}

			IEnumerable<StatsValue> SummarizeGauges() =>
				Gauges.Select(item => new StatsValue("stats.gauges." + item.Key, item.Value.AsDouble()));

			IEnumerable<StatsValue> SummarizeCounts(TimeSpan flushInterval) {
				foreach(var counter in Counts) {
					yield return new StatsValue("stats_counts." + counter.Key, counter.Value);
					yield return new StatsValue("stats." + counter.Key, counter.Value / flushInterval.TotalSeconds);
				}
			}

			IEnumerable<StatsValue> SummarizeTimers() {
				foreach(var timer in Timers) {
					var items = timer.Value.Select(x => x.AsDouble()).ToList();
					items.Sort();

					var prefix = "stats.timers." + timer.Key;
					if(config.Percentiles.Count > 0) {
						var cumulativeValues = new double[items.Count];
						cumulativeValues[0] = items[0];
						for (var i = 1; i < items.Count; ++i)
							cumulativeValues[i]= items[i] + cumulativeValues[i-1];

						var mean = items[0];
						var sumAtThreshold = items[items.Count - 1];
						var maxAtThreshold = items[items.Count - 1];
						var indexScale = items.Count / 100.0;
						foreach(var pct in config.Percentiles) {
							if(items.Count > 1) {
								var thresholdIndex = (100.0 - pct) * indexScale;
								var numInThreshold = (int)Math.Round(items.Count - thresholdIndex);

								maxAtThreshold = items[numInThreshold - 1];
								sumAtThreshold = cumulativeValues[numInThreshold - 1];
								mean = sumAtThreshold / numInThreshold;
							}
							var suffix = pct.ToString(CultureInfo.InvariantCulture).Replace(".", "_");
							yield return new StatsValue(prefix + ".mean_"  + suffix, mean);
							yield return new StatsValue(prefix + ".upper_" + suffix, maxAtThreshold);
							yield return new StatsValue(prefix + ".sum_" + suffix, sumAtThreshold);
						}
					}

					var sum = items.Sum();

					yield return new StatsValue(prefix + ".upper", items[items.Count - 1]);
					yield return new StatsValue(prefix + ".lower", items[0]);
					yield return new StatsValue(prefix + ".count", items.Count);
					yield return new StatsValue(prefix + ".sum", sum);
					yield return new StatsValue(prefix + ".mean", sum / items.Count);
				}
			} 
		}

		readonly StatsCollectionConfig config;
		BucketCollection buckets;

		public StatsCollection() : this(new StatsCollectionConfig()) { }
		public StatsCollection(StatsCollectionConfig config) {
			this.config = config;
			this.buckets = new BucketCollection(config);
		}

		public List<double> Percentiles => config.Percentiles;

		public StatsSummary Summarize() => Summarize(DateTime.Now, TimeSpan.FromSeconds(10));

		public StatsSummary Summarize(DateTime timestamp, TimeSpan flushInterval) =>
			buckets.Summarize(timestamp, flushInterval);

		public StatsSummary Flush(DateTime timestamp, TimeSpan flushInterval) => 
			Interlocked.Exchange(ref buckets, new BucketCollection(config)).Summarize(timestamp, flushInterval);

		void IStatsClient.Send(Metric metric) => SendCore(metric);

		void SendCore(Metric metric) {
			switch (metric.Value.Type & MetricType.MetricTypeMask) {
				default: throw new NotImplementedException();
				case MetricType.Gauge:
					buckets.Gauges[metric.Name] = metric.Value;
					break;
				case MetricType.Counter:
					buckets.Counts.AddOrUpdate(metric.Name, _ => 1, (_, x) => x + (long)metric.Value.Bits);
					break;
				case MetricType.Time:
					buckets.Timers
						.GetOrAdd(metric.Name, _ => new ConcurrentBag<MetricValue>())
						.Add(metric.Value);
					break;
			}
		}

		void IStatsClient.Send(IEnumerable<Metric> metrics) {
			foreach(var item in metrics)
				SendCore(item);
		}
	}
}