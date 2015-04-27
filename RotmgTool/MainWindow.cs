using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;
using RotmgTool.Commands;
using RotmgTool.Hooks;
using RotmgTool.Network;
using RotmgTool.Proxy;
using RotmgTool.UI;
using Timer = System.Windows.Forms.Timer;

namespace RotmgTool
{
	internal class MainWindow : Form, IToolInstance
	{
		public MainWindow()
		{
			PopulateWindow();
			timer = new Timer();
			timer.Interval = 100;
			timer.Tick += OnTick;
		}

		private RichTextBox logBox;
		private ToolStripStatusLabel proxyLink;
		private ToolStripDropDownButton windowDropDown;

		private void PopulateWindow()
		{
			FormBorderStyle = FormBorderStyle.FixedSingle;
			MaximizeBox = false;
			Text = "RotMG Tool";
			Size = new Size(600, 400);
			Font = new Font("Segoe UI", 9);
			Shown += OnShown;

			// log box

			logBox = new RichTextBox();
			logBox.BackColor = Color.White;
			logBox.ForeColor = Color.Black;
			logBox.BorderStyle = BorderStyle.None;
			logBox.Cursor = Cursors.Default;
			logBox.ReadOnly = true;
			logBox.Dock = DockStyle.Fill;
			Controls.Add(logBox);

			// status bar
			var status = new StatusStrip();
			status.RenderMode = ToolStripRenderMode.ManagerRenderMode;
			status.Dock = DockStyle.Bottom;
			status.SizingGrip = false;
			Controls.Add(status);

			proxyLink = new ToolStripStatusLabel();
			proxyLink.IsLink = true;
			var ctxMenu = new ContextMenuStrip
			{
				Items =
				{
					new ToolStripMenuItem("Copy AGC Loader link...", null, (sender, e) =>
					{
						string link = new Uri(new Uri(proxyLink.Text), "/RotMG").AbsoluteUri;
						Clipboard.SetText(link);
						AppendLog("Link copyed to clipboard.");
					})
				}
			};
			proxyLink.MouseUp += (sender, e) =>
			{
				if (e.Button == MouseButtons.Right)
					ctxMenu.Show(MousePosition);
				else if (e.Button == MouseButtons.Left)
					Process.Start(proxyLink.Text);
			};
			status.Items.Add(proxyLink);

			// tool bar

			var toolBar = new ToolStrip();
			toolBar.Items.Add(new ToolStripButton("Clear Log", null, (sender, e) => { logBox.Clear(); }));
			toolBar.Items.Add(new ToolStripButton("Reload word list", null, (sender, e) =>
			{
				Filter.LoadWordList();
				AppendLog("Word list reloaded.");
			}));
			toolBar.Items.Add(new ToolStripButton("Reload settings", null, (sender, e) =>
			{
				Settings = new SimpleSettings();
				AppendLog("Settings reloaded.");
			}));
			windowDropDown = new ToolStripDropDownButton("Info Windows", null);
			toolBar.Items.Add(windowDropDown);

			toolBar.Dock = DockStyle.Top;
			Controls.Add(toolBar);
			toolBar.PerformLayout();
		}

		public void AppendLog(string text, params object[] args)
		{
			if (InvokeRequired)
				Invoke(new Action<string, object[]>(AppendLog), text, args);
			else if (Visible)
				logBox.AppendText(string.Format("[{0}] {1}{2}",
					DateTime.Now.ToString("hh:mm:ss"),
					string.Format(text, args),
					Environment.NewLine));
		}

		private bool InitializeComponent(string name, Action func)
		{
			if (Debugger.IsAttached)
			{
				func();
				AppendLog("{0} initialized.", name);
				return true;
			}
			try
			{
				func();
				AppendLog("{0} initialized.", name);
				return true;
			}
			catch (Exception ex)
			{
				AppendLog("Failed to initialize {0}: {1}", name, ex.ToString());
				return false;
			}
		}

		private void Initialize()
		{
			AppendLog("RotMG Tool initializing...");

			try
			{
				Settings = new SimpleSettings();
				string packetFile = Path.Combine(Program.RootDirectory, "packets.dat");
				if (File.Exists(packetFile))
					PacketTable = PacketTable.Load(File.ReadAllText(packetFile));
				AppendLog("Settings loaded.");
			}
			catch (Exception ex)
			{
				AppendLog("Error when loading settings: " + ex.Message);
				return;
			}

			//new System.Threading.Thread(DoCheckUpdate) { IsBackground = true }.Start();

			AppendLog("Retrieving server list...");
			var doc =
				XDocument.Load("http://realmofthemadgodhrd.appspot.com/char/list?guid=" +
				               Guid.NewGuid().ToString().Replace("-", "").ToUpper());
			byte id = 1;
			Servers = doc.XPathSelectElements("//Server").Select(srv => new RemoteServer
			{
				Name = srv.Element("Name").Value,
				DNS = srv.Element("DNS").Value,
				Loopback = new IPAddress(new byte[] { 127, 0, 0, id++ })
			}).ToArray();
			AppendLog("Server list retrieved, total {0} servers.", Servers.Length);


			if (!InitializeComponent("Spam Filter", () =>
			{
				Filter = new SpamFilter(this);
				Filter.LoadWordList();
			})) return;

			if (!InitializeComponent("Handlers", () =>
			{
				Commands = new CommandManager();
				Hooks = new HookManager();
				Invoke(new Action(() => Windows = new WindowManager(this, windowDropDown)));
			})) return;

			AppendLog("If you see any firewall warning, allow this program to pass!");

			if (!InitializeComponent("Web Proxy", () =>
			{
				WebProxy = new HttpProxy(this);
				WebProxy.Start();
				BeginInvoke(new Action(() => { proxyLink.Text = new Uri(WebProxy.ProxyUrl).AbsoluteUri; }));
			})) return;

			if (!InitializeComponent("Socket Proxy", () =>
			{
				SocketProxy = new SocketProxy(this);
				SocketProxy.Start();
			})) return;

			if (!InitializeComponent("Policy Server", () =>
			{
				PolicyServer = new PolicyServer();
				if (!PolicyServer.Start())
					throw new Exception("Cannot start policy server! Try start as adminstrator/sudo!");
			})) return;

			new HttpHandler(this).Attach();
			new SocketHandler(this).Attach();
			BeginInvoke(new Action(() => timer.Start()));

			AppendLog("RotMG Tool initialized.");
		}

		private void OnShown(object sender, EventArgs e)
		{
			new Thread(() =>
			{
				Initialize();
				Thread.CurrentThread.Join();
			}) { IsBackground = true }.Start();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (MessageBox.Show("Exiting will cause all proxied RotMG connection to be terminated! Are you sure?",
				Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
				e.Cancel = true;
			base.OnClosing(e);
		}

		protected override void OnClosed(EventArgs e)
		{
			timer.Stop();

			if (SocketProxy != null)
				SocketProxy.Stop();

			if (WebProxy != null)
				WebProxy.Stop();

			if (PolicyServer != null)
				PolicyServer.Stop();

			base.OnClosed(e);
		}


		private readonly Timer timer;

		private void OnTick(object sender, EventArgs e)
		{
			Windows.Tick();
			Settings.Save();
		}


		public Form MainForm
		{
			get { return this; }
		}

		public SimpleSettings Settings { get; private set; }
		public SpamFilter Filter { get; private set; }
		public RemoteServer[] Servers { get; private set; }

		private PacketTable packetTable;

		public PacketTable PacketTable
		{
			get { return packetTable; }
			set
			{
				packetTable = value;
				File.WriteAllText(Path.Combine(Program.RootDirectory, "packets.dat"), packetTable.Save());
			}
		}

		private readonly Dictionary<string, XmlData> xmlDatas = new Dictionary<string, XmlData>();

		public void SaveXmlData(XmlData xmlData)
		{
			xmlDatas[xmlData.Version] = xmlData;
			var xmlDir = Directory.CreateDirectory(Path.Combine(Program.RootDirectory, "xml"));
			File.WriteAllText(Path.Combine(xmlDir.FullName, xmlData.Version + ".xml"), xmlData.Document.ToString());
		}

		public XmlData LoadXmlData(string version)
		{
			XmlData ret;
			if (xmlDatas.TryGetValue(version, out ret))
				return ret;

			var xmlDir = Directory.CreateDirectory(Path.Combine(Program.RootDirectory, "xml"));
			string xmlFile = Path.Combine(xmlDir.FullName, version + ".xml");
			if (!File.Exists(xmlFile))
			{
				AppendLog("Could not find XML data for version '{0}'!", version);
				return null;
			}
			ret = new XmlData(XDocument.Parse(File.ReadAllText(xmlFile)));
			xmlDatas[version] = ret;
			return ret;
		}

		private XmlData xmlData;

		public XmlData XmlData
		{
			get { return xmlData; }
			set
			{
				xmlData = value;
				var xml = Directory.CreateDirectory("xml");
				File.WriteAllText(Path.Combine(xml.FullName, xmlData.Version + ".xml"), value.Document.ToString());
			}
		}

		public PolicyServer PolicyServer { get; private set; }
		public SocketProxy SocketProxy { get; private set; }
		public HttpProxy WebProxy { get; private set; }

		public CommandManager Commands { get; private set; }
		public HookManager Hooks { get; private set; }
		public WindowManager Windows { get; private set; }
	}
}