using CefSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
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
			var args = Environment.GetCommandLineArgs();
			if (args.Length <= 1)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Error.WriteLine(StringResources.CONSOLE_WELCOME);
				Console.Error.WriteLine(StringResources.CONSOLE_WELCOME2);
				Console.Error.WriteLine(StringResources.CONSOLE_WELCOME3);
			}

			if (args.Length > 1)
				new Program().ConsoleMain(args);
			else
			{
				Cef.Initialize(new CefSettings
				{
					Locale = CultureInfo.CurrentCulture.Name
				});
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
			string outputMatchCollectionPath = null, outputLogPath = null, lastOption = "";
			Action<string> next = null;
			List<string> requiredArguments = new List<string>();
			MatchConfiguration conf;
			Match[] matches = null;

			Logger = new ConsoleLogger();

			try
			{
				// 解析参数
				foreach (var arg in args.Skip(1))
				{
					switch (arg)
					{
						case "-h":
							Console.WriteLine(StringResources.TITLE + " " + Assembly.GetEntryAssembly().GetName().Version);
							Console.WriteLine(String.Format(StringResources.CONSOLE_HELP, args[0]));
							return;
						case "-o":
							if (next != null)
								throw new FormatException(
									String.Format(StringResources.CONSOLE_MISSING_ARGUMENT, lastOption));
							next = file =>
							{
								if (File.Exists(file))
								{
									try
									{
										using (var s = File.Open(file, FileMode.Open))
										{
											var f = new BinaryFormatter();
											matches = f.Deserialize(s) as Match[];
										}
									}
									catch (Exception e)
									{
										throw new FormatException(
											StringResources.BAD_MATCH_COLLECTION_FORMAT + ": " + e.Message);
									}
								}
								outputMatchCollectionPath = file;
							};
							break;
						case "-l":
							if (next != null)
								throw new FormatException(
									String.Format(StringResources.CONSOLE_MISSING_ARGUMENT, lastOption));
							next = file => outputLogPath = file;
							break;
						case "-u":
							if (next != null)
								throw new FormatException(
									String.Format(StringResources.CONSOLE_MISSING_ARGUMENT, lastOption));
							next = url => BotzoneProtocol.Credentials.BotzoneCopiedURL = url;
							break;
						case "--simple-io":
							LocalProgramRunner.IsSimpleIO = true;
							break;
						default:
							if (next != null)
							{
								next(arg);
								next = null;
							}
							else
								requiredArguments.Add(arg);
							break;
					}
					lastOption = arg;
				}
				if (next != null)
					throw new FormatException(
						String.Format(StringResources.CONSOLE_MISSING_ARGUMENT, lastOption));
				if (requiredArguments.Count < 2)
					throw new FormatException(StringResources.CONSOLE_BAD_FORMAT);

				// 生成对局配置
				conf = new MatchConfiguration
				{
					Game = new Game
					{
						Name = requiredArguments[0],
						PlayerCount = requiredArguments.Count - 1
					}
				};
				for (int i = 0; i < conf.Count; i++)
				{
					string arg = requiredArguments[i + 1];
					if (File.Exists(arg))
						conf[i].Type = PlayerType.LocalAI;
					else if (arg.Length == 24 && arg.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f'))
						conf[i].Type = PlayerType.BotzoneBot;
					else
						throw new FormatException(
							String.Format(StringResources.CONSOLE_BAD_ID, arg));
					conf[i].ID = arg;
				}
				if (!conf.IsValid)
					throw new FormatException(conf.ValidationString);
			}
			catch (FormatException e)
			{
				Logger.Log(LogLevel.Error, e.Message);
				Console.WriteLine(String.Format(StringResources.CONSOLE_HELP, args[0]));
				return;
			}

			if (conf.IsLocalMatch)
			{
				Logger.Log(LogLevel.Info, StringResources.CONSOLE_LOCALMATCH);
				Cef.Initialize(new CefSettings
				{
					Locale = CultureInfo.CurrentCulture.Name
				});
				var tcs = new TaskCompletionSource<object>();
				var b = new CefSharp.OffScreen.ChromiumWebBrowser();
				BotzoneProtocol.CurrentBrowser = b;
				b.BrowserInitialized += delegate
				{
					tcs.SetResult(null);
				};
				tcs.Task.Wait();
			}
			else
			{
				Logger.Log(LogLevel.Info, StringResources.CONSOLE_BOTZONEMATCH);
				if (!BotzoneProtocol.Credentials.IsValid)
				{
					Logger.Log(LogLevel.Error, StringResources.CONSOLE_BAD_LOCALAI_URL);
					Console.WriteLine(String.Format(StringResources.CONSOLE_HELP, args[0]));
					return;
				}
			}

			try
			{
				// 创建并开始对局
				var matchTask = conf.CreateMatch();
				matchTask.Wait();
				var match = matchTask.Result;
				Console.CancelKeyPress += delegate
				{
					match.AbortMatch().Wait();
				};
				match.RunMatch().Wait();

				string logJson = JsonConvert.SerializeObject(match.Logs);
				if (outputLogPath != null)
					using (var sw = new StreamWriter(outputLogPath))
					{
						sw.WriteLine(logJson);
					}

				if (outputMatchCollectionPath != null)
				{
					using (var s = File.Open(outputMatchCollectionPath, FileMode.Create))
					{
						var tempMatches = new Match[matches?.Length ?? 0];
						matches?.CopyTo(tempMatches, 0);
						tempMatches[tempMatches.Length - 1] = match;
						var f = new BinaryFormatter();
						f.Serialize(s, tempMatches);
					}
				}

				Console.WriteLine(logJson);
				Console.WriteLine(
					(match.Status == MatchStatus.Finished ? "finished " : "aborted ") +
					String.Join(" ", match.Scores));
			}
			catch (Exception e)
			{
				Logger.Log(LogLevel.Error, e.Message);
				Console.WriteLine("error");
			}
			finally
			{
				Cef.Shutdown();
			}
		}
	}
}
