using System;
using System.IO;
using System.Windows.Forms;

namespace RotmgTool
{
	internal static class Program
	{
		[STAThread]
		private static void Main()
		{
			AppDomain.CurrentDomain.UnhandledException += (sender, e) => OnUnhandledException(e.ExceptionObject as Exception);
			Application.ThreadException += (sender, e) => OnUnhandledException(e.Exception);

			//Application.EnableVisualStyles();
			//Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainWindow());
		}

		private static void OnUnhandledException(Exception ex)
		{
			if (ex == null)
				MessageBox.Show(ex.ToString());
			else
				MessageBox.Show(string.Format("{0}\r\n{1}", ex.Message, ex.StackTrace));
		}

		public static string RootDirectory
		{
			get { return Path.GetDirectoryName(Application.ExecutablePath); }
		}
	}
}