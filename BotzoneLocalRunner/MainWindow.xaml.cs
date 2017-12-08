using CefSharp;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System;
using System.Linq;
using System.Globalization;
using System.Windows.Interop;
using System.Text.RegularExpressions;
using static BotzoneLocalRunner.Util;
using System.Windows.Media;
using System.Configuration;

namespace BotzoneLocalRunner
{
	internal static class NativeMethods
	{
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AddClipboardFormatListener(IntPtr hwnd);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
	}

	public class MainWindowViewModel : INotifyPropertyChanged
	{

		private BotzoneCredentials _Credentials;
		public BotzoneCredentials Credentials
		{
			get
			{
				return _Credentials;
			}
			set
			{
				if (value != _Credentials)
				{
					if (_Credentials != null)
						_Credentials.ValidationChanged -= UpdateValidation;

					_Credentials = value;
					_Credentials.ValidationChanged += UpdateValidation;
					NotifyPropertyChanged("Credentials");
				}
			}
		}

		private MatchConfiguration _CurrentConfiguration;
		public MatchConfiguration CurrentConfiguration
		{
			get
			{
				return _CurrentConfiguration;
			}
			set
			{
				if (value != _CurrentConfiguration)
				{
					if (_CurrentConfiguration != null)
						_CurrentConfiguration.ValidationChanged -= UpdateValidation;

					_CurrentConfiguration = value;
					_CurrentConfiguration.ValidationChanged += UpdateValidation;
					NotifyPropertyChanged("CurrentConfiguration");
				}
			}
		}

		private void UpdateValidation(object sender, System.EventArgs e)
		{
			if (Credentials?.IsValid == false)
			{
				IsValid = false;
				ValidationString = Credentials.ValidationString;
				return;
			}
			IsValid = CurrentConfiguration.IsValid;
			ValidationString = CurrentConfiguration.ValidationString;
		}

		private RangeObservableCollection<Game> _AllGames;
		public RangeObservableCollection<Game> AllGames
		{
			get
			{
				return _AllGames;
			}
			set
			{
				if (value != _AllGames)
				{
					_AllGames = value;
					NotifyPropertyChanged("AllGames");
				}
			}
		}

		private LogCollection _Logs;
		public LogCollection Logs
		{
			get
			{
				return _Logs;
			}
			set
			{
				if (value != _Logs)
				{
					_Logs = value;
					NotifyPropertyChanged("Logs");
				}
			}
		}

		private bool _MatchStarted = false;
		public bool MatchStarted
		{
			get
			{
				return _MatchStarted;
			}
			set
			{
				if (value != _MatchStarted)
				{
					_MatchStarted = value;
					NotifyPropertyChanged("MatchStarted");
				}
			}
		}

		private bool _GamesLoaded = false;
		public bool GamesLoaded
		{
			get
			{
				return _GamesLoaded;
			}
			set
			{
				if (value != _GamesLoaded)
				{
					_GamesLoaded = value;
					NotifyPropertyChanged("GamesLoaded");
				}
			}
		}

		private bool _IsValid = false;
		public bool IsValid
		{
			get
			{
				return _IsValid;
			}
			set
			{
				if (value != _IsValid)
				{
					_IsValid = value;
					NotifyPropertyChanged("IsValid");
				}
			}
		}


		private string _ValidationString = StringResources.BAD_LOCALAI_URL;
		public string ValidationString
		{
			get
			{
				return _ValidationString;
			}
			set
			{
				if (value != _ValidationString)
				{
					_ValidationString = value;
					NotifyPropertyChanged("ValidationString");
				}
			}
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public event PropertyChangedEventHandler PropertyChanged;
	}

	public class SavedConfiguration : ApplicationSettingsBase
	{
		[UserScopedSetting()]
		[SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
		[DefaultSettingValue("")]
		public MatchConfiguration Configuration
		{
			get
			{
				return ((MatchConfiguration)this["Configuration"]);
			}
			set
			{
				this["Configuration"] = (MatchConfiguration)value;
			}
		}
	}

	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window
	{
		private const int WM_CLIPBOARDUPDATE = 0x031D;
		private IntPtr hwnd;

		OpenFileDialog ofd = new OpenFileDialog
		{
			ValidateNames = true,
			Title = StringResources.OFD_TITLE,
			Filter = StringResources.OFD_FILTER
		};

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			hwnd = new WindowInteropHelper(this).EnsureHandle();
			HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);

			NativeMethods.AddClipboardFormatListener(hwnd);
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == WM_CLIPBOARDUPDATE)
			{
				IDataObject iData = Clipboard.GetDataObject();

				if (iData.GetDataPresent(DataFormats.Text))
				{
					string text = (string)iData.GetData(DataFormats.Text);
					if (Regex.IsMatch(text, StringResources.BOTZONE_LOCALAI_URL_REGEX))
						ViewModel.Credentials.BotzoneCopiedURL = text;
				}
			}

			return IntPtr.Zero;
		}

		private SavedConfiguration LastConf;

		public MainWindow()
		{
			InitializeComponent();

			LastConf = new SavedConfiguration();
			if (LastConf.Configuration != null)
				ViewModel.CurrentConfiguration = LastConf.Configuration;
			else
				ViewModel.CurrentConfiguration = new MatchConfiguration();
			BotzoneProtocol.Credentials = ViewModel.Credentials = new BotzoneCredentials();
			ViewModel.AllGames = new RangeObservableCollection<Game>(new[] { new Game { Name = "..." } });
			ViewModel.Logs = new LogCollection();

			if (Properties.Settings.Default.LastBotzoneLocalAIURL?.Length > 0)
				BotzoneProtocol.Credentials.BotzoneCopiedURL = Properties.Settings.Default.LastBotzoneLocalAIURL;

			Logger = new ViewModelLogger(ViewModel.Logs);
			WebBrowser.BrowserSettings = new BrowserSettings
			{
				AcceptLanguageList =
					String.Join(",", new[]
					{
						CultureInfo.CurrentCulture.Name,
						CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
						"en-US",
						"en"
					})
			};
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			LastConf.Configuration = ViewModel.CurrentConfiguration;
			LastConf.Save();
			Cef.Shutdown();
			NativeMethods.RemoveClipboardFormatListener(hwnd);
		}

		private async void Window_Loaded(object sender, RoutedEventArgs e)
		{
			var games = await BotzoneProtocol.GetGames();
			ViewModel.AllGames.RemoveAt(0);
			ViewModel.AllGames.AddRange(games);
			ViewModel.GamesLoaded = true;
		}

		private void btnSelect_Click(object sender, RoutedEventArgs e)
		{
			var player = (sender as Button)?.Tag as PlayerConfiguration;
			if (ofd.ShowDialog(this) == true)
				player.ID = ofd.FileName;
		}

		private void txtContent_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			var s = (sender as TextBox);
			if (s.IsReadOnly)
				return;

			s.Focus();
			s.SelectAll();
		}

		private void txtLocalAIURL_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			var s = (sender as TextBox);
			if (s.IsReadOnly)
				return;

			s.Focus();
			s.SelectAll();
		}

		private async void btnStartMatch_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var match = await ViewModel.CurrentConfiguration.CreateMatch();
				if (match is BotzoneMatch)
				{
					WebBrowser.Load(Properties.Settings.Default.BotzoneMatchURLBase + (match as BotzoneMatch).MatchID);
					WebBrowser.LoadingStateChanged += WebBrowser_LoadingStateChanged;
				}

				ViewModel.MatchStarted = true;
				await match.RunMatch();
			}
			catch
			{
				Logger.Log(LogLevel.No, "对局失败");
			}
			finally
			{
				ViewModel.MatchStarted = false;
			}
		}

		private void WebBrowser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
		{
			if (e.IsLoading == false)
			{
				WebBrowser.EvaluateScriptAsync(AssetResources.SimplifyOnlineMatch);
				WebBrowser.LoadingStateChanged -= WebBrowser_LoadingStateChanged;
			}
		}

		private void ScrollChangedAndScrollToEnd(object sender, ScrollChangedEventArgs e)
		{
			if (e.ExtentHeightChange > 0.0)
				((ScrollViewer)e.OriginalSource).ScrollToEnd();
		}

		private void List_Loaded(object sender, RoutedEventArgs e)
		{
			var parent = VisualTreeHelper.GetChild(sender as FrameworkElement, 0) as Decorator;
			(parent?.Child as ScrollViewer).ScrollToEnd();
		}
	}
}
