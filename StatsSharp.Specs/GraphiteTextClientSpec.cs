using Cone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StatsSharp.Specs
{
	[Describe(typeof(GraphiteTextClient))]
	public class GraphiteTextClientSpec
	{
		class DataReceivedEventArgs : EventArgs
		{
			public readonly Socket Client;
			public readonly string Data;

			public DataReceivedEventArgs(Socket client, string data) {
				this.Client = client;
				this.Data = data;
			}
		}

		class SimpleServer
		{
			class LineReader
			{
				readonly Decoder decoder;
				readonly StringBuilder line;

				public LineReader(Encoding encoding) {
					this.decoder = encoding.GetDecoder();
					this.line = new StringBuilder();
				}

				public void Consume(byte[] bytes, int offset, int count, Action<string> onLine) {
					var chars = new char[count];
					var n = 0;
					decoder.Convert(bytes, offset, count, chars, 0, chars.Length, false, out int bytesUsed, out int charsUsed, out bool completed);
					for(; charsUsed != 0; --charsUsed) {
						if(chars[n] == '\n') {
							onLine(line.ToString());
							line.Clear();
						} else line.Append(chars[n++]);
					}
				}
			}

			readonly Dictionary<Socket, LineReader> sessions = new Dictionary<Socket, LineReader>();		
			Socket socket;

			SimpleServer() {
				StartServing(new IPEndPoint(IPAddress.Any, 1234));
			}

			public static SimpleServer Create() {
				return new SimpleServer();
			}

			public event EventHandler<DataReceivedEventArgs> DataReceived;

			public int Port => (socket.LocalEndPoint as IPEndPoint).Port;

			public void Reset() {
				var ep = (IPEndPoint)socket.LocalEndPoint;
				Stop();
				StartServing(new IPEndPoint(IPAddress.Any, ep.Port));
			}

			public void Stop() {
				socket.Close();
				foreach(var item in sessions.Keys)
					item.Close();
				sessions.Clear();
			}

			public void Poll() {
				while(PumpEvents())
					;
			}

			bool PumpEvents() {
				var workDone = false;
				if(socket.Poll(1000, SelectMode.SelectRead)) {
					var c = socket.Accept();
					sessions.Add(c, new LineReader(Encoding.ASCII));
					workDone = true;
				}
				foreach(var c in sessions) {
					if(!c.Key.Poll(1000, SelectMode.SelectRead))
						continue;
					workDone = true;
					var buff = new byte[4096];
					var n = c.Key.Receive(buff);
					c.Value.Consume(buff, 0, n, line => DataReceived?.Invoke(this, new DataReceivedEventArgs(c.Key, line)));
				}
				return workDone;
			}

			void StartServing(IPEndPoint endpoint) {
				socket = new Socket(SocketType.Stream, ProtocolType.IP);
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger,new LingerOption(false, 0));
				socket.Bind(endpoint);
				socket.Listen(1);
			}
		}

		public void attempts_to_reconnect_on_send_failure() {
			var server = SimpleServer.Create();
			var r = new List<string>();

			server.DataReceived += (_, e) => r.Add(e.Data);
			var graphite = GraphiteTextClient.Create("localhost", server.Port);

			graphite.Send(new GraphiteValue("test-1", 1, DateTime.Now));
			server.Poll();
			server.Reset();

			graphite.Send(new GraphiteValue("test-2", 2, DateTime.Now));
			server.Poll();
			server.Stop();

			Check.That(() => r.Count == 2);
		}
	}
}
