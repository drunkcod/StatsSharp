using System.Diagnostics.Contracts;
using System.Text;

namespace StatsSharp
{
	public enum MetricType : byte
	{
		Gauge, GaugeDelta, Counter, Time
	}

	public struct MetricValue
	{
		static readonly int[] MetaLength = { 3, 3, 3, 4 };
		
		public readonly ulong Bits;
		readonly string value;

		MetricValue(ulong bits, string value) {
			this.Bits = bits;
			this.value = value;
		}

		public MetricType Type => 
			value.EndsWith("g") ? (char.IsNumber(value[1]) ? MetricType.Gauge : MetricType.GaugeDelta)
			: value.EndsWith("c") ? MetricType.Counter 
			: MetricType.Time;

		public static MetricValue Gauge(ulong value) => new MetricValue(value, $":{value}|g");
		public static MetricValue GaugeDelta(int delta) => new MetricValue((ulong)delta, delta < 0 ? $":{delta}|g" : $":+{delta}|g");
		public static MetricValue Counter(long value) => new MetricValue((ulong)value, $":{value}|c");
		public static MetricValue Time(ulong value) => new MetricValue(value, $":{value}|ms");

		public override string ToString() => value?.Substring(1, value.Length - MetaLength[(int)Type]) ?? string.Empty;

		[Pure]
		public int GetBytes(Encoding encoding, byte[] target, int targetIndex) {
			return encoding.GetBytes(value, 0, value.Length, target, targetIndex);
		}
	}
}