using System;
using RotmgTool.Network;

namespace RotmgTool.Hooks
{
	internal abstract class HookBase
	{
		public HookBase(string id, string name)
		{
			ID = id;
			Name = name;
		}

		public string ID { get; private set; }
		public string Name { get; private set; }

		protected bool IsEnabled(SocketProxyWorker worker, bool defVal = true)
		{
			string name = string.Format("hook.{0}.enabled", ID);
			return worker.Proxy.Tool.Settings.GetValue<bool>(name, defVal.ToString());
		}

		public abstract void Attach(SocketProxyWorker worker);
	}
}