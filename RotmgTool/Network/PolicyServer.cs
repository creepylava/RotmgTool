using System;
using System.Net;
using System.Net.Sockets;

namespace RotmgTool.Network
{
	internal class PolicyServer
	{
		private readonly TcpListener listener;

		public PolicyServer()
		{
			listener = new TcpListener(IPAddress.Any, 843);
		}

		private static void ServePolicyFile(IAsyncResult ar)
		{
			TcpClient cli = null;
			try
			{
				cli = (ar.AsyncState as TcpListener).EndAcceptTcpClient(ar);
			}
			catch
			{
			}
			try
			{
				(ar.AsyncState as TcpListener).BeginAcceptTcpClient(ServePolicyFile, ar.AsyncState);
			}
			catch
			{
			}
			if (cli == null) return;
			if (!(cli.Client.RemoteEndPoint is IPEndPoint) ||
			    !IPAddress.IsLoopback(((IPEndPoint)cli.Client.RemoteEndPoint).Address))
			{
				cli.Close();
				return;
			}
			try
			{
				var s = cli.GetStream();
				var rdr = new NReader(s);
				var writer = new NWriter(s);
				if (rdr.ReadNullTerminatedString() == "<policy-file-request/>")
				{
					writer.WriteNullTerminatedString(@"<cross-domain-policy>
     <allow-access-from domain=""*"" to-ports=""*"" />
</cross-domain-policy>");
					writer.Write((byte)'\r');
					writer.Write((byte)'\n');
				}
				cli.Close();
			}
			catch
			{
			}
		}

		private bool started;

		public bool Start()
		{
			try
			{
				listener.Start();
				listener.BeginAcceptTcpClient(ServePolicyFile, listener);
				started = true;
			}
			catch
			{
				started = false;
			}
			return started;
		}

		public void Stop()
		{
			if (started)
			{
				listener.Stop();
			}
		}
	}
}