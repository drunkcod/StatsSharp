using Cone;
using System.Text;

namespace StatsSharp.Specs
{
	[Describe(typeof(Metric))]
	public class MetricSpec
	{
		[DisplayAs("{0} => ({1}, {2})")
		,Row("gauge:1|g", "gauge", "1", MetricType.Gauge)
		,Row("gauge-incr:+2|g", "gauge-incr", "+2", MetricType.GaugeDelta)
		,Row("gauge-decr:-3|g", "gauge-decr", "-3", MetricType.GaugeDelta)
		,Row("counter:4|c", "counter", "4", MetricType.Counter)
		,Row("timer:5|ms", "timer", "5", MetricType.Time)]
		public void parsing(string input, string name, string value, MetricType type) {
			var result = new Metric();
			Check.That(
				() => Metric.TryParse(input, out result),
				() => result.Name == name,
				() => result.Value.ToString() == value,
				() => result.Value.Type == type
			);
		}

		public void gauage_delta_encoding() {
			var bytes = new byte[32];

			Check.With(() => MetricValue.GaugeDelta(-1).GetBytes(Encoding.UTF8, bytes, 0))
			.That(n => Encoding.UTF8.GetString(bytes, 0, n) == ":-1|g");
		}

		public void standard_values_are_ulongs() =>
			Check.That(() => MetricValue.Gauge(42).BoxedBits() is ulong);

		public void deltas_are_ints() =>
			Check.That(() => MetricValue.GaugeDelta(-42).BoxedBits() is int);

		public void counters_are_longs() =>
			Check.That(() => MetricValue.Counter(123).BoxedBits() is long);

		public void supports_double_timers() {
			var value = MetricValue.Time(56.23);
			Check.That(
				() => value.BoxedBits() is ulong,
				() => (ulong)value.BoxedBits() == 56,
				() => value.AsDouble() == 56.23);
		}
	}
}
