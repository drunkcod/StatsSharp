using Cone;

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
    }
}
