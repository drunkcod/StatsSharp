using System;
using System.Collections.Generic;
using System.Net.Sockets;
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

	public class GraphiteTextClient
	{
		static readonly byte[] RecordSeparator = { (byte)'\n' };

		readonly Socket socket;

		GraphiteTextClient(string host, int port) {
			this.socket = new Socket(SocketType.Stream, ProtocolType.IP);
			socket.Connect(host, port);
		}

		public static GraphiteTextClient Create(string host, int port = 2003) {
			return new GraphiteTextClient(host, port);
		}

		public void Send(GraphiteValue value) {
			socket.Send(value.GetBytes(Encoding.ASCII));
			socket.Send(RecordSeparator);
		}

		public void Send(IEnumerable<GraphiteValue> values) {
			foreach(var item in values)
				Send(item);
		}

		public void Close() {
			socket.Close();
		}
	}
}
