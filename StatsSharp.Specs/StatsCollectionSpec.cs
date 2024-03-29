using System;
using System.Linq;
using CheckThat;
using Xunit;

namespace StatsSharp.Specs
{
	public class StatsCollectionSpec
	{
		readonly StatsCollection Stats;

		public StatsCollectionSpec() { 
			Stats = new StatsCollection(); 
		}

		[Fact]
		public void Gauge_has_last_seen_value() {
			Stats.GaugeAbsoluteValue("MyGauge", 1);
			Stats.GaugeAbsoluteValue("MyGauge", 2);

			Check.That(() => Stats.Summarize().Single(x => x.Name == "stats.gauges.MyGauge").Value == 2);
		}

		[Fact]
		public void Timer_has_upper_lower_count_sum_mean() {
			Stats.Timer("MyTimer", TimeSpan.FromMilliseconds(10));
			Stats.Timer("MyTimer", TimeSpan.Zero);
			Stats.Timer("MyTimer", TimeSpan.FromMilliseconds(5));

			var summary = Stats.Summarize();
			Check.That(
				() => summary.Single(x => x.Name == "stats.timers.MyTimer.upper").Value == 10,
				() => summary.Single(x => x.Name == "stats.timers.MyTimer.lower").Value == 0,
				() => summary.Single(x => x.Name == "stats.timers.MyTimer.count").Value == 3,
				() => summary.Single(x => x.Name == "stats.timers.MyTimer.sum").Value == 15,
				() => summary.Single(x => x.Name == "stats.timers.MyTimer.mean").Value == 5
			);
		}

		[Fact]
		public void Timer_includeds_configured_percentiles() {
			Stats.Percentiles.Add(90);

			foreach(var value in new ulong[] { 450, 120, 553, 994, 334, 844, 675, 496 })
				Stats.Timer("MyTimer", value);

			var summary = Stats.Summarize();
			Check.That(
				() => summary.Single(x => x.Name == "stats.timers.MyTimer.mean_90").Value == 496,
				() => summary.Single(x => x.Name == "stats.timers.MyTimer.upper_90").Value == 844,
				() => summary.Single(x => x.Name == "stats.timers.MyTimer.sum_90").Value == 3472
			);
		}

		[Fact]
		public void Counters_are_per_second() {
			Stats.Counter("MyCount");
			Stats.Counter("MyCount", 4);

			Check.That(
				() => Stats.Summarize(DateTime.Now, TimeSpan.FromSeconds(5)).Single(x => x.Name == "stats_counts.MyCount").Value == 5,
				() => Stats.Summarize(DateTime.Now, TimeSpan.FromSeconds(5)).Single(x => x.Name == "stats.MyCount").Value == 1);
		}
	}
}
