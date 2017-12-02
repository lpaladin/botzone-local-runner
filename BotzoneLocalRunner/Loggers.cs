using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace BotzoneLocalRunner
{
	internal interface ILogger
	{
		void Log(string Message);
	}
	internal class ConsoleLogger : ILogger
	{
		public void Log(string Message)
		{
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write($"[{DateTime.Now}] ");
			Console.ResetColor();
			Console.WriteLine(Message);
		}
	}
	internal class ViewModelLogger : ILogger
	{
		internal class LogItem
		{
			public string Date { get; set; }
			public string Message { get; set; }
		}

		static readonly int LogMaxLength = 4096;

		ObservableCollection<LogItem> ViewModel { get; }

		internal ViewModelLogger(ObservableCollection<LogItem> viewModel)
		{
			ViewModel = viewModel;
		}

		public void Log(string Message)
		{
			ViewModel.Add(new LogItem
			{
				Date = DateTime.Now.ToString(),
				Message = Message
			});
			if (ViewModel.Count > LogMaxLength)
				ViewModel.RemoveAt(0);
		}
	}
}
