using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using RotmgTool.World;

namespace RotmgTool.Network
{
	internal class PacketEventArgs : EventArgs
	{
		public PacketEventArgs(byte id, byte[] content)
		{
			ID = id;
			Content = content;
			Send = true;
		}

		public byte ID { get; set; }
		public byte[] Content { get; set; }
		public bool Send { get; set; }
	}

	internal class SocketProxyWorker
	{
		private delegate bool HandlePacket(ref byte id, ref byte[] content);

		private readonly TcpClient client;
		private TcpClient server;
		private Thread cliWkr;
		private Thread svrWkr;
		public RC4 ReceiveKey1 { get; private set; }
		public RC4 ReceiveKey2 { get; private set; }
		public RC4 SendKey1 { get; private set; }
		public RC4 SendKey2 { get; private set; }

		public IPEndPoint LocalEndPoint
		{
			get { return (IPEndPoint)server.Client.LocalEndPoint; }
		}

		private readonly string local;
		private readonly string destDns;
		private readonly int destPort;
		public SocketProxy Proxy { get; private set; }
		public string ServerName { get; private set; }
		public PacketTable PacketTable { get; private set; }

		public int ID { get; set; }

		private void AppendLog(string text, params object[] args)
		{
			Proxy.Tool.AppendLog("[Client " + ID + "] " + text, args);
		}

		public SocketProxyWorker(SocketProxy proxy, string local, string dest, string serverName, TcpClient client)
		{
			this.client = client;

			this.local = local;

			destDns = dest;
			destPort = 2050;

			Proxy = proxy;
			ServerName = serverName;
			PacketTable = proxy.Tool.PacketTable;

			ReceiveKey1 = new RC4(new byte[] { 0x31, 0x1f, 0x80, 0x69, 0x14, 0x51, 0xc7, 0x1d, 0x09, 0xa1, 0x3a, 0x2a, 0x6e });
			ReceiveKey2 = new RC4(new byte[] { 0x31, 0x1f, 0x80, 0x69, 0x14, 0x51, 0xc7, 0x1d, 0x09, 0xa1, 0x3a, 0x2a, 0x6e });
			SendKey1 = new RC4(new byte[] { 0x72, 0xc5, 0x58, 0x3c, 0xaf, 0xb6, 0x81, 0x89, 0x95, 0xcd, 0xd7, 0x4b, 0x80 });
			SendKey2 = new RC4(new byte[] { 0x72, 0xc5, 0x58, 0x3c, 0xaf, 0xb6, 0x81, 0x89, 0x95, 0xcd, 0xd7, 0x4b, 0x80 });

			cliWkr = new Thread(() => ProcessLoop(client, null, ReceiveKey1, ReceiveKey2, sendServer, OnClientPacketReceived));
			cliWkr.Start();

			proxy.Stopped += Kill;
		}

		private bool OnClientPacketReceived(ref byte id, ref byte[] content)
		{
			bool ret = true;
			var e = new PacketEventArgs(id, content);
			if (ClientPacketReceived != null)
			{
				ClientPacketReceived(this, e);
				id = e.ID;
				content = e.Content;
				ret &= e.Send;
			}
			if (ret && World != null)
				World.OnPacketReceived(this, e);
			if (ret && PostClientPacketReceived != null)
			{
				PostClientPacketReceived(this, e);
				id = e.ID;
				content = e.Content;
				ret &= e.Send;
			}
			return ret;
		}

		public GameWorld World { get; private set; }

		private static readonly ConcurrentDictionary<string, string> serverNames = new ConcurrentDictionary<string, string>();

		private static readonly ConcurrentDictionary<int, Tuple<string, int, byte[]>> reconnKeys =
			new ConcurrentDictionary<int, Tuple<string, int, byte[]>>();

		private readonly Random rand = new Random();

		private bool OnServerPacketReceived(ref byte id, ref byte[] content)
		{
			if (id == PacketTable.RECONNECT)
			{
				ReconnPacket reconn = ReconnPacket.Read(new NBufferReader(content));

				var endPoint = (IPEndPoint)server.Client.RemoteEndPoint;

				int reconnId = rand.Next();
				while (!reconnKeys.TryAdd(reconnId, Tuple.Create(
					string.IsNullOrEmpty(reconn.Host) ? endPoint.Address.ToString() : reconn.Host,
					reconn.Port == 0xffffffff ? endPoint.Port : (int)reconn.Port,
					reconn.Key)))
				{
					reconnId = rand.Next();
				}

				if (reconn.Name.StartsWith("NexusPortal."))
				{
					var entry = Dns.GetHostEntry(reconn.Host);
					serverNames[entry.AddressList[0].ToString()] = reconn.Name;
				}
				AppendLog("Reconnect to '{0}'.", reconnKeys[reconnId].Item1);

				reconn.Host = local;
				reconn.Port = 2050;
				reconn.Key = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(reconnId));

				content = NWriter.Write(writer => reconn.Write(writer));
			}
			else if (id == PacketTable.MAPINFO)
			{
				MapInfoPacket p = MapInfoPacket.Read(new NBufferReader(content));
				if (ConnectionName == null)
					ConnectionName = p.Name;

				World = GameWorld.CreateWorld(p, this);
				if (MapCreated != null)
					MapCreated(this, EventArgs.Empty);
			}

			bool ret = true;
			var e = new PacketEventArgs(id, content);
			if (ServerPacketReceived != null)
			{
				ServerPacketReceived(this, e);
				id = e.ID;
				content = e.Content;
				ret &= e.Send;
			}
			if (ret && World != null)
				World.OnPacketReceived(this, e);
			if (ret && PostServerPacketReceived != null)
			{
				PostServerPacketReceived(this, e);
				id = e.ID;
				content = e.Content;
				ret &= e.Send;
			}
			return ret;
		}

		public event EventHandler<PacketEventArgs> ServerPacketReceived;
		public event EventHandler<PacketEventArgs> ClientPacketReceived;
		public event EventHandler<PacketEventArgs> PostServerPacketReceived;
		public event EventHandler<PacketEventArgs> PostClientPacketReceived;
		public event EventHandler Disconnected;
		public event EventHandler MapCreated;

		public bool IsConnectionAlive
		{
			get { return client.Connected || (server != null && server.Connected); }
		}

		public string Version { get; private set; }
		public uint GameId { get; private set; }
		public int ConnectionId { get; private set; }
		public string ConnectionName { get; private set; }

		private NWriter OnHello(HelloPacket packet)
		{
			string dns = destDns;
			int port = destPort;

			Tuple<string, int, byte[]> x;
			int key;
			if (packet.Key.Length == 4 &&
			    reconnKeys.TryGetValue(key = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(packet.Key, 0)), out x))
			{
				dns = x.Item1;
				port = x.Item2;
				packet.Key = x.Item3;
				ConnectionId = key;
			}
			else
				ConnectionId = -1;

			server = new TcpClient();
			server.Connect(dns, port);
			AppendLog("Connected to remote '{0}'.", dns);

			var adr = ((IPEndPoint)server.Client.RemoteEndPoint).Address.ToString();
			if (serverNames.ContainsKey(adr))
				ConnectionName = serverNames[adr];

			Version = packet.BuildVer;
			GameId = packet.GameId;

			svrWkr = new Thread(() => ProcessLoop(server, client, SendKey1, SendKey2, sendClient, OnServerPacketReceived));
			svrWkr.Start();

			return new NWriter(server.GetStream());
		}

		private readonly Queue<Tuple<byte, byte[]>> sendServer = new Queue<Tuple<byte, byte[]>>();
		private readonly Queue<Tuple<byte, byte[]>> sendClient = new Queue<Tuple<byte, byte[]>>();

		public void SendAsServer(byte id, byte[] content)
		{
			lock (sendClient)
				sendClient.Enqueue(Tuple.Create(id, content));
		}

		public void SendAsClient(byte id, byte[] content)
		{
			lock (sendServer)
				sendServer.Enqueue(Tuple.Create(id, content));
		}

		public void SendText(string name, string text)
		{
			var packet = NWriter.Write(writer => new TextPacket
			{
				name = name,
				text = text,
				star = 0xffffffff
			}.Write(writer));
			SendAsServer(PacketTable.TEXT, packet);
		}

		private void ProcessLoop(TcpClient src, TcpClient dst, RC4 i, RC4 o, Queue<Tuple<byte, byte[]>> queue, HandlePacket cb)
		{
			try
			{
				var rdr = new NReader(src.GetStream());
				NWriter writer = dst == null ? null : new NWriter(dst.GetStream());
				while (true)
				{
					int len = rdr.ReadInt32();
					byte id = rdr.ReadByte();
					byte[] content = rdr.ReadBytes(len - 5);

					i.Crypt(content, 0, content.Length);

					bool send = true;
					if (id == PacketTable.HELLO)
					{
						HelloPacket helloPacket = HelloPacket.Read(new NBufferReader(content));
						writer = OnHello(helloPacket);
						content = NWriter.Write(w => helloPacket.Write(w));
					}

					send = cb(ref id, ref content);

					if (send)
					{
						o.Crypt(content, 0, content.Length);

						writer.Write(content.Length + 5);
						writer.Write(id);
						writer.Write(content);
					}

					if (queue.Count > 0)
						lock (queue)
						{
							Tuple<byte, byte[]> packet;
							while (queue.Count > 0)
							{
								packet = queue.Dequeue();
								o.Crypt(packet.Item2, 0, packet.Item2.Length);

								writer.Write(packet.Item2.Length + 5);
								writer.Write(packet.Item1);
								writer.Write(packet.Item2);
							}
						}
				}
			}
			catch
			{
			}
			finally
			{
				OnDisconnected(src, dst);
			}
			Proxy.Stopped -= Kill;
		}

		private readonly object disconnectLock = new object();
		private bool disconnected;

		private void OnDisconnected(TcpClient src, TcpClient dst)
		{
			lock (disconnectLock)
			{
				src.Close();

				try
				{
					dst.Close();
				}
				catch
				{
				}

				if (!disconnected)
				{
					AppendLog("Disconnected.");
					if (Disconnected != null)
						Disconnected(this, EventArgs.Empty);
					disconnected = true;
				}
			}
		}

		public void Kill(object sender, EventArgs e)
		{
			try
			{
				client.Close();
			}
			catch
			{
			}

			try
			{
				server.Close();
			}
			catch
			{
			}
		}
	}
}