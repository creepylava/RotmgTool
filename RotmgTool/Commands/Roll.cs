using System;
using System.Xml.XPath;
using RotmgTool.Network;
using RotmgTool.World;

namespace RotmgTool.Commands
{
	internal class Roll : CommandBase
	{
		public Roll()
			: base("roll", "/roll")
		{
		}

		public struct StatData
		{
			public StatData(string xmlName, string statName, StatsType statType, StatsType boostType)
			{
				XMLName = xmlName;
				StatName = statName;
				StatType = statType;
				StatBoostType = boostType;
			}

			public string XMLName;
			public string StatName;
			public StatsType StatType;
			public StatsType StatBoostType;
		}

		public static readonly StatData[] stats =
		{
			new StatData("MaxHitPoints", "HP", StatsType.MaximumHP, StatsType.HPBoost),
			new StatData("MaxMagicPoints", "MP", StatsType.MaximumMP, StatsType.MPBoost),
			new StatData("Attack", "ATT", StatsType.Attack, StatsType.AttackBonus),
			new StatData("Defense", "DEF", StatsType.Defense, StatsType.DefenseBonus),
			new StatData("Speed", "SPD", StatsType.Speed, StatsType.SpeedBonus),
			new StatData("Dexterity", "DEX", StatsType.Dexterity, StatsType.DexterityBonus),
			new StatData("HpRegen", "VIT", StatsType.Vitality, StatsType.VitalityBonus),
			new StatData("MpRegen", "WIS", StatsType.Wisdom, StatsType.WisdomBonus)
		};

		protected override bool Process(SocketProxyWorker client, string args)
		{
			Entity player = client.World[client.World.SelfID];
			var playerData = client.Proxy.Tool.LoadXmlData(client.Version)[player.ObjectType];
			foreach (var stat in stats)
			{
				var statLimitNode = playerData.Element(stat.XMLName);
				var statIncrNode = playerData.XPathSelectElement("LevelIncrease[text() = '" + stat.XMLName + "']");

				int beginStat = int.Parse(statLimitNode.Value);
				int maxStat = int.Parse(statLimitNode.Attribute("max").Value);

				int minIncr = int.Parse(statIncrNode.Attribute("min").Value);
				int maxIncr = int.Parse(statIncrNode.Attribute("max").Value);
				float avgIncr = (minIncr + maxIncr) / 2f;

				float avgStat = beginStat + (player.GetStats<int>(StatsType.Level) - 1) * avgIncr;
				int playerStat = player.GetStats<int>(stat.StatType) - player.GetStats<int>(stat.StatBoostType);
				var avgDiff = (int)(playerStat - avgStat);

				client.SendText("",
					string.Format("{0}: {1} from average ({2} until max)", stat.StatName, avgDiff, maxStat - playerStat));
			}
			return true;
		}
	}
}