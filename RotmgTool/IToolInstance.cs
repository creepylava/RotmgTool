using System;
using System.Windows.Forms;
using RotmgTool.Commands;
using RotmgTool.Hooks;
using RotmgTool.Network;
using RotmgTool.Proxy;
using RotmgTool.UI;

namespace RotmgTool
{
	internal interface IToolInstance
	{
		Form MainForm { get; }
		SimpleSettings Settings { get; }
		SpamFilter Filter { get; }
		RemoteServer[] Servers { get; }

		PacketTable PacketTable { get; set; }
		void SaveXmlData(XmlData xmlData);
		XmlData LoadXmlData(string version);

		PolicyServer PolicyServer { get; }
		SocketProxy SocketProxy { get; }
		HttpProxy WebProxy { get; }

		CommandManager Commands { get; }
		HookManager Hooks { get; }
		WindowManager Windows { get; }

		void AppendLog(string text, params object[] args);
	}
}