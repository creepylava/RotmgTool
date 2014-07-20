using System;
using RotmgTool.Network;

namespace RotmgTool.Hooks
{
	internal class Server : HookBase
	{
		public Server()
			: base("server", "Server")
		{
		}

		public override void Attach(SocketProxyWorker worker)
		{
			worker.ClientPacketReceived += OnClientPacketReceived;
		}

		private void OnClientPacketReceived(object sender, PacketEventArgs e)
		{
			var worker = (SocketProxyWorker)sender;
			if (e.ID == worker.PacketTable.PLAYERTEXT)
			{
				string text = new NBufferReader(e.Content).ReadUTF();

				if (text.EqualsIgnoreCase("/server") && IsEnabled(worker))
				{
					e.Send = false;

					if (worker.ConnectionName != null)
					{
						if (worker.ConnectionName.StartsWith("NexusPortal."))
							worker.SendText("", string.Format("{0} {1}", worker.ServerName, worker.ConnectionName.Substring(12)));
						else
							worker.SendText("", string.Format("{0} {1}", worker.ServerName, worker.ConnectionName));
					}
					else
						worker.SendText("", worker.ServerName);
				}
			}
		}
	}
}