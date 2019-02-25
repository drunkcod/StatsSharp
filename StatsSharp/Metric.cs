using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace StatsSharp
{
	public struct Metric
	{
		static readonly Regex MetricPattern = new Regex(@"(?<name>.+):(?<value>(\+|-)?[0-9]+)\|(?<unit>g|c|ms)", RegexOptions.Compiled);
		public readonly string Name;
		public readonly MetricValue Value;

		public Metric(string name, MetricValue value) {
			this.Name = name;
			this.Value = value;
		}

		public static Metric Time(string name, double value) => new Metric(name, MetricValue.Time(value));
		public static Metric Time(string name, ulong value) => new Metric(name, MetricValue.Time(value));

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

		public static string GetName(string path) {
			var parts = path.Split('.');
			return parts[parts.Length - 1];
		}

		public int GetBytes(Encoding encoding, byte[] target, int targetOffset) {
			var v = new MemoryStream(target, targetOffset, target.Length - targetOffset);
			using(var w = new StreamWriter(v, encoding, 64)) { 
				w.Write(Name);
				Value.WriteTo(w);
			}
			return (int)v.Position;
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