using System.CodeDom;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace StatsSharp
{
	public struct MetricValue
	{
		public static readonly Encoding Encoding = Encoding.ASCII;
		internal readonly byte[] Bytes;

		MetricValue(byte[] bytes) {
			this.Bytes = bytes;
		}

		public static MetricValue Gauge(ulong value) => new MetricValue(Encoding.GetBytes($"{value}|g"));
		public static MetricValue GaugeDelta(int delta) => new MetricValue(Encoding.GetBytes(delta < 0 ? $"{delta}|g" : $"+{delta}|g"));
		public static MetricValue Counter(long value) => new MetricValue(Encoding.GetBytes($"{value}|c"));
		public static MetricValue Time(ulong value) => new MetricValue(Encoding.GetBytes($"{value}|ms"));

		public override string ToString() => Encoding.GetString(Bytes);
	}

	public struct StatsPrefix
	{
		readonly string prefix;

		public StatsPrefix(string prefix) {
			this.prefix = prefix.EndsWith(".") 
				? prefix
				: prefix + '.'; 
		}

		public override string ToString() { return prefix.TrimEnd('.'); }

		public static string operator+(StatsPrefix lhs, string rhs) {return lhs.prefix + rhs; }
	}

	public class UdpStatsClient
	{
		const int DatagramSize = 512;
		static readonly byte NameValueSeparator = MetricValue.Encoding.GetBytes(":").Single();
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
			var datagram = new byte[DatagramSize];
			var n = MetricValue.Encoding.GetBytes(name, 0, name.Length, datagram, 0);
			datagram[n++] = NameValueSeparator;
			value.Bytes.CopyTo(datagram, n);
			n += value.Bytes.Length;
			socket.SendTo(datagram, 0, n, SocketFlags.None, target);
		}
	}
}
