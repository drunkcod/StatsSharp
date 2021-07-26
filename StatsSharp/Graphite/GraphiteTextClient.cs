using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace StatsSharp.Graphite
{
	public interface IGraphiteClient
	{
		void Send(GraphiteValue value);
		void Send(IEnumerable<GraphiteValue> values);
	}

	public sealed class GraphiteTextClient : IDisposable, IGraphiteClient, IStatsClient
	{
		const int MaxStackAlloc = 1024;
		static readonly byte[] RecordSeparator = { (byte)'\n' };

		Socket socket;

		readonly string host;
		readonly int port;

		public static readonly Encoding Encoding = new UTF8Encoding(false);

		public GraphiteTextClient(string host, int port) {
			this.host = host;
			this.port = port;
		}

		public static GraphiteTextClient Connect(string host, int port = 2003) {
			var client = new GraphiteTextClient(host, port);
			client.Connect();
			return client;
		}

		public void Connect() {
			socket = new Socket(SocketType.Stream, ProtocolType.IP);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
			socket.Connect(host, port);
		}

		public void Send(GraphiteValue value) {
			var s = value.ToString();
			if(!SendFast(s)) {
				socket.Send(Encoding.GetBytes(s.ToString()));
				socket.Send(RecordSeparator);
			}
		}

		bool SendFast(string value) {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
			if((MaxStackAlloc - 1) < value.Length)
				return false;
			try {
				Span<byte> bytes = stackalloc byte[MaxStackAlloc];
				var n = Encoding.GetBytes(value, bytes) + 1;
				if(n < bytes.Length) {
					bytes[n - 1] = (byte)'\n';
					socket.Send(bytes.Slice(0, n));
				}
				return true;
			} catch {
				return false;
			}
#else
			return false;
#endif
		}

		public void Send(IEnumerable<GraphiteValue> values) {
			foreach(var item in values)
				Send(item);
		}

		public void Close() =>
			socket.Close();

		public void Dispose() =>
			socket.Dispose();

		public void Send(Metric metric) =>
			Send(new GraphiteValue(metric.Name, metric.Value.AsFloat(), DateTime.UtcNow));

		public void Send(IEnumerable<Metric> metrics)
		{
			var now = DateTime.UtcNow;
			foreach(var item in metrics)
				Send(new GraphiteValue(item.Name, item.Value.AsFloat(), now));
		}
	}
}
