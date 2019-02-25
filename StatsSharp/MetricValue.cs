using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;

namespace StatsSharp
{
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
			var r = new StringWriter();
			WriteValueTo(r);
			return r.ToString();
		}

		public double AsDouble() => (Type & MetricType.Double) == 0 
			? (double)Bits 
			: BitConverter.Int64BitsToDouble((long)Bits);

		public void WriteTo(TextWriter output) {
			output.Write(':');
			WriteValueTo(output);
			output.Write(TypeSuffix[(int)(Type & MetricType.MetricTypeMask)]);
		}

		void WriteValueTo(TextWriter output) {
			if (Type == MetricType.GaugeDelta)
				output.Write("{0:+0;-#}", (int)Bits);
			else output.Write(BoxedBits());
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