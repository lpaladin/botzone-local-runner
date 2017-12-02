using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotzoneLocalRunner
{
	internal static class LocalProgramRunner
	{
		internal static Process RunProgram(string programPath)
		{
			var processInfo = new ProcessStartInfo
			{
				CreateNoWindow = true, RedirectStandardInput = true,
				RedirectStandardError = true, RedirectStandardOutput = true,
				WorkingDirectory = Path.GetDirectoryName(programPath)
			};

			switch (Path.GetExtension(programPath))
			{
				case "exe":
					processInfo.FileName = programPath;
					break;
				case "py":
					processInfo.FileName = "python " + programPath;
					break;
				case "js":
					processInfo.FileName = "node " + programPath;
					break;
				case "class":
					processInfo.FileName = "java " + Path.GetFileNameWithoutExtension(programPath);
					break;
				default:
					return null;
			}

			return Process.Start(processInfo);
		}
	}
}
