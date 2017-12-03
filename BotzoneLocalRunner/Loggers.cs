using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace BotzoneLocalRunner
{
	public enum LogLevel
	{
		Info,
		InfoTip,
		OK,
		Warning,
		No,
		Block,
		Error
	}

	public interface ILogger
	{
		void Log(LogLevel level, string message);
	}

	public class ConsoleLogger : ILogger
	{
		public void Log(LogLevel level, string message)
		{
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write($"[{DateTime.Now}] ");
			if (level <= LogLevel.InfoTip)
				Console.ForegroundColor = ConsoleColor.Gray;
			else if (level == LogLevel.OK)
				Console.ForegroundColor = ConsoleColor.Green;
			else if (level == LogLevel.Warning)
				Console.ForegroundColor = ConsoleColor.Yellow;
			else if (level <= LogLevel.Block)
				Console.ForegroundColor = ConsoleColor.Red;
			else
			{
				Console.ForegroundColor = ConsoleColor.White;
				Console.BackgroundColor = ConsoleColor.Red;
			}
			Console.WriteLine(message);
			Console.ResetColor();
		}
    }

    [ValueConversion(typeof(LogLevel), typeof(BitmapSource))]
    public class LogLevelToWPFBitmapConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Bitmap b = null;
            switch ((LogLevel)value)
            {
                case LogLevel.Block: b = AssetResources.Deny; break;
                case LogLevel.Error: b = AssetResources.Error; break;
                case LogLevel.Info: b = AssetResources.Info; break;
                case LogLevel.InfoTip: b = AssetResources.InvertInfo; break;
                case LogLevel.No: b = AssetResources.No; break;
                case LogLevel.OK: b = AssetResources.OK; break;
                case LogLevel.Warning: b = AssetResources.Warning; break;
            }
            var hBitmap = b.GetHbitmap();

            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                BitmapHelper.DeleteObject(hBitmap);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => null;

        public override object ProvideValue(IServiceProvider serviceProvider)
            => this;
    }

    public class ViewModelLogger : ILogger
	{
		public class LogItem
		{
			public LogLevel Level { get; set; }
			public string Date { get; set; }
			public string Message { get; set; }
        }

        static readonly int LogMaxLength = 4096;

		ObservableCollection<LogItem> ViewModel { get; }

		internal ViewModelLogger(ObservableCollection<LogItem> viewModel)
		{
			ViewModel = viewModel;
		}

		public void Log(LogLevel level, string message)
		{
			ViewModel.Add(new LogItem
			{
				Level = level,
				Date = DateTime.Now.ToString(),
				Message = message
			});
			if (ViewModel.Count > LogMaxLength)
				ViewModel.RemoveAt(0);
		}
	}
}
