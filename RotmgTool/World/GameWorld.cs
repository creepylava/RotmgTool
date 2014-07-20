using System;
using System.Collections.Generic;
using System.Net;
using RotmgTool.Network;

namespace RotmgTool.World
{
	internal class WorldEntityEventArgs : EventArgs
	{
		public WorldEntityEventArgs(Entity entity)
		{
			Entity = entity;
		}

		public Entity Entity { get; private set; }
	}

	internal class GameWorld : Dictionary<uint, Entity>
	{
		private readonly SocketProxyWorker worker;

		private GameWorld(SocketProxyWorker worker, uint w, uint h, string name)
		{
			this.worker = worker;
			Width = w;
			Height = h;
			Name = name;
			SelfID = 0xffffffff;
		}

		public static GameWorld CreateWorld(MapInfoPacket packet, SocketProxyWorker worker)
		{
			var ret = new GameWorld(worker, packet.Width, packet.Height, packet.Name);
			return ret;
		}

		internal void OnPacketReceived(object sender, PacketEventArgs e)
		{
			var worker = (SocketProxyWorker)sender;
			if (e.ID == worker.PacketTable.UPDATE)
			{
				var packet = UpdatePacket.Read(new NBufferReader(e.Content));
				Update(packet);
			}
			else if (e.ID == worker.PacketTable.MOVE)
			{
				var packet = MovePacket.Read(new NBufferReader(e.Content));
				Update(packet);
			}
			else if (e.ID == worker.PacketTable.NEW_TICK)
			{
				var packet = NewTickPacket.Read(new NBufferReader(e.Content));
				Update(packet);
			}
			else if (e.ID == worker.PacketTable.CREATE_SUCCESS)
			{
				SelfID = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(e.Content, 0));
			}
		}

		public string Name { get; private set; }
		public uint Width { get; private set; }
		public uint Height { get; private set; }

		public uint SelfID { get; set; }
		public uint Time { get; set; }

		private void Update(UpdatePacket packet)
		{
			foreach (var obj in packet.NewObjects)
			{
				var entity = new Entity(obj.Stats.Id, obj.ObjectType)
				{
					Position = obj.Stats.Position
				};
				foreach (var stats in obj.Stats.Stats)
					entity.Stats[stats.Item1] = stats.Item2;
				this[entity.ID] = entity;
				EntityAdded(this, new WorldEntityEventArgs(entity));
			}
			foreach (var id in packet.RemovedObjectIds)
			{
				if (ContainsKey(id))
					EntityRemoved(this, new WorldEntityEventArgs(this[id]));
				Remove(id);
			}
		}

		private void Update(MovePacket packet)
		{
			Entity self = this[SelfID];
			self.Position = packet.Position;
			Time = packet.Time;

			self.OnUpdated();
		}

		private void Update(NewTickPacket packet)
		{
			foreach (var i in packet.UpdateStats)
			{
				Entity entity = this[i.Id];
				entity.Position = i.Position;
				foreach (var stats in i.Stats)
					entity.Stats[stats.Item1] = stats.Item2;
				entity.OnUpdated();
			}
		}

		public Entity GetPlayer(string name)
		{
			var xmlData = worker.Proxy.Tool.LoadXmlData(worker.Version);
			Entity player = null;

			foreach (var p in Values)
				if (xmlData.PlayerTypes.Contains(p.ObjectType) &&
				    p.GetStats<string>(StatsType.Name).EqualsIgnoreCase(name))
				{
					if (player == null)
						player = p;
					else
						return null; //not unique
				}
			return player;
		}

		public IEnumerable<Entity> GetPlayers()
		{
			var xmlData = worker.Proxy.Tool.LoadXmlData(worker.Version);
			foreach (var p in Values)
				if (xmlData.PlayerTypes.Contains(p.ObjectType))
					yield return p;
		}

		public event EventHandler<WorldEntityEventArgs> EntityAdded = delegate { };
		public event EventHandler<WorldEntityEventArgs> EntityRemoved = delegate { };
	}
}