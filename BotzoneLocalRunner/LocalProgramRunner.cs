using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BotzoneLocalRunner
{
	internal class LocalProgramRunner
	{
		public string ProgramPath { get; set; }
		public List<dynamic> Requests { get; set; } = new List<dynamic>();
		public List<dynamic> Responses { get; set; } = new List<dynamic>();
		public dynamic Data { get; set; }
		public dynamic GlobalData { get; set; }

		// 暂时先不考虑本地程序不是简单IO的情况
		public bool IsSimpleIO { get; set; } = true;

		/// <summary>
		/// 运行程序，并返回程序运行时间
		/// </summary>
		/// <returns>程序运行了多少毫秒</returns>
		public async Task<int> RunForResponse()
		{
			var p = RunProgram();
			p.EnableRaisingEvents = true;

			var tcsExited = new TaskCompletionSource<object>();
			var tcsTimeout = new TaskCompletionSource<object>();
			p.Exited += (sender, e) => tcsExited.SetResult(null);
			p.StandardInput.WriteLine(Requests.Count);
			for (int i = 0; i < Responses.Count; i++)
			{
				p.StandardInput.WriteLine(Requests[i]);
				p.StandardInput.WriteLine(Responses[i]);
			}
			p.StandardInput.WriteLine(Requests.Last());
			p.StandardInput.Close();
			
			using (CancellationTokenSource cts = new CancellationTokenSource())
			{
				Task exitedTask = tcsExited.Task;
				Task completedTask;
				using (cts.Token.Register(o => ((TaskCompletionSource<object>)o).SetResult(false), tcsTimeout))
				{
					cts.CancelAfter(Properties.Settings.Default.HardTimeout);
					completedTask = await Task.WhenAny(exitedTask, tcsTimeout.Task);
				}
				
				if (completedTask != exitedTask)
					throw new TimeoutException();
				await exitedTask;
			}
			Responses.Add(p.StandardOutput.ReadLine());
			return (int)p.TotalProcessorTime.TotalMilliseconds;
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
					processInfo.FileName = "python " + ProgramPath;
					break;
				case ".js":
					processInfo.FileName = "node " + ProgramPath;
					break;
				case ".class":
					processInfo.FileName = "java " + Path.GetFileNameWithoutExtension(ProgramPath);
					break;
				default:
					return null;
			}

			return Process.Start(processInfo);
		}
	}
}
