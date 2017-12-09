using System;
using System.Collections.Generic;
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
			Console.WriteLine("欢迎使用 Botzone 本地调试工具。");
			Console.WriteLine("如果不需要图形界面，请添加命令行参数启动。");
			Console.WriteLine("-h 参数可以查看命令行参数使用方法。");
			var args = Environment.GetCommandLineArgs();
			if (args.Length > 1)
				new Program().ConsoleMain(args);
			else
			{
				Console.ForegroundColor = ConsoleColor.Gray;
				Console.WriteLine("正在加载图形界面……");
				FreeConsole();
				App.Main();
			}
		}

		/// <summary>
		/// 控制台模式程序入口。
		/// </summary>
		private void ConsoleMain(string[] args)
		{
			Logger = new ConsoleLogger();
			Logger.Log(LogLevel.Info, "测试纯命令行");
			var str = Console.ReadLine();
		}
	}
}
