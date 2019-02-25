using System;
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
		public static readonly Encoding Encoding = Encoding.UTF8;

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

		public void Send(Metric metric) {
			var datagram = new Dgram(DatagramSize);
			if(!datagram.TryAppend(metric, Encoding))
				throw new ArgumentException();
			datagram.SendTo(socket, target);
		}

		public void Send(IEnumerable<Metric> metrics) {
			var datagram = new Dgram(DatagramSize);
			foreach(var item in metrics) {
				var start = datagram.Position;
				if(!datagram.TryAppend(item, Encoding)) {
					datagram.SendTo(socket, target, start);
					datagram.Clear();
					if(!datagram.TryAppend(item, Encoding))
						throw new ArgumentException();
				}
				if(datagram.Capacity > 0)
					datagram.Append(RecordSeparator);
				else {
					datagram.SendTo(socket, target);
					datagram.Clear();
				}
			}
			datagram.SendTo(socket, target);
		}
	}
}
