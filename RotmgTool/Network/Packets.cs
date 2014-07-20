using System;

namespace RotmgTool.Network
{
	internal class HelloPacket
	{
		public string BuildVer;
		public uint GameId;
		public string Guid;
		public uint Rand1;
		public string Password;
		public uint Rand2;
		public string Secret;
		public uint KeyTime;
		public byte[] Key;
		public string MapInfo;
		public string X1;
		public string X2;
		public string X3;
		public string X4;
		public string X5;

		public static HelloPacket Read(NBufferReader reader)
		{
			var ret = new HelloPacket();
			ret.BuildVer = reader.ReadUTF();
			ret.GameId = reader.ReadUInt32();
			ret.Guid = reader.ReadUTF();
			ret.Rand1 = reader.ReadUInt32();
			ret.Password = reader.ReadUTF();
			ret.Rand2 = reader.ReadUInt32();
			ret.Secret = reader.ReadUTF();
			ret.KeyTime = reader.ReadUInt32();
			ret.Key = reader.ReadBytes(reader.ReadUInt16());
			ret.MapInfo = reader.Read32UTF();
			ret.X1 = reader.ReadUTF();
			ret.X2 = reader.ReadUTF();
			ret.X3 = reader.ReadUTF();
			ret.X4 = reader.ReadUTF();
			ret.X5 = reader.ReadUTF();
			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.WriteUTF(BuildVer);
			writer.Write(GameId);
			writer.WriteUTF(Guid);
			writer.Write(Rand1);
			writer.WriteUTF(Password);
			writer.Write(Rand2);
			writer.WriteUTF(Secret);
			writer.Write(KeyTime);
			writer.Write((short)Key.Length);
			writer.Write(Key);
			writer.Write32UTF(MapInfo);
			writer.WriteUTF(X1);
			writer.WriteUTF(X2);
			writer.WriteUTF(X3);
			writer.WriteUTF(X4);
			writer.WriteUTF(X5);
		}
	}

	internal class ReconnPacket
	{
		public string Name;
		public string Host;
		public uint Port;
		public uint GameId;
		public uint KeyTime;
		public byte[] Key;
		public bool IsFromArena;

		public static ReconnPacket Read(NBufferReader reader)
		{
			var ret = new ReconnPacket();
			ret.Name = reader.ReadUTF();
			ret.Host = reader.ReadUTF();
			ret.Port = reader.ReadUInt32();
			ret.GameId = reader.ReadUInt32();
			ret.KeyTime = reader.ReadUInt32();
			ret.IsFromArena = reader.ReadBoolean();
			ret.Key = reader.ReadBytes(reader.ReadUInt16());
			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.WriteUTF(Name);
			writer.WriteUTF(Host);
			writer.Write(Port);
			writer.Write(GameId);
			writer.Write(KeyTime);
			writer.Write(IsFromArena);
			writer.Write((short)Key.Length);
			writer.Write(Key);
		}
	}

	internal class TextPacket
	{
		public string name;
		public uint objectId;
		public uint star;
		public byte bubbleTime;
		public string recipient;
		public string text;
		public string cleanText;

		public static TextPacket Read(NBufferReader reader)
		{
			var ret = new TextPacket();
			ret.name = reader.ReadUTF();
			ret.objectId = reader.ReadUInt32(); //objectId
			ret.star = reader.ReadUInt32();
			ret.bubbleTime = reader.ReadByte(); //bubbleTime
			ret.recipient = reader.ReadUTF(); //recipient
			ret.text = reader.ReadUTF();
			ret.cleanText = reader.ReadUTF(); //cleanText
			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.WriteUTF(name);
			writer.Write(objectId); //objectId
			writer.Write(star);
			writer.Write(bubbleTime); //bubbleTime
			writer.WriteUTF(recipient); //recipient
			writer.WriteUTF(text);
			writer.WriteUTF(cleanText); //cleanText
		}
	}

	internal class InvSwapPacket
	{
		public uint Time;
		public Position Position;
		public ObjectSlot Obj1;
		public ObjectSlot Obj2;

		public static InvSwapPacket Read(NBufferReader reader)
		{
			var ret = new InvSwapPacket();
			ret.Time = reader.ReadUInt32();
			ret.Position = Position.Read(reader);
			ret.Obj1 = ObjectSlot.Read(reader);
			ret.Obj2 = ObjectSlot.Read(reader);
			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.Write(Time);
			Position.Write(writer);
			Obj1.Write(writer);
			Obj2.Write(writer);
		}
	}

	internal class InvDropPacket
	{
		public ObjectSlot Obj;

		public static InvDropPacket Read(NBufferReader reader)
		{
			var ret = new InvDropPacket();
			ret.Obj = ObjectSlot.Read(reader);
			return ret;
		}

		public void Write(NWriter writer)
		{
			Obj.Write(writer);
		}
	}

	internal class UpdatePacket
	{
		public TileData[] Tiles;
		public ObjectDef[] NewObjects;
		public uint[] RemovedObjectIds;

		public static UpdatePacket Read(NBufferReader reader)
		{
			var ret = new UpdatePacket();

			ret.Tiles = new TileData[reader.ReadUInt16()];
			for (var i = 0; i < ret.Tiles.Length; i++)
			{
				ret.Tiles[i] = new TileData
				{
					X = reader.ReadUInt16(),
					Y = reader.ReadUInt16(),
					Tile = reader.ReadUInt16()
				};
			}

			ret.NewObjects = new ObjectDef[reader.ReadUInt16()];
			for (var i = 0; i < ret.NewObjects.Length; i++)
				ret.NewObjects[i] = ObjectDef.Read(reader);

			ret.RemovedObjectIds = new uint[reader.ReadUInt16()];
			for (var i = 0; i < ret.RemovedObjectIds.Length; i++)
				ret.RemovedObjectIds[i] = reader.ReadUInt32();

			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.Write((short)Tiles.Length);
			foreach (var i in Tiles)
			{
				writer.Write(i.X);
				writer.Write(i.Y);
				writer.Write(i.Tile);
			}
			writer.Write((short)NewObjects.Length);
			foreach (var i in NewObjects)
			{
				i.Write(writer);
			}
			writer.Write((short)RemovedObjectIds.Length);
			foreach (var i in RemovedObjectIds)
			{
				writer.Write(i);
			}
		}
	}

	internal class MovePacket
	{
		public uint TickId;
		public uint Time;
		public Position Position;
		public TimedPosition[] Records;

		public static MovePacket Read(NBufferReader reader)
		{
			var ret = new MovePacket();
			ret.TickId = reader.ReadUInt32();
			ret.Time = reader.ReadUInt32();
			ret.Position = Position.Read(reader);
			ret.Records = new TimedPosition[reader.ReadUInt16()];
			for (var i = 0; i < ret.Records.Length; i++)
				ret.Records[i] = TimedPosition.Read(reader);
			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.Write(TickId);
			writer.Write(Time);
			Position.Write(writer);
			writer.Write((short)Records.Length);
			foreach (var i in Records)
				i.Write(writer);
		}
	}

	internal class NewTickPacket
	{
		public uint TickId;
		public uint TickTime;
		public ObjectStats[] UpdateStats;

		public static NewTickPacket Read(NBufferReader reader)
		{
			var ret = new NewTickPacket();
			ret.TickId = reader.ReadUInt32();
			ret.TickTime = reader.ReadUInt32();

			ret.UpdateStats = new ObjectStats[reader.ReadUInt16()];
			for (var i = 0; i < ret.UpdateStats.Length; i++)
				ret.UpdateStats[i] = ObjectStats.Read(reader);

			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.Write(TickId);
			writer.Write(TickTime);

			writer.Write((short)UpdateStats.Length);
			foreach (var i in UpdateStats)
				i.Write(writer);
		}
	}

	internal class MapInfoPacket
	{
		public uint Width;
		public uint Height;
		public string Name;
		public string Name2;
		public uint Seed;
		public uint Background;
		public uint Diffculty;
		public bool AllowTeleport;
		public bool ShowDisplays;
		public string[] ClientXML;
		public string[] ExtraXML;

		public static MapInfoPacket Read(NBufferReader reader)
		{
			var ret = new MapInfoPacket();
			ret.Width = reader.ReadUInt32();
			ret.Height = reader.ReadUInt32();
			ret.Name = reader.ReadUTF();
			ret.Name2 = reader.ReadUTF();
			ret.Seed = reader.ReadUInt32();
			ret.Background = reader.ReadUInt32();
			ret.Diffculty = reader.ReadUInt32();
			ret.AllowTeleport = reader.ReadBoolean();
			ret.ShowDisplays = reader.ReadBoolean();

			ret.ClientXML = new string[reader.ReadUInt16()];
			for (int i = 0; i < ret.ClientXML.Length; i++)
				ret.ClientXML[i] = reader.Read32UTF();

			ret.ExtraXML = new string[reader.ReadUInt16()];
			for (int i = 0; i < ret.ExtraXML.Length; i++)
				ret.ExtraXML[i] = reader.Read32UTF();

			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.Write(Width);
			writer.Write(Height);
			writer.WriteUTF(Name);
			writer.WriteUTF(Name2);
			writer.Write(Seed);
			writer.Write(Background);
			writer.Write(Diffculty);
			writer.Write(AllowTeleport);
			writer.Write(ShowDisplays);

			writer.Write((short)ClientXML.Length);
			foreach (var i in ClientXML)
				writer.Write32UTF(i);

			writer.Write((short)ExtraXML.Length);
			foreach (var i in ExtraXML)
				writer.Write32UTF(i);
		}
	}

	internal class EditAccountListPacket
	{
		public uint AccountListId;
		public bool Add;
		public uint ObjectId;

		public static EditAccountListPacket Read(NBufferReader reader)
		{
			var ret = new EditAccountListPacket();
			ret.AccountListId = reader.ReadUInt32();
			ret.Add = reader.ReadBoolean();
			ret.ObjectId = reader.ReadUInt32();
			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.Write(AccountListId);
			writer.Write(Add);
			writer.Write(ObjectId);
		}
	}

	internal class AccountListPacket
	{
		public uint AccountListId;
		public string[] AccountIds;

		public static AccountListPacket Read(NBufferReader reader)
		{
			var ret = new AccountListPacket();
			ret.AccountListId = reader.ReadUInt32();
			ret.AccountIds = new string[reader.ReadUInt16()];
			for (int i = 0; i < ret.AccountIds.Length; i++)
				ret.AccountIds[i] = reader.ReadUTF();
			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.Write(AccountListId);
			writer.Write((short)AccountIds.Length);
			foreach (var i in AccountIds)
				writer.WriteUTF(i);
		}
	}

	internal class PlayerTextPacket
	{
		public string Text;

		public static PlayerTextPacket Read(NBufferReader reader)
		{
			var ret = new PlayerTextPacket();
			ret.Text = reader.ReadUTF();
			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.WriteUTF(Text);
		}
	}

	internal class TradeStartPacket
	{
		public TradeItem[] MyItems;
		public string YourName;
		public TradeItem[] YourItems;

		public static TradeStartPacket Read(NBufferReader reader)
		{
			var ret = new TradeStartPacket();
			ret.MyItems = new TradeItem[reader.ReadUInt16()];
			for (int i = 0; i < ret.MyItems.Length; i++)
				ret.MyItems[i] = TradeItem.Read(reader);

			ret.YourName = reader.ReadUTF();
			ret.YourItems = new TradeItem[reader.ReadUInt16()];
			for (int i = 0; i < ret.YourItems.Length; i++)
				ret.YourItems[i] = TradeItem.Read(reader);

			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.Write((short)MyItems.Length);
			foreach (var i in MyItems)
				i.Write(writer);

			writer.WriteUTF(YourName);
			writer.Write((short)YourItems.Length);
			foreach (var i in YourItems)
				i.Write(writer);
		}
	}

	internal class UseItemPacket
	{
		public uint Time;
		public ObjectSlot Slot;
		public Position Position;
		public byte UseType;

		public static UseItemPacket Read(NBufferReader reader)
		{
			var ret = new UseItemPacket();
			ret.Time = reader.ReadUInt32();
			ret.Slot = ObjectSlot.Read(reader);
			ret.Position = Position.Read(reader);
			ret.UseType = reader.ReadByte();
			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.Write(Time);
			Slot.Write(writer);
			Position.Write(writer);
			writer.Write(UseType);
		}
	}

	internal class ShowEffectPacket
	{
		public EffectType EffectType;
		public uint TargetId;
		public Position PosA;
		public Position PosB;
		public ARGB Color;

		public static ShowEffectPacket Read(NBufferReader reader)
		{
			var ret = new ShowEffectPacket();
			ret.EffectType = (EffectType)reader.ReadByte();
			ret.TargetId = reader.ReadUInt32();
			ret.PosA = Position.Read(reader);
			ret.PosB = Position.Read(reader);
			ret.Color = ARGB.Read(reader);
			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.Write((byte)EffectType);
			writer.Write(TargetId);
			PosA.Write(writer);
			PosB.Write(writer);
			Color.Write(writer);
		}
	}

	internal class RequestTradePacket
	{
		public string Name;

		public static RequestTradePacket Read(NBufferReader reader)
		{
			var ret = new RequestTradePacket();
			ret.Name = reader.ReadUTF();
			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.WriteUTF(Name);
		}
	}

	internal class TeleportPacket
	{
		public uint ObjectId;

		public static TeleportPacket Read(NBufferReader reader)
		{
			var ret = new TeleportPacket();
			ret.ObjectId = reader.ReadUInt32();
			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.Write(ObjectId);
		}
	}
}