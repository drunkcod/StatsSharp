using System;
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
		static readonly string[] TypeSuffix = {
			"|g",
			"|g",
			"|c",
			"|ms"
		};

		public readonly ulong Bits;
		public readonly MetricType Type;

		MetricValue(ulong bits, MetricType type) {
			this.Bits = bits;
			this.Type = type;
		}

		public static MetricValue Gauge(ulong value) => new MetricValue(value, MetricType.Gauge);
		public static MetricValue GaugeDelta(int delta) => new MetricValue((ulong)delta, MetricType.GaugeDelta);
		public static MetricValue Counter(long value) => new MetricValue((ulong)value, MetricType.Counter);
		public static MetricValue Time(ulong value) => new MetricValue(value, MetricType.Time);

		public override string ToString() {
			if(Type == MetricType.GaugeDelta)
				return ((int)Bits).ToString("+0;-#");
			return BoxedBits().ToString();
		}

		[Pure]
		public int GetBytes(Encoding encoding, byte[] target, int targetIndex) {
			var value = ":" + ToString() + TypeSuffix[(int)Type];
			return encoding.GetBytes(value, 0, value.Length, target, targetIndex);
		}

		object BoxedBits() {
			switch(Type) {
				case MetricType.Gauge:
				case MetricType.Time: return Bits;
				case MetricType.GaugeDelta: return ((int)Bits);
				case MetricType.Counter: return ((long)Bits);
				default: throw new InvalidOperationException();
			}
		}
	}
}