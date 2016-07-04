using System;
using System.Text;
using System.Text.RegularExpressions;

namespace StatsSharp
{
	public struct Metric
	{
		static readonly Regex MetricPattern = new Regex(@"(?<name>.+):(?<value>(\+|-)?[0-9]+)\|(?<unit>g|c|ms)");
		public readonly string Name;
		public readonly MetricValue Value;

		public Metric(string name, MetricValue value) {
			this.Name = name;
			this.Value = value;
		}

		public static bool TryParse(string input, out Metric result) {
			var m = MetricPattern.Match(input);
			if(!m.Success) {
				result = new Metric();
				return false;
			}

			result = new Metric(
				m.Groups["name"].Value, 
				ParseValue(m.Groups["value"].Value, m.Groups["unit"].Value)
			);
			return true;
		}

		public int GetBytes(Encoding encoding, byte[] target, int targetOffset) {
			var n = encoding.GetBytes(Name, 0, Name.Length, target, targetOffset);
			return n + Value.GetBytes(encoding, target, targetOffset + n);
		}

		static MetricValue ParseValue(string value, string type) {
			switch(type) {
				case "g":
					if(value[0] == '+' || value[0] == '-')
						return MetricValue.GaugeDelta(int.Parse(value));
					return MetricValue.Gauge(ulong.Parse(value));
				case "c": return MetricValue.Counter(long.Parse(value));
				case "ms": return MetricValue.Time(ulong.Parse(value));
				default: throw new NotSupportedException($"Invalid metrics type '{type}'");
			}
		}
	}
}