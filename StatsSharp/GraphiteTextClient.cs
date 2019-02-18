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

	public class GraphiteTextClient : IDisposable, IGraphiteClient, IStatsClient
	{
		static readonly byte[] RecordSeparator = { (byte)'\n' };

		readonly Socket socket;

		public Encoding Encoding => Encoding.UTF8;

		GraphiteTextClient(string host, int port) {
			this.socket = new Socket(SocketType.Stream, ProtocolType.IP);
			socket.Connect(host, port);
		}

		public static GraphiteTextClient Create(string host, int port = 2003) =>
			new GraphiteTextClient(host, port);

		public void Send(GraphiteValue value) {
			socket.Send(value.GetBytes(Encoding));
			socket.Send(RecordSeparator);
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
			Send(new GraphiteValue(metric.Name, metric.Value.AsDouble(), DateTime.UtcNow));

		public void Send(IEnumerable<Metric> metrics)
		{
			var now = DateTime.UtcNow;
			foreach(var item in metrics)
				Send(new GraphiteValue(item.Name, item.Value.AsDouble(), now));
		}
	}
}
