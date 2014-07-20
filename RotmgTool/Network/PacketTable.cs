using System;
using System.IO;
using System.Reflection;

#pragma warning disable 0649

namespace RotmgTool.Network
{
	internal class PacketTable
	{
		public long TimeStamp { get; private set; }

		public PacketTable(long ts)
		{
			TimeStamp = ts;
		}

		public byte FAILURE;
		public byte CREATE_SUCCESS;
		public byte CREATE;
		public byte PLAYERSHOOT;
		public byte MOVE;
		public byte PLAYERTEXT;
		public byte TEXT;
		public byte SHOOT;
		public byte DAMAGE;
		public byte UPDATE;
		public byte UPDATEACK;
		public byte NOTIFICATION;
		public byte NEW_TICK;
		public byte INVSWAP;
		public byte USEITEM;
		public byte SHOW_EFFECT;
		public byte HELLO;
		public byte GOTO;
		public byte INVDROP;
		public byte INVRESULT;
		public byte RECONNECT;
		public byte PING;
		public byte PONG;
		public byte MAPINFO;
		public byte LOAD;
		public byte PIC;
		public byte SETCONDITION;
		public byte TELEPORT;
		public byte USEPORTAL;
		public byte DEATH;
		public byte BUY;
		public byte BUYRESULT;
		public byte AOE;
		public byte GROUNDDAMAGE;
		public byte PLAYERHIT;
		public byte ENEMYHIT;
		public byte AOEACK;
		public byte SHOOTACK;
		public byte OTHERHIT;
		public byte SQUAREHIT;
		public byte GOTOACK;
		public byte EDITACCOUNTLIST;
		public byte ACCOUNTLIST;
		public byte QUESTOBJID;
		public byte CHOOSENAME;
		public byte NAMERESULT;
		public byte CREATEGUILD;
		public byte CREATEGUILDRESULT;
		public byte GUILDREMOVE;
		public byte GUILDINVITE;
		public byte ALLYSHOOT;
		public byte MULTISHOOT;
		public byte REQUESTTRADE;
		public byte TRADEREQUESTED;
		public byte TRADESTART;
		public byte CHANGETRADE;
		public byte TRADECHANGED;
		public byte ACCEPTTRADE;
		public byte CANCELTRADE;
		public byte TRADEDONE;
		public byte TRADEACCEPTED;
		public byte CLIENTSTAT;
		public byte CHECKCREDITS;
		public byte ESCAPE;
		public byte FILE;
		public byte INVITEDTOGUILD;
		public byte JOINGUILD;
		public byte CHANGEGUILDRANK;
		public byte PLAYSOUND;
		public byte GLOBAL_NOTIFICATION;
		public byte RESKIN;
		public byte _PACKET47;
		public byte _PACKET48;
		public byte _PACKET49;
		public byte _PACKET4A;
		public byte _PACKET4B;
		public byte _PACKET4C;
		public byte _PACKET4D;
		public byte _PACKET4E;
		public byte ENTER_ARENA;
		public byte _PACKET50;
		public byte _PACKET51;
		public byte _PACKET52;
		public byte _PACKET53;
		public byte _PACKET54;
		public byte _PACKET55;
		public byte _PACKET56;
		public byte _PACKET57;
		public byte _PACKET58;
		public byte _PACKET59;
		public byte _PACKET5A;
		public byte _PACKET5B;
		public byte _PACKET5C;
		public byte _PACKET5D;
		public byte _PACKET5E;
		public byte _PACKET5F;
		public byte _PACKET60;
		public byte _PACKET61;
		public byte _PACKET62;
		public byte _PACKET63;
		public byte _PACKET64;
		public byte _PACKET65;
		public byte _PACKET66;
		public byte _PACKET67;
		public byte _PACKET68;
		public byte _PACKET69;
		public byte _PACKET6A;
		public byte _PACKET6B;
		public byte _PACKET6C;
		public byte _PACKET6D;
		public byte _PACKET6E;
		public byte _PACKET6F;
		public byte _PACKET70;

		public static PacketTable Load(string data)
		{
			var type = typeof(PacketTable);
			using (var reader = new StringReader(data))
			{
				var ret = new PacketTable(long.Parse(reader.ReadLine()));
				string entry = reader.ReadLine();
				do
				{
					string packetName = entry.Split(':')[0];
					byte packetId = byte.Parse(entry.Split(':')[1]);

					type.GetField(packetName).SetValue(ret, packetId);
					entry = reader.ReadLine();
				} while (entry != null);
				return ret;
			}
		}

		public string Save()
		{
			var type = typeof(PacketTable);
			using (var writer = new StringWriter())
			{
				writer.WriteLine(TimeStamp);
				foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
				{
					if (field.Name[0] != '_') // numerical ID
						writer.WriteLine("{0}:{1}", field.Name, field.GetValue(this));
				}
				return writer.ToString();
			}
		}
	}
}