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
using System.Threading.Tasks;

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
		private TimeSpan _TimeLimit;
		public TimeSpan TimeLimit
		{
			get => _TimeLimit;
			set
			{
				if (value != _TimeLimit)
				{
					_TimeLimit = value;
					Properties.Settings.Default.TimeLimit = value;
					NotifyPropertyChanged("TimeLimit");
				}
			}
		}

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


		private ObservableCollection<Match> _MatchCollection;
		public ObservableCollection<Match> MatchCollection
		{
			get => _MatchCollection;
			set
			{
				if (value != _MatchCollection)
				{
					_MatchCollection = value;
					NotifyPropertyChanged("MatchCollection");
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
		[UserScopedSetting]
		[SettingsSerializeAs(SettingsSerializeAs.Binary)]
		[DefaultSettingValue("")]
		public PlainPlayerConfiguration[] Configuration
		{
			get
			{
				return (PlainPlayerConfiguration[])this["Configuration"];
			}
			set
			{
				this["Configuration"] = value;
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
			BotzoneProtocol.CurrentBrowser = WebBrowser;
			BrowserJSObject.Init();

			ViewModel.TimeLimit = Properties.Settings.Default.TimeLimit;
			LastConf = new SavedConfiguration();
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
					String.Join(",", CultureInfo.CurrentCulture.Name, CultureInfo.CurrentCulture.TwoLetterISOLanguageName, "en-US", "en")
			};
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			if (ViewModel.CurrentConfiguration.Game != null)
			{
				Properties.Settings.Default.LastSelectedGame = ViewModel.CurrentConfiguration.Game.Name;
				LastConf.Configuration = (from player in ViewModel.CurrentConfiguration
					select new PlainPlayerConfiguration
					{
						ID = player.ID,
						Type = player.Type
					}).ToArray();
			}
			Properties.Settings.Default.Save();
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
			var lastGame = Properties.Settings.Default.LastSelectedGame;
			if (lastGame?.Length > 0)
			{
				ViewModel.CurrentConfiguration.Game = ViewModel.AllGames.First(x => x.Name == lastGame);

				// Dirty hack……设置Game后，绑定了Type的ComboBox会在下一时刻更新Type，并覆盖掉这里的初值……
				// 所以要等一时刻
				await Task.Delay(500);
				if (LastConf.Configuration?.Length > 0)
					for (int i = 0; i < LastConf.Configuration.Length; i++)
					{
						ViewModel.CurrentConfiguration[i].Type = LastConf.Configuration[i].Type;
						ViewModel.CurrentConfiguration[i].ID = LastConf.Configuration[i].ID;
					}
			}
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
			if (s?.IsReadOnly == true)
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
					WebBrowser.Load(Properties.Settings.Default.BotzoneMatchURLBase + (match as BotzoneMatch).MatchID);

				WebBrowser.LoadingStateChanged += WebBrowser_LoadingStateChanged;

				ViewModel.MatchStarted = true;
				await match.RunMatch();
			}
			//catch
			//{
			//	Logger.Log(LogLevel.No, "对局失败");
			//}
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
			(parent?.Child as ScrollViewer)?.ScrollToEnd();
		}
	}
}
