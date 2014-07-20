using System;
using System.Collections.Generic;
using RotmgTool.Network;

namespace RotmgTool.World
{
	internal class Entity
	{
		public Entity(uint id, ushort objType)
		{
			Stats = new Dictionary<StatsType, object>();
			ID = id;
			ObjectType = objType;
		}

		public uint ID { get; private set; }
		public ushort ObjectType { get; private set; }
		public Position Position { get; set; }
		public Dictionary<StatsType, object> Stats { get; private set; }

		public T GetStats<T>(StatsType type)
		{
			object ret;
			if (Stats.TryGetValue(type, out ret))
				return (T)ret;
			return default(T);
		}

		internal void OnUpdated()
		{
			EntityUpdated(this, EventArgs.Empty);
		}

		public event EventHandler EntityUpdated = delegate { };
	}
}