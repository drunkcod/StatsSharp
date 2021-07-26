using System;
using System.Globalization;

namespace StatsSharp.Graphite
{
	public readonly struct GraphiteValue
	{
		static readonly DateTime UnixExpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public readonly string Name;
		public readonly double Value;
		readonly int unixTime;

		public DateTime TimeStamp => UnixExpoch.AddSeconds(unixTime);

		public static int ToUnixTime(DateTime timeStamp) => 
			(int)timeStamp.ToUniversalTime().Subtract(UnixExpoch).TotalSeconds;

		public GraphiteValue(string name, double value, DateTime timeStamp) : this(name, value, ToUnixTime(timeStamp))
		{ }

		public GraphiteValue(string name, double value, int unixTimestamp) {
			this.Name = name;
			this.Value = value;
			this.unixTime = unixTimestamp;
		}

		public override string ToString() =>
			string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", Name, Value, unixTime);
	}
}