using System;
using RotmgTool.Network;
using RotmgTool.World;

namespace RotmgTool.Commands
{
	internal class Lock : CommandBase
	{
		public Lock()
			: base("lock", "/lock [player name]")
		{
		}

		protected override bool Process(SocketProxyWorker client, string args)
		{
			Entity player = client.World.GetPlayer(args);

			if (player == null)
			{
				client.SendText("*Error*", "Player not found!");
			}

			var packet = NWriter.Write(writer => new EditAccountListPacket
			{
				AccountListId = 0,
				Add = true,
				ObjectId = player.ID
			}.Write(writer));
			client.SendAsClient(client.PacketTable.EDITACCOUNTLIST, packet);

			packet = NWriter.Write(writer => new AccountListPacket
			{
				AccountListId = 0,
				AccountIds = new[] { player.Stats[StatsType.AccountId].ToString() }
			}.Write(writer));
			client.SendAsServer(client.PacketTable.ACCOUNTLIST, packet);

			client.SendText("", "Added " + player.GetStats<string>(StatsType.Name) + " to starred list");
			return true;
		}
	}

	internal class Unlock : CommandBase //client's starred list cannot be updated :P
	{
		public Unlock()
			: base("unlock", "/unlock [player name]")
		{
		}

		protected override bool Process(SocketProxyWorker client, string args)
		{
			Entity player = client.World.GetPlayer(args);

			if (player == null)
			{
				client.SendText("*Error*", "Player not found!");
			}


			var packet = NWriter.Write(writer => new EditAccountListPacket
			{
				AccountListId = 0,
				Add = false,
				ObjectId = player.ID
			}.Write(writer));
			client.SendAsClient(client.PacketTable.EDITACCOUNTLIST, packet);

			client.SendText("", "Removed " + player.GetStats<string>(StatsType.Name) + " from starred list");
			return true;
		}
	}
}