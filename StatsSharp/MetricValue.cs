using System;
using System.IO;
using System.Security;

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

		readonly long bits;

		public readonly MetricType Type;

		MetricValue(long bits, MetricType type) {
			this.bits = bits;
			this.Type = type;
		}

		public static MetricValue Gauge(ulong value) => new MetricValue((long)value, MetricType.Gauge);
		public static MetricValue Delta(long delta) => new MetricValue(delta, MetricType.GaugeDelta);
		public static MetricValue Counter(long value) => new MetricValue(value, MetricType.Counter);
		public static MetricValue Time(uint value) => new MetricValue(value, MetricType.Time);
		public static MetricValue Time(double value) => new MetricValue(FloatToBits(value), MetricType.Time | MetricType.Float);

		public override string ToString() {
			var r = new StringWriter();
			WriteValueTo(r);
			return r.ToString();
		}

		public double AsFloat() => IsFloat ? BitsToFloat(bits) : bits;
		public long AsInt32() => IsFloat ? (int)BitsToFloat(bits) : bits;

		public bool IsFloat => (Type & MetricType.Float) == MetricType.Float;

		public void WriteTo(TextWriter output) {
			output.Write(':');
			WriteValueTo(output);
			output.Write(TypeSuffix[(int)(Type & MetricType.MetricTypeMask)]);
		}

		void WriteValueTo(TextWriter output) {
			if (Type == MetricType.GaugeDelta)
				output.Write("{0:+0;-#}", bits);
			else output.Write(BoxedBits());
		}

		public object BoxedBits() {
			var b = IsFloat ? BitsToFloat(bits) : bits;
			switch(Type & MetricType.MetricTypeMask) {
				case MetricType.Gauge:
				case MetricType.Time: return (ulong)b;
				case MetricType.GaugeDelta: return (int)b;
				case MetricType.Counter: return (int)b;
				default: throw new InvalidOperationException();
			}
		}

		static long FloatToBits(double value) => BitConverter.DoubleToInt64Bits(value);

		static double BitsToFloat(long value) => BitConverter.Int64BitsToDouble(value);
	}
}