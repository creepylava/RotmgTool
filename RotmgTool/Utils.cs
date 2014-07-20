using System;
using RotmgTool.Network;
using RotmgTool.World;

namespace RotmgTool
{
	internal static class StringUtils
	{
		public static bool ContainsIgnoreCase(this string self, string val)
		{
			return self.IndexOf(val, StringComparison.InvariantCultureIgnoreCase) != -1;
		}

		public static bool EqualsIgnoreCase(this string self, string val)
		{
			return self.Equals(val, StringComparison.InvariantCultureIgnoreCase);
		}
	}

	internal static class InvUtils
	{
		public static short GetInv(this Entity entity, int slot)
		{
			return (short)(int)entity.Stats[(StatsType)((int)StatsType.Inventory0 + slot)];
		}
	}
}