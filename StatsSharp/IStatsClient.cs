using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StatsSharp
{
	public interface IStatsClient
	{
		void Send(Metric metric);
		void Send(IEnumerable<Metric> metrics);
	}

	public class NullStatsClient : IStatsClient
	{
		NullStatsClient() { }
		public void Send(Metric metric) { }
		public void Send(IEnumerable<Metric> metrics) { } 

		public static NullStatsClient Instance = new NullStatsClient();
	}

	public static class StatsClientExtensions
	{
		static readonly MetricValue CountOfOne = MetricValue.Counter(1); 

		public static void Send(this IStatsClient stats, string name, MetricValue value) { stats.Send(new Metric(name, value)); }

		public static void Send(this IStatsClient stats, params Metric[] metrics) { stats.Send(metrics); }

		public static void Counter(this IStatsClient stats, string name) {
			stats.Send(new Metric(name, CountOfOne));
		}

		public static void Counter(this IStatsClient stats, string name, int count) {
			stats.Send(new Metric(name, MetricValue.Counter(count)));
		}

		public static void Timer(this IStatsClient stats, string name, ulong value) {
			stats.Send(new Metric(name, MetricValue.Time(value)));
		}

		public static void Timer(this IStatsClient stats, string name, TimeSpan value) {
			stats.Timer(name, (ulong)value.TotalMilliseconds);
		}

		public static void Timer(this IStatsClient stats, string name, Action action) {
			var time = Stopwatch.StartNew();
			action();
			stats.Timer(name, time.Elapsed);
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