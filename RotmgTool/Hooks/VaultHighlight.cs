using System;
using System.Linq;
using RotmgTool.Network;

namespace RotmgTool.Hooks
{
	internal class VaultHighlight : HookBase
	{
		public VaultHighlight()
			: base("vaultHL", "Vault Highlight")
		{
		}

		public override void Attach(SocketProxyWorker worker)
		{
			worker.ServerPacketReceived += OnServerPacketReceived;
			worker.PostServerPacketReceived += OnPostServerPacketReceived;
		}

		private void UpdateStats(ref ObjectStats stats)
		{
			int count = 0;
			foreach (var i in stats.Stats)
			{
				if (i.Item1 >= StatsType.Inventory0 && i.Item1 <= StatsType.Inventory8 &&
				    (int)i.Item2 > 0)
					count++;
			}
			string name = string.Format("{0}/8", count);
			stats.Stats = stats.Stats.Concat(new[]
			{
				Tuple.Create(StatsType.Name, (object)name)
			}).ToArray();
		}

		private void UpdateStats(SocketProxyWorker worker, ref ObjectStats stats)
		{
			int count = 0;
			for (int i = 0; i < 8; i++)
			{
				var s = (StatsType)((int)StatsType.Inventory0 + i);
				if (worker.World[stats.Id].Stats.ContainsKey(s) && (int)worker.World[stats.Id].Stats[s] > 0)
					count++;
			}
			string name = string.Format("{0}/8", count);
			stats.Stats = stats.Stats.Concat(new[]
			{
				Tuple.Create(StatsType.Name, (object)name),
				Tuple.Create(StatsType.NameChosen, (object)1)
			}).ToArray();
		}

		private void OnServerPacketReceived(object sender, PacketEventArgs e)
		{
			var worker = (SocketProxyWorker)sender;

			if ((int)worker.GameId != -5)
				return;

			if (e.ID == worker.PacketTable.UPDATE)
			{
				bool enabled = IsEnabled(worker);
				if (!enabled) return;

				UpdatePacket packet = UpdatePacket.Read(new NBufferReader(e.Content));

				bool updated = false;
				for (int i = 0; i < packet.NewObjects.Length; i++)
					if (packet.NewObjects[i].ObjectType == 0x0504) //Vault Chest
					{
						UpdateStats(ref packet.NewObjects[i].Stats);
						updated = true;
					}

				if (updated)
					e.Content = NWriter.Write(writer => packet.Write(writer));
			}
			else if (e.ID == worker.PacketTable.MAPINFO)
			{
				MapInfoPacket packet = MapInfoPacket.Read(new NBufferReader(e.Content));

				packet.ClientXML = packet.ClientXML.Concat(new[]
				{
					@"	<Objects>
		<Object type=""0x0504"" id=""Vault Chest"">
			<Class>Container</Class>
			<Container/>
			<CanPutNormalObjects/>
			<CanPutSoulboundObjects/>
			<ShowName/>
			<Texture><File>lofiObj2</File><Index>0x0e</Index></Texture>
			<SlotTypes>0, 0, 0, 0, 0, 0, 0, 0</SlotTypes>
		</Object>
	</Objects>"
				}).ToArray();
				e.Content = NWriter.Write(writer => packet.Write(writer));
			}
		}

		private void OnPostServerPacketReceived(object sender, PacketEventArgs e)
		{
			var worker = (SocketProxyWorker)sender;
			if (e.ID == worker.PacketTable.NEW_TICK)
			{
				NewTickPacket packet = NewTickPacket.Read(new NBufferReader(e.Content));

				bool updated = false;
				for (int i = 0; i < packet.UpdateStats.Length; i++)
					if (worker.World[packet.UpdateStats[i].Id].ObjectType == 0x0504) //Vault Chest
					{
						UpdateStats(worker, ref packet.UpdateStats[i]);
						updated = true;
					}

				if (updated)
					e.Content = NWriter.Write(writer => packet.Write(writer));
			}
		}
	}
}