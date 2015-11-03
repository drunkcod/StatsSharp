using System.Collections.Generic;

namespace StatsSharp
{
	public interface IStatsClient
	{
		void Send(string name, MetricValue value);
		void Send(IEnumerable<Metric> metrics);
	}

	public static class StatsClientExtensions
	{
		static readonly MetricValue CountOfOne = MetricValue.Counter(1); 

		public static void Send(this IStatsClient stats, params Metric[] metrics) { stats.Send(metrics); }

		public static void Counter(this IStatsClient stats, string name) {
			stats.Send(name, CountOfOne);
		}

		public static void Timer(this IStatsClient stats, string name, ulong value) {
			stats.Send(name, MetricValue.Time(value));
		}

		public static void GaugeAbsoluteValue(this IStatsClient stats, string name, int value) {
			if(value < 0) {
				stats.Send(
					new Metric(name, MetricValue.Gauge(0)), 
					new Metric(name, MetricValue.GaugeDelta(value)));
			} else {
				stats.Send(name, MetricValue.Gauge((ulong)value));
			}
		}

		public static void GaugeDelta(this IStatsClient stats, string name, int value) {
			stats.Send(name, MetricValue.GaugeDelta(value));
		}
	}
}