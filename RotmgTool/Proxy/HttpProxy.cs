using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace RotmgTool.Proxy
{
	internal class RequestEventArgs : EventArgs
	{
		public Uri URI { get; set; }
		public byte[] DataIn { get; set; }
		public byte[] DataOut { get; set; }
	}

	internal class HttpProxy
	{
		private readonly IToolInstance tool;
		private readonly HttpListener listener;
		private readonly SwfPatcher patcher;

		public string ProxyUrl { get; private set; }

		public HttpProxy(IToolInstance tool)
		{
			var host = tool.Settings.GetValue<string>("proxy.host", "127.0.0.7");
			var port = tool.Settings.GetValue<int>("proxy.port", "2000");
			this.tool = tool;

			listener = new HttpListener();
			listener.Prefixes.Add(ProxyUrl = string.Format("http://{0}:{1}/", host, port));
			patcher = new SwfPatcher(tool, host, port);
		}

		public void Start()
		{
			listener.Start();
			listener.BeginGetContext(ContextReceived, null);
		}

		private volatile bool stopped;

		public void Stop()
		{
			stopped = true;
			listener.Stop();
		}

		private static readonly string[] httpPostfixes =
		{
			".png",
			".gif",
			".mp3",
			".xml",
			".css",
			".js"
		};

		private static readonly Regex loaderPattern = new Regex("AGCLoader(\\d+)\\.swf");
		private static readonly Regex agcPattern = new Regex("AssembleeGameClient(\\d+)\\.swf");

		private void ContextReceived(IAsyncResult ar)
		{
			if (stopped)
				return;

			HttpListenerContext ctx;
			try
			{
				ctx = listener.EndGetContext(ar);
				listener.BeginGetContext(ContextReceived, null);
			}
			catch
			{
				return;
			}

			try
			{
				if (ctx.Request.Url.LocalPath.EqualsIgnoreCase("/rotmg"))
				{
					// redirect
					var client = new WebClient();
					var ver = client.DownloadString("http://realmofthemadgod.appspot.com/version.txt");
					ctx.Response.Redirect("AGCLoader" + ver + ".swf");
				}
				else if (loaderPattern.IsMatch(ctx.Request.Url.LocalPath))
				{
					// patch loader
					tool.AppendLog("Retrieving AGCLoader...");
					var client = new WebClient();
					var swf = client.DownloadData("http://realmofthemadgod.appspot.com/" + ctx.Request.Url.LocalPath);
					long ts = long.Parse(loaderPattern.Match(ctx.Request.Url.LocalPath).Groups[1].Value);

					long t = Environment.TickCount;
					patcher.Patch(ts, ref swf, true);
					tool.AppendLog("Patched SWF: {0} ms", Environment.TickCount - t);

					ctx.Response.ContentType = "application/x-shockwave-flash";
					ctx.Response.OutputStream.Write(swf, 0, swf.Length);
				}
				else if (agcPattern.IsMatch(ctx.Request.Url.LocalPath))
				{
					string cacheDir = Directory.CreateDirectory(Path.Combine(Program.RootDirectory, "cache")).FullName;
					string cachePath = Path.Combine(cacheDir, Path.GetFileName(ctx.Request.Url.LocalPath));
					byte[] swf;
					if (File.Exists(cachePath))
						swf = File.ReadAllBytes(cachePath);
					else
					{
						// patch client
						tool.AppendLog("Retrieving Game Client...");
						var client = new WebClient();
						swf = client.DownloadData("http://realmofthemadgod.appspot.com/" + ctx.Request.Url.LocalPath);
						long ts = long.Parse(agcPattern.Match(ctx.Request.Url.LocalPath).Groups[1].Value);
						long t = Environment.TickCount;
						patcher.Patch(ts, ref swf, false);
						if (Type.GetType("Mono.Runtime") == null)
						{
							GC.Collect();
							GC.WaitForFullGCApproach();
							GC.WaitForFullGCComplete();
						}
						tool.AppendLog("Patched SWF: {0} ms", Environment.TickCount - t);
						File.WriteAllBytes(cachePath, swf);
					}

					ctx.Response.ContentType = "application/x-shockwave-flash";
					ctx.Response.OutputStream.Write(swf, 0, swf.Length);
				}
				else if (httpPostfixes.Any(postfix => ctx.Request.Url.LocalPath.EndsWith(postfix)))
				{
					// redirection
					var newUri = new UriBuilder(ctx.Request.Url);
					newUri.Host = "realmofthemadgod.appspot.com";
					newUri.Scheme = "http";
					newUri.Port = 80;
					ctx.Response.Redirect(newUri.Uri.ToString());
				}
				else
				{
					// https
					var newUri = new UriBuilder(ctx.Request.Url);
					newUri.Host = "realmofthemadgod.appspot.com";
					newUri.Scheme = "https";
					newUri.Port = 443;

					var req = (HttpWebRequest)WebRequest.Create(newUri.Uri);
					req.ContentType = ctx.Request.ContentType;
					req.Method = ctx.Request.HttpMethod;
					req.UserAgent = ctx.Request.UserAgent;

					var buffer = new byte[0x100];
					int count;
					if (req.Method == "POST")
					{
						var reqStream = req.GetRequestStream();
						while ((count = ctx.Request.InputStream.Read(buffer, 0, buffer.Length)) > 0)
							reqStream.Write(buffer, 0, count);
						reqStream.Close();
					}
					WebResponse resp;
					resp = req.GetResponse();
					ctx.Response.ContentType = resp.ContentType;

					var respStream = resp.GetResponseStream();
					var tmp = new MemoryStream();
					while ((count = respStream.Read(buffer, 0, buffer.Length)) > 0)
						tmp.Write(buffer, 0, count);

					var eventArgs = new RequestEventArgs { URI = ctx.Request.Url };
					eventArgs.DataIn = eventArgs.DataOut = tmp.ToArray();
					if (HandleRequest != null)
						HandleRequest(this, eventArgs);

					ctx.Response.OutputStream.Write(eventArgs.DataOut, 0, eventArgs.DataOut.Length);
				}

				ctx.Response.Close();
			}
			catch (Exception ex)
			{
				if (!(ex is HttpListenerException) && !(ex is WebException) && !(ex is SocketException))
					Application.OnThreadException(ex);
			}
		}

		public event EventHandler<RequestEventArgs> HandleRequest;
	}
}