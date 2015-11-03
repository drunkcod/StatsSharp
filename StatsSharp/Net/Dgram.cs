using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace StatsSharp.Net
{
	struct Dgram
	{
		readonly byte[] bytes;
		int position;

		public Dgram(int size) {
			this.bytes = new byte[size];
			this.position = 0;
		}

		public int Position => position;
		public int Capacity => bytes.Length - position;

		public void Append(string value, Encoding encoding) {
			position += encoding.GetBytes(value, 0, value.Length, bytes, position);
		} 

		public bool TryAppend(string name, Encoding encoding) {
			try {
				position += encoding.GetBytes(name, 0, name.Length, bytes, position);
				return true;
			} catch(ArgumentException) {
				return false;
			}
		}

		public void Append(MetricValue value) {
			value.Bytes.CopyTo(bytes, position);
			position += value.Bytes.Length;
		}

		public void Append(byte b) { bytes[position++] = b; }

		public void SendTo(Socket socket, EndPoint target) {
			SendTo(socket, target, position);
		}

		public void SendTo(Socket socket, EndPoint target, int count) {
			socket.SendTo(bytes, 0, count, SocketFlags.None, target);
		}

		public void Clear() { position = 0; }
	}
}