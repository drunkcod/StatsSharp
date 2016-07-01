using System;
using System.Linq;
using Cone;

namespace StatsSharp.Specs
{
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

		public void Timer_percentiles() {
			var stats = new GraphiteStatsCollection();
			stats.Percentiles.Add(90);

			foreach(var value in new ulong[] { 450, 120, 553, 994, 334, 844, 675, 496 })
				stats.Timer("MyTimer", value);

			var summary = stats.Summarize();
			Check.That(
				() => summary.Single(x => x.Name == "stats.timers.MyTimer.mean_90").Value == 496,
				() => summary.Single(x => x.Name == "stats.timers.MyTimer.upper_90").Value == 844,
				() => summary.Single(x => x.Name == "stats.timers.MyTimer.sum_90").Value == 3472
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
