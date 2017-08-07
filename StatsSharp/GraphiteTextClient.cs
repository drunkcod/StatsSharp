using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace StatsSharp
{
	public interface IGraphiteClient
	{
		void Send(GraphiteValue value);
		void Send(IEnumerable<GraphiteValue> values);
	}

	public class GraphiteTextClient : IDisposable, IGraphiteClient
	{
		static readonly byte[] RecordSeparator = { (byte)'\n' };
		Socket socket;

		GraphiteTextClient(string host, int port) {
			this.socket = new Socket(SocketType.Stream, ProtocolType.IP);
			socket.Connect(host, port);
		}

		public static GraphiteTextClient Create(string host, int port = 2003) {
			return new GraphiteTextClient(host, port);
		}

		public void Send(GraphiteValue value) => Send(value, true);

		void Send(GraphiteValue value, bool retry) {
			try {
				socket.Send(value.GetBytes(Encoding.ASCII));
				socket.Send(RecordSeparator);
			} catch(SocketException) {
				if(retry) {
					var ep = socket.RemoteEndPoint;
					socket = new Socket(SocketType.Stream, ProtocolType.IP);
					socket.Connect(ep);
					Send(value, false);
				}
			}
		}

		public void Send(IEnumerable<GraphiteValue> values) {
			foreach(var item in values)
				Send(item);
		}

		public void Close() {
			socket.Close();
		}

		void IDisposable.Dispose() {
			socket.Dispose();
		}
	}
}
