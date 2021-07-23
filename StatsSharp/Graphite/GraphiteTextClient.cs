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
		static readonly byte[] RecordSeparator = { (byte)'\n' };

		readonly Socket socket;

		public static readonly Encoding Encoding = new UTF8Encoding(false);

		GraphiteTextClient(string host, int port) {
			this.socket = new Socket(SocketType.Stream, ProtocolType.IP);
			socket.Connect(host, port);
		}

		public static GraphiteTextClient Create(string host, int port = 2003) =>
			new(host, port);

		public void Send(GraphiteValue value) {
			var s = value.ToString();
			if(!SendFast(s)) {
				socket.Send(Encoding.GetBytes(s.ToString()));
				socket.Send(RecordSeparator);
			}
		}

		bool SendFast(string value) {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
			try {
				Span<byte> bytes = stackalloc byte[value.Length + 3];
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

		void IDisposable.Dispose() =>
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
