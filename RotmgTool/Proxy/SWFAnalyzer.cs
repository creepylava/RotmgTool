using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using RotmgTool.Network;
using RotmgTool.SWF;

namespace RotmgTool.Proxy
{
	internal class SWFAnalyzer
	{
		private static readonly string[] packetNames =
		{
			"FAILURE",
			"CREATE_SUCCESS",
			"CREATE",
			"PLAYERSHOOT",
			"MOVE",
			"PLAYERTEXT",
			"TEXT",
			"SHOOT",
			"DAMAGE",
			"UPDATE",
			"UPDATEACK",
			"NOTIFICATION",
			"NEW_TICK",
			"INVSWAP",
			"USEITEM",
			"SHOW_EFFECT",
			"HELLO",
			"GOTO",
			"INVDROP",
			"INVRESULT",
			"RECONNECT",
			"PING",
			"PONG",
			"MAPINFO",
			"LOAD",
			"PIC",
			"SETCONDITION",
			"TELEPORT",
			"USEPORTAL",
			"DEATH",
			"BUY",
			"BUYRESULT",
			"AOE",
			"GROUNDDAMAGE",
			"PLAYERHIT",
			"ENEMYHIT",
			"AOEACK",
			"SHOOTACK",
			"OTHERHIT",
			"SQUAREHIT",
			"GOTOACK",
			"EDITACCOUNTLIST",
			"ACCOUNTLIST",
			"QUESTOBJID",
			"CHOOSENAME",
			"NAMERESULT",
			"CREATEGUILD",
			"CREATEGUILDRESULT",
			"GUILDREMOVE",
			"GUILDINVITE",
			"ALLYSHOOT",
			"MULTISHOOT",
			"REQUESTTRADE",
			"TRADEREQUESTED",
			"TRADESTART",
			"CHANGETRADE",
			"TRADECHANGED",
			"ACCEPTTRADE",
			"CANCELTRADE",
			"TRADEDONE",
			"TRADEACCEPTED",
			"CLIENTSTAT",
			"CHECKCREDITS",
			"ESCAPE",
			"FILE",
			"INVITEDTOGUILD",
			"JOINGUILD",
			"CHANGEGUILDRANK",
			"PLAYSOUND",
			"GLOBAL_NOTIFICATION",
			"RESKIN",
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			"ENTER_ARENA"
		};

		public static string AnalyzePackets(IToolInstance tool, long ts, ABCFile abc)
		{
			var zip = abc.instances.Zip(abc.classes, (inst, cls) => new { Instance = inst, Class = cls });

			// Find version
			var parameter = zip
				.SingleOrDefault(type =>
				{
					var qName = abc.multinames[type.Instance.name];
					return abc.strings[abc.namespaces[qName.QName.ns].name] == "com.company.assembleegameclient.parameters";
				});
			if (parameter == null)
				return null;

			var stringTraits = parameter.Class.traits
				.Where(trait => trait.kind == TraitKind.Const && trait.Slot.vkind == ASType.Utf8)
				.ToList();
			var versionTrait = stringTraits.Single(trait => trait.Slot.slotId == 1);
			string version = abc.strings[versionTrait.Slot.vindex];
			var buildTrait = stringTraits.Single(trait => trait.Slot.slotId == 2);
			string build = abc.strings[buildTrait.Slot.vindex];

			// Find gsc_
			var gscTrait = abc.instances
				.SelectMany(inst => inst.traits)
				.Single(trait =>
				{
					var qName = abc.multinames[trait.name];
					return abc.strings[qName.QName.name] == "gsc_";
				});

			var handlerType = zip.Single(type => type.Instance.name == gscTrait.Slot.typeName);

			var table = new PacketTable(ts);
			var tableType = typeof(PacketTable);
			foreach (
				var packetEntry in
					handlerType.Class.traits.Where(trait => trait.kind == TraitKind.Const && trait.Slot.vkind == ASType.Integer))
			{
				int packetIndex = (int)packetEntry.Slot.slotId - 1;
				var packetId = (int)abc.ints[packetEntry.Slot.vindex];

				string name = null;
				if (packetIndex < packetNames.Length && packetIndex >= 0)
					name = packetNames[packetIndex];
				if (name == null) name = string.Format("_PACKET{0:X2}", packetIndex);

				tableType.GetField(name).SetValue(table, (byte)packetId);
			}
			tool.PacketTable = table;

			return version + "." + build;
		}

		public static void AnalyzeXML(IToolInstance tool, string version, SwfFile swf)
		{
			var doc = new XDocument();
			doc.Add(new XElement("XML"));
			foreach (var tag in swf.Tags)
			{
				// DefineBinaryData
				if (tag.Type != 87)
					continue;

				// <?xml
				if (tag.Content[6] != 0x3c || tag.Content[7] != 0x3f || tag.Content[8] != 0x78 ||
				    tag.Content[9] != 0x6d || tag.Content[10] != 0x6c)
					continue;

				string xmlString = Encoding.UTF8.GetString(tag.Content, 6, tag.Content.Length - 6);
				try
				{
					XDocument xml = XDocument.Parse(xmlString);
					doc.Root.Add(xml.Root.Elements());
				}
				catch
				{
				}
			}
			doc.Root.Add(new XAttribute("version", version));
			tool.SaveXmlData(new XmlData(doc));
		}
	}
}