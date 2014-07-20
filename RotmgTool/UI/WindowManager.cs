using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RotmgTool.Network;

namespace RotmgTool.UI
{
	internal class WindowManager
	{
		private readonly Dictionary<string, InfoWindow> windows = new Dictionary<string, InfoWindow>();
		private readonly ToolStripDropDownButton dropDown;
		public IToolInstance Tool { get; private set; }

		private readonly Form parent;

		public WindowManager(IToolInstance tool, ToolStripDropDownButton dropDown)
		{
			parent = new Form
			{
				FormBorderStyle = FormBorderStyle.FixedToolWindow,
				Opacity = 0,
				ShowInTaskbar = false
			};

			Tool = tool;
			this.dropDown = dropDown;
			parent.Show();
			AddWindow(new Latency(this));
			AddWindow(new Connection(this));
			parent.Hide();

			Load();
		}

		private void AddWindow(InfoWindow window)
		{
			windows.Add(window.ID, window);
			parent.AddOwnedForm(window);
			dropDown.DropDownItems.Add(new ToolStripMenuItem(window.Text, null, (sender, e) => window.Show()));
		}

		public void Show(string id)
		{
			windows[id].Show();
		}

		private static readonly RectangleConverter converter = new RectangleConverter();

		internal Rectangle? GetSaveBounds(string id)
		{
			string settingID = string.Format("window.{0}.bounds", id);
			var s = Tool.Settings.GetValue<string>(settingID, "");
			if (string.IsNullOrEmpty(s))
				return null;
			return (Rectangle)converter.ConvertFromString(s);
		}

		public void SetActiveWorker(SocketProxyWorker worker)
		{
			foreach (var window in windows.Values)
				window.SetActiveWorker(worker);
		}

		public void Tick()
		{
			foreach (var window in windows.Values)
				window.Tick();
			Save();
		}

		private void Load()
		{
			foreach (var window in windows.Values)
			{
				var settingID = string.Format("window.{0}.visible", window.ID);
				if (Tool.Settings.GetValue<bool>(settingID, "false"))
					window.Show();
			}
		}

		private void Save()
		{
			foreach (var window in windows.Values)
			{
				string settingID = string.Format("window.{0}.visible", window.ID);
				Tool.Settings.SetValue(settingID, window.Visible.ToString());

				if (window.Visible)
				{
					settingID = string.Format("window.{0}.bounds", window.ID);
					Tool.Settings.SetValue(settingID, converter.ConvertToString(window.DesktopBounds));
				}
			}
		}
	}
}