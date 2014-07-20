using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RotmgTool.Network;

namespace RotmgTool.UI
{
	internal abstract class InfoWindow : Form
	{
		public WindowManager Manager { get; private set; }

		private class CloseButton : Label
		{
			public CloseButton()
			{
				BackColor = Color.Transparent;
				ForeColor = Color.Black;
				AutoSize = false;
				Size = new Size(15, 15);

				SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
			}

			private const int padding = 4;

			protected override void OnPaint(PaintEventArgs pevent)
			{
				pevent.Graphics.SmoothingMode = SmoothingMode.HighQuality;
				pevent.Graphics.DrawLine(pen,
					Point.Add(DisplayRectangle.Location, new Size(padding, padding)),
					Point.Add(DisplayRectangle.Location, new Size(DisplayRectangle.Width - padding, DisplayRectangle.Height - padding)));
				pevent.Graphics.DrawLine(pen,
					Point.Add(DisplayRectangle.Location, new Size(DisplayRectangle.Width - padding, padding)),
					Point.Add(DisplayRectangle.Location, new Size(padding, DisplayRectangle.Height - padding)));
			}
		}

		private readonly CloseButton closeBtn;

		public InfoWindow(WindowManager manager, string title)
		{
			closeBtn = new CloseButton();
			closeBtn.MouseClick += (sender, e) =>
			{
				if (e.Button == MouseButtons.Left)
					Close();
			};
			Controls.Add(closeBtn);

			FormBorderStyle = FormBorderStyle.None;
			AllowTransparency = true;
			Opacity = 0.85;
			TopMost = true;
			Text = title;
			Font = manager.Tool.MainForm.Font;
			StartPosition = FormStartPosition.WindowsDefaultLocation;
			ShowInTaskbar = false;
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

			Manager = manager;
		}

		protected override void OnLoad(EventArgs e)
		{
			var bounds = Manager.GetSaveBounds(ID);
			if (bounds != null)
				DesktopBounds = bounds.Value;
			base.OnLoad(e);
		}

		private static readonly Pen pen = new Pen(Brushes.White, 2);

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			e.Graphics.Clear(Color.FromArgb(unchecked((int)0xFF202020)));

			var rect = ClientRectangle;
			rect.Inflate(-1, -1);
			e.Graphics.DrawRectangle(pen, rect);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			Hide();
			e.Cancel = true;
		}

		private bool dragging;
		private int dx, dy;

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if ((MouseButtons & MouseButtons.Left) != 0)
			{
				dragging = true;
				dx = e.X;
				dy = e.Y;
				Capture = true;
			}

			base.OnMouseDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if ((MouseButtons & MouseButtons.Left) != 0)
			{
				if (dragging)
					Location = new Point(MousePosition.X - dx, MousePosition.Y - dy);
			}
			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			dragging = false;
			Capture = false;
			base.OnMouseUp(e);
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			closeBtn.Location = new Point(ClientRectangle.Width - closeBtn.Size.Width - 2, 2);
			closeBtn.BringToFront();
			base.OnLayout(levent);
		}

		public abstract string ID { get; }

		protected internal abstract void SetActiveWorker(SocketProxyWorker worker);

		protected internal virtual void Tick()
		{
		}
	}
}