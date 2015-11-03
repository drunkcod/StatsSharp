using System.Text;

namespace StatsSharp
{
	public struct MetricValue
	{
		public static readonly Encoding Encoding = Encoding.ASCII;
		internal readonly byte[] Bytes;

		MetricValue(byte[] bytes) {
			this.Bytes = bytes;
		}

		public static MetricValue Gauge(ulong value) => new MetricValue(Encoding.GetBytes($"{value}|g"));
		public static MetricValue GaugeDelta(int delta) => new MetricValue(Encoding.GetBytes(delta < 0 ? $"{delta}|g" : $"+{delta}|g"));
		public static MetricValue Counter(long value) => new MetricValue(Encoding.GetBytes($"{value}|c"));
		public static MetricValue Time(ulong value) => new MetricValue(Encoding.GetBytes($"{value}|ms"));

		public override string ToString() => Encoding.GetString(Bytes);
	}
}