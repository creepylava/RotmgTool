using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace RotmgTool.Network
{
	internal class ConnectedEventArgs : EventArgs
	{
		public ConnectedEventArgs(SocketProxyWorker client)
		{
			Client = client;
		}

		public SocketProxyWorker Client { get; private set; }
	}

	internal class SocketProxy
	{
		public IToolInstance Tool { get; private set; }

		public SocketProxy(IToolInstance tool)
		{
			Tool = tool;
		}

		private TcpListener[] listeners;

		public void Start()
		{
			listeners = new TcpListener[Tool.Servers.Length];
			for (int i = 0; i < Tool.Servers.Length; i++)
			{
				listeners[i] = new TcpListener(Tool.Servers[i].Loopback, 2050);
				listeners[i].Start();
				listeners[i].BeginAcceptTcpClient(TcpClientAccepted, listeners[i]);
			}
		}

		private bool stopped;

		public void Stop()
		{
			stopped = true;
			foreach (var i in listeners)
			{
				if (i != null)
					i.Stop();
			}
			if (Stopped != null)
				Stopped(this, EventArgs.Empty);
		}

		private void TcpClientAccepted(IAsyncResult ar)
		{
			if (stopped)
				return;

			var listener = (TcpListener)ar.AsyncState;
			TcpClient client = null;
			try
			{
				client = listener.EndAcceptTcpClient(ar);
			}
			catch
			{
				return;
			}

			listener.BeginAcceptTcpClient(TcpClientAccepted, listener);

			IPAddress loopBack = ((IPEndPoint)listener.LocalEndpoint).Address;
			var redir = Tool.Servers.Single(server => server.Loopback.Equals(loopBack));

			if (client != null)
			{
				var worker = new SocketProxyWorker(this, loopBack.ToString(), redir.DNS, redir.Name, client);
				worker.ServerPacketReceived += ServerPacketReceived;
				worker.ClientPacketReceived += ClientPacketReceived;
				if (ClientConnected != null)
					ClientConnected(this, new ConnectedEventArgs(worker));
			}
		}

		public event EventHandler<PacketEventArgs> ServerPacketReceived;
		public event EventHandler<PacketEventArgs> ClientPacketReceived;
		public event EventHandler<ConnectedEventArgs> ClientConnected;

		public event EventHandler Stopped;
	}
}