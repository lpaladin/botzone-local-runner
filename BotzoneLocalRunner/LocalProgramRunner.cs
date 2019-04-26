using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BotzoneLocalRunner
{
	internal class RuntimeException : Exception
	{
		internal RuntimeException(string output) : base(output) { }
	}

	public class LocalProgramRunner
	{
		public string ProgramPath { get; set; }
		public List<dynamic> Requests { get; set; } = new List<dynamic>();
		public List<dynamic> Responses { get; set; } = new List<dynamic>();
		public dynamic Data { get; set; }
		public dynamic GlobalData { get; set; }
		public Process CurrentProcess { get; set; }

		// 暂时默认为非简单IO（TODO：命令行下）
		public static bool IsSimpleIO { get; set; } = false;

		/// <summary>
		/// 运行程序，直到得到反馈
		/// </summary>
		/// <returns>程序运行状况</returns>
		public async Task<ProgramLogItem> RunForResponse()
		{
			try
			{
				var p = CurrentProcess = RunProgram();
				p.EnableRaisingEvents = true;

				var tcsExited = new TaskCompletionSource<object>();
				var tcsTimeout = new TaskCompletionSource<object>();
				p.Exited += (sender, e) => tcsExited.SetResult(null);

				if (IsSimpleIO)
				{
					p.StandardInput.WriteLine(Requests.Count);
					for (int i = 0; i < Responses.Count; i++)
					{
						p.StandardInput.WriteLine(Requests[i].Trim());
						p.StandardInput.WriteLine(Responses[i].Trim());
					}
					p.StandardInput.WriteLine(Requests.Last().Trim());
					p.StandardInput.WriteLine(Data ?? "");
					p.StandardInput.WriteLine(GlobalData ?? "");
					p.StandardInput.Close();
				}
				else
				{
					p.StandardInput.WriteLine(JsonConvert.SerializeObject(new
					{
						requests = Requests,
						responses = Responses,
						data = Data,
						globaldata = GlobalData,
						time_limit = Properties.Settings.Default.TimeLimit.TotalMilliseconds,
						memory_limit = 256
					}, Formatting.None));
					p.StandardInput.Close();
				}

				// 超时处理
				using (CancellationTokenSource cts = new CancellationTokenSource())
				{
					Task exitedTask = tcsExited.Task;
					Task completedTask;
					using (cts.Token.Register(o => ((TaskCompletionSource<object>)o).SetResult(false), tcsTimeout))
					{
						cts.CancelAfter(Properties.Settings.Default.TimeLimit);
						completedTask = await Task.WhenAny(exitedTask, tcsTimeout.Task);
					}

					if (completedTask != exitedTask)
					{
						p.Kill();
						throw new TimeoutException();
					}
					await exitedTask;
				}

				if (p.ExitCode != 0)
					throw new RuntimeException(p.StandardError.ReadToEnd());

				if (IsSimpleIO)
				{
					var raw = p.StandardOutput.ReadLine();
					var debug = p.StandardOutput.ReadLine();

					Data = p.StandardOutput.ReadLine();
					GlobalData = p.StandardOutput.ReadLine();
					Responses.Add(raw);
					return new ProgramLogItem
					{
						time = (int)p.TotalProcessorTime.TotalMilliseconds,
						// memory = (int)(p.PeakWorkingSet64 / 1024 / 1024),
						raw = raw,
						debug = debug,
						verdict = "OK"
					};
				}
				else
				{
					dynamic resp = JsonConvert.DeserializeObject(p.StandardOutput.ReadToEnd());
					Data = resp.data;
					GlobalData = resp.globaldata;
					Responses.Add(resp.response);
					return new ProgramLogItem
					{
						time = (int)p.TotalProcessorTime.TotalMilliseconds,
						// memory = (int)(p.PeakWorkingSet64 / 1024 / 1024),
						response = resp.response,
						debug = resp.debug,
						verdict = "OK"
					};
				}
			}
			finally
			{
				CurrentProcess = null;
			}
		}

		internal Process RunProgram()
		{
			var processInfo = new ProcessStartInfo
			{
				CreateNoWindow = true, RedirectStandardInput = true,
				RedirectStandardError = true, RedirectStandardOutput = true,
				UseShellExecute = false,
				WorkingDirectory = Path.GetDirectoryName(ProgramPath)
			};

			switch (Path.GetExtension(ProgramPath))
			{
				case ".exe":
					processInfo.FileName = ProgramPath;
					break;
				case ".py":
					processInfo.FileName = "python";
					processInfo.Arguments = $"\"{ProgramPath}\"";
					break;
				case ".js":
					processInfo.FileName = "node";
					processInfo.Arguments = $"\"{ProgramPath}\"";
					break;
				case ".class":
					processInfo.FileName = "java";
					processInfo.Arguments = Path.GetFileNameWithoutExtension(ProgramPath);
					break;
				default:
					return null;
			}

			return Process.Start(processInfo);
		}
	}
}
