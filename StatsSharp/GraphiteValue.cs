using System;
using System.Text;

namespace StatsSharp
{
	public struct GraphiteValue
	{
		static readonly DateTime UnixExpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public readonly string Name;
		public readonly double Value;
		readonly int unixTime;

		public DateTime TimeStamp => UnixExpoch.AddSeconds(unixTime);

		public GraphiteValue(string name, double value, DateTime timeStamp) {
			this.Name = name;
			this.Value = value;
			this.unixTime = (int)timeStamp.ToUniversalTime().Subtract(UnixExpoch).TotalSeconds;
		}

		public byte[] GetBytes(Encoding encoding) { return encoding.GetBytes($"{Name} {Value} {unixTime}"); }
	}
}