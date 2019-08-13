using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace VSCodeDebug
{
	class StreamTransportConnection : Mono.Debugger.Soft.Connection
	{
		Stream stream;

		internal StreamTransportConnection(Stream stream) : base(new StringWriter())
		{
			this.stream = stream;
		}

		protected override int TransportSend(byte[] buf, int buf_offset, int len)
		{
			stream.Write(buf, buf_offset, len);
			return len;
		}

		protected override int TransportReceive(byte[] buf, int buf_offset, int len)
		{
			return stream.Read(buf, buf_offset, len);
		}

		protected override void TransportSetTimeouts(int send_timeout, int receive_timeout)
		{
			Console.WriteLine("Resources.StreamTransportConnection_NotSupported", send_timeout, receive_timeout);
		}

		protected override void TransportClose()
		{
			stream.Close();
		}
	}
}