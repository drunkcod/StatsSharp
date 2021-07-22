using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace StatsSharp
{
	public class UdpStatsClient : IStatsClient
	{
		const string MetricTooLong = "Metric length exceeds datagram size.";
		public static readonly Encoding Encoding = new UTF8Encoding(false);

		const int DatagramSize = 512;
		const byte RecordSeparator = (byte)'\n';

		readonly Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		IPEndPoint target;
		
		readonly MemoryStream bytes = new(DatagramSize);
		readonly string hostNameOrAddress;

		public UdpStatsClient(string hostNameOrAddress, int port) {
			this.hostNameOrAddress = hostNameOrAddress;
			this.target = new IPEndPoint(IPAddress.Loopback, port);
		}

		public void RefreshEndPoint() {
			if (!IPAddress.TryParse(hostNameOrAddress, out var ip)) {
				ip = Dns.GetHostAddresses(hostNameOrAddress).First(x => x.AddressFamily == AddressFamily.InterNetwork);
			}

			if(target.Address != ip)
				target = new IPEndPoint(ip, target.Port);
		}

		public static UdpStatsClient Create(string server, int port = 8125) {
			var client = new UdpStatsClient(server, port);
			client.RefreshEndPoint();
			return client;
		}

		public void Send(Metric metric) {
			bytes.Position = 0;
			metric.WriteTo(bytes, Encoding);
			if(bytes.Position > DatagramSize)
				throw new ArgumentException(MetricTooLong);
			Send(0, (int)bytes.Position);
		}

		public void Send(IEnumerable<Metric> metrics) {
			var start = bytes.Position = 0;
			var end = 0;
			foreach(var item in metrics) {
				item.WriteTo(bytes, Encoding);
				if (bytes.Position > DatagramSize) {
					Send(0, end);
					var len = (int)(bytes.Position - start);
					if (len > DatagramSize)
						throw new ArgumentException(MetricTooLong);
					else {
						Send((int)start, len);
						start = bytes.Position = 0;
						continue;
					}
				}
				
				end = (int)bytes.Position;
				bytes.WriteByte(RecordSeparator);
				start = bytes.Position + 1;
				if(start >= DatagramSize) { 
					Send(0, end);
					start = bytes.Position = 0;
				}
				
			}
			Send(0, end);
		}

		void Send(int offset, int count) {
			if(count > 0)
				socket.SendTo(bytes.GetBuffer(), offset, count, SocketFlags.None, target);
		}
	}
}
