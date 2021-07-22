using CheckThat;
using Xunit;

namespace StatsSharp.Specs
{
	public class MetricSpec
	{
		[Fact]
		public void standard_values_are_ulongs() =>
			Check.That(() => MetricValue.Gauge(42).BoxedBits() is ulong);

		[Fact]
		public void deltas_are_ints() =>
			Check.That(() => MetricValue.Delta(-42).BoxedBits() is int);

		[Fact]
		public void counters_are_ints() =>
			Check.That(() => MetricValue.Counter(123).BoxedBits() is int);

		[Fact]
		public void supports_double_timers() {
			var value = MetricValue.Time(56.23);
			Check.That(
				() => value.BoxedBits() is ulong,
				() => (ulong)value.BoxedBits() == 56,
				() => value.AsFloat() == 56.23);
		}
	}
}
