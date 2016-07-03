using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace StatsSharp
{
	public struct StatsValue
	{
		public readonly string Name;
		public readonly double Value;

		public StatsValue(string name, double value) {
			this.Name = name;
			this.Value = value;
		}
	}

	public struct StatsSummary : IEnumerable<StatsValue>
	{
		readonly StatsValue[] values;
		public readonly DateTime Timestamp;
		
		public int Count => values.Length;
		public StatsValue this[int index] => values[index];

		public StatsSummary(DateTime timestamp, StatsValue[] values) {
			this.values = values;
			this.Timestamp = timestamp;
		}

		public IEnumerator<StatsValue> GetEnumerator() => values.AsEnumerable().GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();
	}

	public class StatsCollectionConfig
	{
		public readonly List<double> Percentiles = new List<double>();
		public readonly Dictionary<string, double> Scales = new Dictionary<string, double>(); 

		public double GetScale(string metricName) {
			double scale;
			return Scales.TryGetValue(metricName, out scale) ? scale : 1.0;
		}

	}

	public class StatsCollection : IStatsClient
	{
		class BucketCollection
		{
			public readonly ConcurrentDictionary<string, ulong> Gauges = new ConcurrentDictionary<string, ulong>();
			public readonly ConcurrentDictionary<string, ConcurrentBag<ulong>> Timers = new ConcurrentDictionary<string, ConcurrentBag<ulong>>();
			public readonly ConcurrentDictionary<string, long> Counts = new ConcurrentDictionary<string, long>();
			readonly StatsCollectionConfig config;

			public BucketCollection(StatsCollectionConfig config) {
				this.config = config;
			} 

			public StatsSummary Summarize(TimeSpan flushInterval) {
				
				return new StatsSummary(DateTime.UtcNow,
					SummarizeGauges()
					.Concat(SummarizeTimers())
					.Concat(SummarizeCounts(flushInterval)).ToArray());
			}

			IEnumerable<StatsValue> SummarizeGauges() =>
				Gauges.Select(item => new StatsValue("stats.gauges." + item.Key, config.GetScale(item.Key) * item.Value));

			IEnumerable<StatsValue> SummarizeCounts(TimeSpan flushInterval) {
				foreach(var counter in Counts) {
					yield return new StatsValue("stats_counts." + counter.Key, counter.Value);
					yield return new StatsValue("stats." + counter.Key, counter.Value / flushInterval.TotalSeconds);
				}
			}

			IEnumerable<StatsValue> SummarizeTimers() {
				foreach(var timer in Timers) {
					var scale = config.GetScale(timer.Key);
					var items = timer.Value.Select(x => x * scale).ToList();
					items.Sort();

					if(config.Percentiles.Count > 0) {
						var cumulativeValues = new double[items.Count];
						cumulativeValues[0] = items[0];
						for (var i = 1; i < items.Count; ++i)
							cumulativeValues[i]= items[i] + cumulativeValues[i-1];

						var sumAtThreshold = (double)items[items.Count - 1];
						var mean = (double)items[0];
						var maxAtThreshold = (double)items[items.Count - 1];
						foreach(var pct in config.Percentiles) {
							if(items.Count > 1) {
								var thresholdIndex = ((100.0 - pct) / 100.0) * items.Count;
								var numInThreshold = Math.Round(items.Count - thresholdIndex);

								maxAtThreshold = items[(int)numInThreshold - 1];
								sumAtThreshold = cumulativeValues[(int)numInThreshold - 1];
								mean = sumAtThreshold / numInThreshold;
							}
							var suffix = pct.ToString(CultureInfo.InvariantCulture).Replace(".", "_");
							yield return new StatsValue("stats.timers." + timer.Key + ".mean_"  + suffix, mean);
							yield return new StatsValue("stats.timers." + timer.Key + ".upper_" + suffix, maxAtThreshold);
							yield return new StatsValue("stats.timers." + timer.Key + ".sum_" + suffix, sumAtThreshold);
						}
					}

					var sum = items.Sum();

					yield return new StatsValue("stats.timers." + timer.Key + ".upper", items[items.Count - 1]);
					yield return new StatsValue("stats.timers." + timer.Key + ".lower", items[0]);
					yield return new StatsValue("stats.timers." + timer.Key + ".count", items.Count);
					yield return new StatsValue("stats.timers." + timer.Key + ".sum", sum);
					yield return new StatsValue("stats.timers." + timer.Key + ".mean", sum / items.Count);
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

		public StatsSummary Summarize() => Summarize(TimeSpan.FromSeconds(10));

		public StatsSummary Summarize(TimeSpan flushInterval) =>
			buckets.Summarize(TimeSpan.FromSeconds(10));

		public StatsSummary Flush(TimeSpan flushInterval) => 
			Interlocked.Exchange(ref buckets, new BucketCollection(config)).Summarize(flushInterval);

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

		public void SetScale(string metricName, double scale) {
			config.Scales[metricName] = scale;
		}
	}
}