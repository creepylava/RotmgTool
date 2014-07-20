using System;
using System.IO;
using RotmgTool.Network;

namespace RotmgTool.Commands
{
	internal class Tp : CommandBase
	{
		public Tp()
			: base("tp", "/tp <player name>")
		{
		}

		protected override bool Process(SocketProxyWorker client, string args)
		{
			var ms = new MemoryStream();
			using (var writer = new NWriter(ms))
				writer.WriteUTF("/teleport " + args);
			client.SendAsClient(client.PacketTable.PLAYERTEXT, ms.ToArray());
			return true;
		}
	}
}