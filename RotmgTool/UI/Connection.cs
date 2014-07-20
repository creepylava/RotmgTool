using System;
using System.Drawing;
using System.Windows.Forms;
using RotmgTool.Network;

namespace RotmgTool.UI
{
	internal class Connection : InfoWindow
	{
		private readonly Font font;
		private readonly Button forceDC;

		public Connection(WindowManager manager)
			: base(manager, "Connection")
		{
			MinimumSize = MaximumSize = Size = new Size(200, 100);
			font = new Font(Font.FontFamily, 10, FontStyle.Bold);

			forceDC = new Button();
			forceDC.Location = new Point(20, 15);
			forceDC.Size = new Size(70, 25);
			forceDC.BackColor = Color.Transparent;
			forceDC.FlatStyle = FlatStyle.Flat;
			forceDC.ForeColor = Color.Silver;
			forceDC.FlatAppearance.MouseOverBackColor = Color.FromArgb(unchecked((int)0xff404040));
			forceDC.FlatAppearance.MouseDownBackColor = Color.FromArgb(unchecked((int)0xff404040));
			forceDC.Enabled = false;
			forceDC.Text = "Force DC";
			forceDC.Click += (sender, e) =>
			{
				if (active != null)
					active.Kill(this, EventArgs.Empty);
			};
			Controls.Add(forceDC);
		}

		public override string ID
		{
			get { return "connection"; }
		}

		private SocketProxyWorker active;

		private string server;
		private string area;

		protected internal override void SetActiveWorker(SocketProxyWorker worker)
		{
			forceDC.Enabled = (worker != null);
			if (worker != null)
			{
				server = worker.ServerName;
				area = worker.ConnectionName;
			}
			else
			{
				server = null;
				area = null;
			}
			active = worker;
			Invalidate();
		}

		protected internal override void Tick()
		{
			if (active == null)
				return;
			if (server != active.ServerName || area != active.ConnectionName)
			{
				server = active.ServerName;
				area = active.ConnectionName;
				Invalidate();
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			string serverName = server ?? "---";
			string connName = area ?? "---";
			serverName = string.Format("Server: {0}", serverName);
			connName = string.Format("Area: {0}", connName.StartsWith("NexusPortal.") ? connName.Substring(12) : connName);
			e.Graphics.DrawString(serverName, font, Brushes.Silver, new Point(20, 45));
			e.Graphics.DrawString(connName, font, Brushes.Silver, new Point(20, 70));
		}
	}
}