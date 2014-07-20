using System;
using System.Runtime.InteropServices;

namespace RotmgTool.Network
{
	internal enum StatsType : byte
	{
		MaximumHP = 0,
		HP = 1,
		Size = 2,
		MaximumMP = 3,
		MP = 4,
		ExperienceGoal = 5,
		Experience = 6,
		Level = 7,
		Inventory0 = 8,
		Inventory1 = 9,
		Inventory2 = 10,
		Inventory3 = 11,
		Inventory4 = 12,
		Inventory5 = 13,
		Inventory6 = 14,
		Inventory7 = 15,
		Inventory8 = 16,
		Inventory9 = 17,
		Inventory10 = 18,
		Inventory11 = 19,
		Attack = 20,
		Defense = 21,
		Speed = 22,
		Vitality = 26,
		Wisdom = 27,
		Dexterity = 28,
		Effects = 29,
		Stars = 30,
		Name = 31,
		Texture1 = 32,
		Texture2 = 33,
		MerchantMerchandiseType = 34,
		Credits = 35,
		SellablePrice = 36,
		PortalUsable = 37,
		AccountId = 38,
		CurrentFame = 39,
		SellablePriceCurrency = 40,
		ObjectConnection = 41,
		/*
         * Mask :F0F0F0F0
         * each byte -> type
         * 0:Dot
         * 1:ShortLine
         * 2:L
         * 3:Line
         * 4:T
         * 5:Cross
         * 0x21222112
        */
		MerchantRemainingCount = 42,
		MerchantRemainingMinute = 43,
		MerchantDiscount = 44,
		SellableRankRequirement = 45,
		HPBoost = 46,
		MPBoost = 47,
		AttackBonus = 48,
		DefenseBonus = 49,
		SpeedBonus = 50,
		VitalityBonus = 51,
		WisdomBonus = 52,
		DexterityBonus = 53,
		OwnerAccountId = 54,
		NameChangerStar = 55,
		NameChosen = 56,
		Fame = 57,
		FameGoal = 58,
		Glowing = 59,
		SinkOffset = 60,
		AltTextureIndex = 61,
		Guild = 62,
		GuildRank = 63,
		OxygenBar = 64,

		LootStatusVisible = 65,
		LootTime = 66,
		LootDropPotionTime = 67,
		LootTierPotionTime = 68,

		HPPotionCount = 69,
		MPPotionCount = 70,

		Backpack0 = 71,
		Backpack1 = 72,
		Backpack2 = 73,
		Backpack3 = 74,
		Backpack4 = 75,
		Backpack5 = 76,
		Backpack6 = 77,
		Backpack7 = 78,

		HasBackpack = 79,

		Skin = 80,

		PetId = 81,
		PetName = 82,
		PetType = 83,
		PetRarity = 84,
		PetGoal = 85,
		_0LJ = 86,
		PetAbility1Points = 87,
		PetAbility2Points = 88,
		PetAbility3Points = 89,
		PetAbility1Level = 90,
		PetAbility2Level = 91,
		PetAbility3Level = 92,
		PetAbility1Type = 93,
		PetAbility2Type = 94,
		PetAbility3Type = 95,
	}

	internal struct TimedPosition
	{
		public uint Time;
		public Position Position;

		public static TimedPosition Read(NBufferReader reader)
		{
			TimedPosition ret;
			ret.Time = reader.ReadUInt32();
			ret.Position = Position.Read(reader);
			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.Write(Time);
			Position.Write(writer);
		}
	}

	internal struct Position
	{
		public float X;
		public float Y;

		public static Position Read(NBufferReader reader)
		{
			Position ret;
			ret.X = reader.ReadSingle();
			ret.Y = reader.ReadSingle();
			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.Write(X);
			writer.Write(Y);
		}
	}

	internal struct ObjectDef
	{
		public ushort ObjectType;
		public ObjectStats Stats;

		public static ObjectDef Read(NBufferReader reader)
		{
			ObjectDef ret;
			ret.ObjectType = reader.ReadUInt16();
			ret.Stats = ObjectStats.Read(reader);
			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.Write(ObjectType);
			Stats.Write(writer);
		}
	}

	internal struct ObjectStats
	{
		public uint Id;
		public Position Position;
		public Tuple<StatsType, object>[] Stats;

		public static ObjectStats Read(NBufferReader reader)
		{
			ObjectStats ret;
			ret.Id = reader.ReadUInt32();
			ret.Position = Position.Read(reader);
			ret.Stats = new Tuple<StatsType, object>[reader.ReadUInt16()];
			for (var i = 0; i < ret.Stats.Length; i++)
			{
				var type = (StatsType)reader.ReadByte();
				if (type == StatsType.Guild || type == StatsType.Name || type == StatsType.PetName)
					ret.Stats[i] = Tuple.Create(type, (object)reader.ReadUTF());
				else if (type == StatsType.AccountId || type == StatsType.OwnerAccountId)
				{
					string x = reader.ReadUTF();
					// wut kabam?
					// AccountId & OwnerAccountId are App Engine numeric IDs
					// Some time ago the format was changed and broke newly created account
					// And...they didn't read the documentation and just use a string...
					// http://googlecloudplatform.blogspot.hk/2013/05/update-on-datastore-auto-ids.html
					// Quote: "guaranteed to be small enough to be completely represented as 64-bit floats"
					ret.Stats[i] = Tuple.Create(type, (object)long.Parse(x));
				}
				else
					ret.Stats[i] = Tuple.Create(type, (object)(int)reader.ReadUInt32());
			}

			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.Write(Id);
			Position.Write(writer);

			writer.Write((short)Stats.Length);
			foreach (var i in Stats)
			{
				writer.Write((byte)i.Item1);
				if (i.Item2 is string) writer.WriteUTF((string)i.Item2);
				else if (i.Item2 is long) writer.WriteUTF(((long)i.Item2).ToString());
				else writer.Write((int)i.Item2);
			}
		}
	}

	internal struct ObjectSlot
	{
		public uint ObjectId;
		public byte SlotId;
		public ushort ObjectType;

		public static ObjectSlot Read(NBufferReader reader)
		{
			ObjectSlot ret;
			ret.ObjectId = reader.ReadUInt32();
			ret.SlotId = reader.ReadByte();
			ret.ObjectType = reader.ReadUInt16();
			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.Write(ObjectId);
			writer.Write(SlotId);
			writer.Write(ObjectType);
		}
	}

	internal struct TileData
	{
		public ushort X;
		public ushort Y;
		public ushort Tile;
	}

	internal struct TradeItem
	{
		public uint Item;
		public uint SlotType;
		public bool Tradeable;
		public bool Included;

		public static TradeItem Read(NBufferReader reader)
		{
			TradeItem ret;
			ret.Item = reader.ReadUInt32();
			ret.SlotType = reader.ReadUInt32();
			ret.Tradeable = reader.ReadBoolean();
			ret.Included = reader.ReadBoolean();
			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.Write(Item);
			writer.Write(SlotType);
			writer.Write(Tradeable);
			writer.Write(Included);
		}
	}

	internal enum EffectType
	{
		Potion = 1,
		Teleport = 2,
		Stream = 3,
		Throw = 4,
		AreaBlast = 5, //radius=pos1.x
		Dead = 6,
		Trail = 7,
		Diffuse = 8, //radius=dist(pos1,pos2)
		Flow = 9,
		Trap = 10, //radius=pos1.x
		Lightning = 11, //particleSize=pos2.x
		Concentrate = 12, //radius=dist(pos1,pos2)
		BlastWave = 13, //origin=pos1, radius = pos2.x
		Earthquake = 14,
		Flashing = 15, //period=pos1.x, numCycles=pos1.y
		BeachBall = 16
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct ARGB
	{
		public ARGB(uint argb)
		{
			A = R = G = B = 0;
			Value = argb;
		}

		[FieldOffset(0)] public uint Value;

		[FieldOffset(3)] public byte A;
		[FieldOffset(2)] public byte R;
		[FieldOffset(1)] public byte G;
		[FieldOffset(0)] public byte B;

		public static ARGB Read(NBufferReader reader)
		{
			var ret = new ARGB();
			ret.Value = reader.ReadUInt32();
			return ret;
		}

		public void Write(NWriter writer)
		{
			writer.Write(Value);
		}
	}
}