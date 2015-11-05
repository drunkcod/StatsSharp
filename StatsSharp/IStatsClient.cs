using System.Collections.Generic;

namespace StatsSharp
{
	public interface IStatsClient
	{
		void Send(Metric metric);
		void Send(IEnumerable<Metric> metrics);
	}

	public static class StatsClientExtensions
	{
		static readonly MetricValue CountOfOne = MetricValue.Counter(1); 

		public static void Send(this IStatsClient stats, string name, MetricValue value) { stats.Send(new Metric(name, value)); }

		public static void Send(this IStatsClient stats, params Metric[] metrics) { stats.Send(metrics); }

		public static void Counter(this IStatsClient stats, string name) {
			stats.Send(new Metric(name, CountOfOne));
		}

		public static void Timer(this IStatsClient stats, string name, ulong value) {
			stats.Send(new Metric(name, MetricValue.Time(value)));
		}

		public static void GaugeAbsoluteValue(this IStatsClient stats, string name, int value) {
			if(value < 0) {
				stats.Send(
					new Metric(name, MetricValue.Gauge(0)), 
					new Metric(name, MetricValue.GaugeDelta(value)));
			} else {
				stats.Send(new Metric(name, MetricValue.Gauge((ulong)value)));
			}
		}

		public static void GaugeDelta(this IStatsClient stats, string name, int value) {
			stats.Send(new Metric(name, MetricValue.GaugeDelta(value)));
		}
	}
}