using System;
using System.Collections.Generic;
using System.Threading;
using RotmgTool.Network;

namespace RotmgTool
{
	internal class SocketHandler
	{
		private readonly IToolInstance tool;

		public SocketHandler(IToolInstance tool)
		{
			this.tool = tool;
		}

		private readonly Dictionary<SocketProxyWorker, LinkedListNode<SocketProxyWorker>> nodeDictionary =
			new Dictionary<SocketProxyWorker, LinkedListNode<SocketProxyWorker>>();

		private readonly LinkedList<SocketProxyWorker> workers = new LinkedList<SocketProxyWorker>();
		private int id;

		public void Attach()
		{
			tool.SocketProxy.ServerPacketReceived += OnServerPacketReceived;
			tool.SocketProxy.ClientPacketReceived += OnClientPacketReceived;
			tool.SocketProxy.ClientConnected += (sender, e) =>
			{
				e.Client.ID = Interlocked.Increment(ref id);
				tool.Hooks.AttachHooks(e.Client);
				if (e.Client.IsConnectionAlive)
				{
					var node = workers.AddFirst(e.Client);
					nodeDictionary.Add(e.Client, node);
					e.Client.Disconnected += (s, f) =>
					{
						workers.Remove(node);
						UpdateActiveWorker();
					};
					UpdateActiveWorker();
				}
			};
		}

		private void UpdateActiveWorker()
		{
			if (tool.MainForm.InvokeRequired)
			{
				tool.MainForm.BeginInvoke(new Action(UpdateActiveWorker));
				return;
			}
			var active = workers.First;
			if (active == null)
				tool.Windows.SetActiveWorker(null);
			else
				tool.Windows.SetActiveWorker(active.Value);
		}

		private void OnServerPacketReceived(object sender, PacketEventArgs e)
		{
			var worker = (SocketProxyWorker)sender;

			if (e.ID == tool.PacketTable.TEXT)
			{
				TextPacket packet = TextPacket.Read(new NBufferReader(e.Content));
				e.Send = !tool.Filter.IsSpam(packet);

				var logSpam = tool.Settings.GetValue<bool>("spam.log", "true");

				if (logSpam && !e.Send)
					tool.AppendLog("<{0}> {1}", packet.name, packet.text);
			}
			else if (e.ID == tool.PacketTable.FAILURE)
			{
				var reader = new NBufferReader(e.Content) { Position = 4 };
				string msg = reader.ReadUTF();
				tool.AppendLog("****{0}****", msg);
			}
		}

		private void OnClientPacketReceived(object sender, PacketEventArgs e)
		{
			if (e.ID == tool.PacketTable.PLAYERTEXT)
			{
				string text = new NBufferReader(e.Content).ReadUTF();

				if (text[0] == '/' && tool.Commands.Execute((SocketProxyWorker)sender, text) != null)
					e.Send = false;
			}
		}
	}
}