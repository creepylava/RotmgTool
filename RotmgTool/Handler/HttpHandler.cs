using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using RotmgTool.Proxy;

namespace RotmgTool
{
	internal class HttpHandler
	{
		private readonly IToolInstance tool;

		public HttpHandler(IToolInstance tool)
		{
			this.tool = tool;
		}

		public void Attach()
		{
			tool.WebProxy.HandleRequest += HandleRequest;
		}

		private void HandleRequest(object sender, RequestEventArgs e)
		{
			if (e.URI.LocalPath == "/char/list")
			{
				var doc = XDocument.Parse(Encoding.UTF8.GetString(e.DataIn));
				foreach (var srv in doc.XPathSelectElements("//Server"))
				{
					string name = srv.Element("Name").Value;
					srv.Element("DNS").Value = tool.Servers.Single(server => server.Name == name).Loopback.ToString();
				}
				e.DataOut = Encoding.UTF8.GetBytes(doc.ToString());
				tool.AppendLog("Altered 'char/list' request.");
			}
			else if (e.URI.LocalPath == "/package/getPackages")
			{
				e.DataOut = Encoding.UTF8.GetBytes("<PackageResponse></PackageResponse>");
				tool.AppendLog("Altered 'package/getPackages' request.");
			}
		}
	}
}