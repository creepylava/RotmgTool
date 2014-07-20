using System;
using RotmgTool.Network;

namespace RotmgTool.Commands
{
	internal abstract class CommandBase
	{
		public CommandBase(string name, string usage)
		{
			CommandName = name;
			Usage = usage;
		}

		public string CommandName { get; private set; }
		public string Usage { get; private set; }

		protected abstract bool Process(SocketProxyWorker client, string args);

		public bool Execute(SocketProxyWorker client, string args)
		{
			try
			{
				return Process(client, args);
			}
			catch
			{
				client.SendText("*Error*", "Error when executing the command.");
				client.SendText("", "Usage: " + Usage);
				return false;
			}
		}
	}
}