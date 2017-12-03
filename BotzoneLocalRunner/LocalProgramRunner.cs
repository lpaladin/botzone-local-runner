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
		public List<string> Requests { get; set; } = new List<string>();
		public List<string> Responses { get; set; } = new List<string>();

		public Task RunForResponse()
		{
			var p = RunProgram();
			p.StandardInput.WriteLine(Requests.Count);
			for (int i = 0; i < Responses.Count; i++)
			{
				p.StandardInput.WriteLine(Requests[i]);
				p.StandardInput.WriteLine(Responses[i]);
			}
			p.StandardInput.WriteLine(Requests.Last());
			p.StandardInput.Close();

			return new Task(() =>
			{
				p.WaitForExit();
				Responses.Add(p.StandardOutput.ReadLine());
			});
		}

		internal Process RunProgram()
		{
			var processInfo = new ProcessStartInfo
			{
				CreateNoWindow = true, RedirectStandardInput = true,
				RedirectStandardError = true, RedirectStandardOutput = true,
				WorkingDirectory = Path.GetDirectoryName(ProgramPath)
			};

			switch (Path.GetExtension(ProgramPath))
			{
				case "exe":
					processInfo.FileName = ProgramPath;
					break;
				case "py":
					processInfo.FileName = "python " + ProgramPath;
					break;
				case "js":
					processInfo.FileName = "node " + ProgramPath;
					break;
				case "class":
					processInfo.FileName = "java " + Path.GetFileNameWithoutExtension(ProgramPath);
					break;
				default:
					return null;
			}

			return Process.Start(processInfo);
		}
	}
}
