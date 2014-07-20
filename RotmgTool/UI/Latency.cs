using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using RotmgTool.Network;

namespace RotmgTool.UI
{
	internal class Latency : InfoWindow
	{
		private readonly Font font;

		public Latency(WindowManager manager)
			: base(manager, "Latency")
		{
			MinimumSize = MaximumSize = Size = new Size(200, 70);
			font = new Font(Font.FontFamily, 10, FontStyle.Bold);
		}

		public override string ID
		{
			get { return "latency"; }
		}

		private SocketProxyWorker active;
		private readonly Stopwatch watch = new Stopwatch();

		private readonly double[] latencies = new double[20];
		private int latPtr;
		private long prevTick;
		private int tickSpan;
		private bool enoughDat;
		private bool lag;

		private string latency = "---";
		private Brush latencyBrush = Brushes.Silver;
		private string instability = "---";
		private Brush instabilityBrush = Brushes.Silver;

		protected internal override void SetActiveWorker(SocketProxyWorker worker)
		{
			if (active != null)
				active.ServerPacketReceived -= OnServerPacketReceived;

			active = worker;
			if (active != null)
			{
				latencies.Initialize();
				latPtr = 0;
				prevTick = 0;
				tickSpan = 20;
				enoughDat = false;
				lag = false;
				watch.Restart();
				active.ServerPacketReceived += OnServerPacketReceived;
			}
			else
				Tick();
			Invalidate();
		}

		private void OnServerPacketReceived(object sender, PacketEventArgs e)
		{
			if (e.ID == active.PacketTable.NEW_TICK)
			{
				lock (latencies)
				{
					long current = watch.ElapsedMilliseconds;
					latencies[latPtr++] = current - prevTick;
					prevTick = current;
					if (latPtr >= 20)
					{
						enoughDat = true;
						latPtr = 0;
					}
				}
				lag = false;
				tickSpan = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(e.Content, 4));
			}
		}

		private int tickCount;

		protected internal override void Tick()
		{
			if (active == null)
			{
				latency = "---";
				latencyBrush = Brushes.Silver;
				instability = "---";
				instabilityBrush = Brushes.Silver;
				return;
			}

			tickCount++;
			if (tickCount % 10 == 0)
			{
				if (lag)
				{
					lock (latencies)
					{
						long current = watch.ElapsedMilliseconds;
						latencies[latPtr++] = (current - prevTick) * 2; // penalize when lagging
						prevTick = current;
						if (latPtr >= 20)
						{
							enoughDat = true;
							latPtr = 0;
						}
					}
				}
				else
					lag = true;
			}

			latency = enoughDat ? (Math.Abs((QM(latencies) / tickSpan - 1) * 2)).ToString("f3") : "---";

			if (!enoughDat)
				latencyBrush = Brushes.Silver;
			else
			{
				double lat = Math.Abs((QM(latencies) / tickSpan - 1) * 2);
				if (lat > 1.0)
					latencyBrush = Brushes.Red;
				else if (lat > 0.3)
					latencyBrush = Brushes.Yellow;
				else
					latencyBrush = Brushes.Lime;
			}

			instability = enoughDat ? (SD(latencies) / tickSpan).ToString("f3") : "---";

			if (!enoughDat)
				instabilityBrush = Brushes.Silver;
			else
			{
				double insta = SD(latencies) / tickSpan;
				if (insta > 1.4)
					instabilityBrush = Brushes.Red;
				else if (insta > 0.6)
					instabilityBrush = Brushes.Yellow;
				else
					instabilityBrush = Brushes.Lime;
			}

			Invalidate();
		}


		private static double SD(double[] values)
		{
			if (values.Length == 0)
				return 0;

			var avg = values.Average();
			var variance = values.Sum(val => (val - avg) * (val - avg)) / values.Length;
			return Math.Sqrt(variance);
		}

		// http://www.remondo.net/calculate-quartiles-interquartilerange-csharp/
		private static double QM(IEnumerable<double> list)
		{
			IOrderedEnumerable<double> s = list.OrderBy(x => x);
			return (Q3(s) + Q1(s)) / 2;
		}

		private static double Q1(IOrderedEnumerable<double> list)
		{
			return Quartile(list, 0.25);
		}

		private static double Q3(IOrderedEnumerable<double> list)
		{
			return Quartile(list, 0.75);
		}

		private static double Quartile(IOrderedEnumerable<double> list, double quartile)
		{
			double result;
			double index = quartile * (list.Count() + 1);
			double remainder = index % 1;
			index = Math.Floor(index) - 1;

			if (remainder.Equals(0))
			{
				result = list.ElementAt((int)index);
			}
			else
			{
				double value = list.ElementAt((int)index);
				double interpolationValue = Interpolate(value, list.ElementAt((int)(index + 1)), remainder);

				result = value + interpolationValue;
			}
			return result;
		}

		private static double Interpolate(double a, double b, double remainder)
		{
			return (b - a) * remainder;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			string latString = string.Format("Tick Latency: {0}", latency);
			string instabString = string.Format("Tick Instability: {0}", instability);

			if (latencyBrush != Brushes.Red || tickCount % 3 < 2)
				e.Graphics.DrawString(latString, font, latencyBrush, new Point(20, 15));

			if (instabilityBrush != Brushes.Red || tickCount % 3 < 2)
				e.Graphics.DrawString(instabString, font, instabilityBrush, new Point(20, 40));
		}
	}
}