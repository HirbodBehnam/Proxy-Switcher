using System;
using System.Windows.Forms;

namespace ProxySwitcher
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			var main = new Main();
			if (args.Length > 0 && args[0] == "--taskbar")
				main.NoGuiStart = true;
			Application.Run(main);
		}
	}
}
