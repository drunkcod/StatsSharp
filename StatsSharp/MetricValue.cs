using System;
using System.Diagnostics.Contracts;
using System.Text;

namespace StatsSharp
{
	public enum MetricType : byte
	{
		Gauge = 0,
		GaugeDelta = 1,
		Counter = 2,
		Time = 3,
		MetricTypeMask = 3,
		Double = 4,
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
		public static MetricValue Time(double value) => new MetricValue((ulong)BitConverter.DoubleToInt64Bits(value), MetricType.Time | MetricType.Double);

		public override string ToString() {
			if(Type == MetricType.GaugeDelta)
				return ((int)Bits).ToString("+0;-#");
			return BoxedBits().ToString();
		}

		public double AsDouble() => (Type & MetricType.Double) == 0 
			? (double)Bits 
			: BitConverter.Int64BitsToDouble((long)Bits);

		[Pure]
		public int GetBytes(Encoding encoding, byte[] target, int targetIndex) {
			var value = ":" + ToString() + TypeSuffix[(int)(Type & MetricType.MetricTypeMask)];
			return encoding.GetBytes(value, 0, value.Length, target, targetIndex);
		}

		public object BoxedBits() {
			var b = (Type & MetricType.Double) == 0 ? Bits : (ulong)BitConverter.Int64BitsToDouble((long)Bits);
			switch(Type & MetricType.MetricTypeMask) {
				case MetricType.Gauge:
				case MetricType.Time: return b;
				case MetricType.GaugeDelta: return ((int)b);
				case MetricType.Counter: return ((long)b);
				default: throw new InvalidOperationException();
			}
		}
	}
}