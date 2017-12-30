using CefSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static BotzoneLocalRunner.Util;

namespace BotzoneLocalRunner
{
	public class Program
	{
		/// <summary>
		/// 释放当前的控制台窗口。
		/// </summary>
		/// <returns></returns>
		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		static extern bool FreeConsole();

		/// <summary>
		/// 根据参数判断是否初始化图形界面的程序入口。
		/// </summary>
		[STAThread]
		public static void Main()
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(StringResources.CONSOLE_WELCOME);
			Console.WriteLine(StringResources.CONSOLE_WELCOME2);
			Console.WriteLine(StringResources.CONSOLE_WELCOME3);
			Cef.Initialize(new CefSettings
			{
				Locale = CultureInfo.CurrentCulture.Name
			});

			var args = Environment.GetCommandLineArgs();
			if (args.Length > 1)
				new Program().ConsoleMain(args);
			else
			{
				Console.ForegroundColor = ConsoleColor.Gray;
				Console.WriteLine(StringResources.CONSOLE_LOAD_GUI);
				FreeConsole();
				App.Main();
			}
		}

		/// <summary>
		/// 控制台模式程序入口。
		/// </summary>
		private void ConsoleMain(string[] args)
		{
			BotzoneProtocol.CurrentBrowser = new CefSharp.OffScreen.ChromiumWebBrowser();
			BrowserJSObject.Init();

			Logger = new ConsoleLogger();
			Logger.Log(LogLevel.Info, "测试纯命令行");
			var str = Console.ReadLine();
		}
	}
}
