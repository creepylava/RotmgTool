using System;
using System.Collections.Generic;
using RotmgTool.Network;

namespace RotmgTool.Commands
{
	internal class CommandManager
	{
		public IDictionary<string, CommandBase> Commands { get; private set; }

		public CommandManager()
		{
			Commands = new Dictionary<string, CommandBase>(StringComparer.InvariantCultureIgnoreCase);
			var t = typeof(CommandBase);
			foreach (var i in t.Assembly.GetTypes())
				if (t.IsAssignableFrom(i) && i != t && !i.IsAbstract)
				{
					var instance = (CommandBase)Activator.CreateInstance(i);
					Commands.Add(instance.CommandName, instance);
				}
		}

		public bool? Execute(SocketProxyWorker client, string text)
		{
			var index = text.IndexOf(' ');
			string cmd = text.Substring(1, index == -1 ? text.Length - 1 : index - 1);
			var args = index == -1 ? "" : text.Substring(index + 1);

			CommandBase command;
			if (!Commands.TryGetValue(cmd, out command))
				return null;
			return command.Execute(client, args);
		}
	}
}