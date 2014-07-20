using System;
using System.Net;

namespace RotmgTool.Network
{
	internal class RemoteServer
	{
		public string Name;
		public string DNS;

		public IPAddress Loopback;

		public override string ToString()
		{
			return string.Format("{0} ({1}) => {2}", Name, DNS, Loopback);
		}
	}
}