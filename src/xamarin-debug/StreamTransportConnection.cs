using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace VSCodeDebug
{
	class IPhoneTransportConnection : Mono.Debugger.Soft.Connection {
		readonly StreamCommandConnection connection;
		readonly Stream stream;

		internal IPhoneTransportConnection (StreamCommandConnection connection, Stream stream)
		{
			this.connection = connection;
			this.stream = stream;
		}

		protected override int TransportSend (byte [] buf, int buf_offset, int len)
		{
			stream.Write (buf, buf_offset, len);
			return len;
		}

		protected override int TransportReceive (byte [] buf, int buf_offset, int len)
		{
			return stream.Read (buf, buf_offset, len);
		}

		protected override void TransportSetTimeouts (int send_timeout, int receive_timeout)
		{
			Console.WriteLine ("StreamTransportConnection.TransportSetTimeouts ({0}, {1}): Not supported", send_timeout, receive_timeout);
		}

		protected override void TransportClose ()
		{
			connection.Dispose ();
			stream.Close ();
		}
		protected override void TransportShutdown ()
		{
			connection.Dispose ();
			stream.Close ();
		}

	}
}