using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace StatsSharp
{
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
