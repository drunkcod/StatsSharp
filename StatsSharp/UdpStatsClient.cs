using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using StatsSharp.Net;

namespace StatsSharp
{
	public class UdpStatsClient : IStatsClient
	{
		public static readonly Encoding Encoding = Encoding.ASCII;

		const int DatagramSize = 512;
		const byte RecordSeparator = (byte)'\n';

		readonly Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		readonly IPEndPoint target;

		public UdpStatsClient(IPEndPoint target) {
			this.target = target;
		}

		public static UdpStatsClient Create(string server, int port = 8125) {
			IPAddress ip;
			if(!IPAddress.TryParse(server, out ip)) {
				ip = Dns.GetHostAddresses(server).First(x => x.AddressFamily == AddressFamily.InterNetwork);
			}
			return new UdpStatsClient(new IPEndPoint(ip, port));
		}

		public void Send(string name, MetricValue value) {
			var datagram = new Dgram(DatagramSize);
			datagram.TryAppend(name, Encoding);
			datagram.TryAppend(value, Encoding);
			datagram.SendTo(socket, target);
		}

		public void Send(IEnumerable<Metric> metrics) {
			var datagram = new Dgram(DatagramSize);
			foreach(var item in metrics) {
				var start = datagram.Position;
				while(!datagram.TryAppend(item.Name, Encoding) || !datagram.TryAppend(item.Value, Encoding) || datagram.Capacity < 1) {
					datagram.SendTo(socket, target, start);
					datagram.Clear();
				}
				datagram.Append(RecordSeparator);
			}
			datagram.SendTo(socket, target);
		}
	}
}
