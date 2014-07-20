using System;
using System.Collections.Generic;
using RotmgTool.Network;

namespace RotmgTool.Hooks
{
	internal class HookManager
	{
		public IList<HookBase> Hooks { get; private set; }

		public HookManager()
		{
			Hooks = new List<HookBase>();
			var t = typeof(HookBase);
			foreach (var i in t.Assembly.GetTypes())
				if (t.IsAssignableFrom(i) && i != t)
				{
					var instance = (HookBase)Activator.CreateInstance(i);
					Hooks.Add(instance);
				}
		}

		public void AttachHooks(SocketProxyWorker client)
		{
			foreach (var hook in Hooks)
				hook.Attach(client);
		}
	}
}