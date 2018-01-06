using CefSharp;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.ComponentModel;
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
using System.Runtime.Serialization.Formatters.Binary;

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

		OpenFileDialog ofdLocalAI = new OpenFileDialog
		{
			ValidateNames = true,
			Title = StringResources.OFD_TITLE,
			Filter = StringResources.OFD_FILTER
		};

		SaveFileDialog sfdMatches = new SaveFileDialog
		{
			ValidateNames = true,
			Title = StringResources.MATCHES_SFD_TITLE,
			Filter = StringResources.MATCHES_FILTER
		};

		OpenFileDialog ofdMatches = new OpenFileDialog
		{
			ValidateNames = true,
			Title = StringResources.MATCHES_OFD_TITLE,
			Filter = StringResources.MATCHES_FILTER
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
			WebBrowser.LoadingStateChanged += WebBrowser_LoadingStateChanged;

			ViewModel.MatchCollection = new ObservableCollection<Match>();
			ViewModel.TimeLimit = Properties.Settings.Default.TimeLimit;
			LastConf = new SavedConfiguration();
			ViewModel.CurrentConfiguration = new MatchConfiguration();
			ViewModel.Credentials = BotzoneProtocol.Credentials;
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
			AbortMatch().Wait();
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
			if (ofdLocalAI.ShowDialog(this) == true)
				player.ID = ofdLocalAI.FileName;
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
			Match match = null;
			try
			{
				match = await ViewModel.CurrentConfiguration.CreateMatch();
				if (match is BotzoneMatch)
					WebBrowser.Load(Properties.Settings.Default.BotzoneMatchURLBase + (match as BotzoneMatch).MatchID);

				ViewModel.MatchStarted = true;

				await match.RunMatch();
			}
			catch
			{
				Logger.Log(LogLevel.No, StringResources.MATCH_FAILED);
				if (match != null)
					match.Status = MatchStatus.Aborted;
			}
			finally
			{
				ViewModel.MatchStarted = false;
				ViewModel.MatchCollection.Add(match);
			}
		}

		private void WebBrowser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
		{
			if (e.IsLoading == false)
				WebBrowser.EvaluateScriptAsync(AssetResources.SimplifyOnlineMatch);
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

		private void btnClear_Click(object sender, RoutedEventArgs e)
		{
			int count = ViewModel.MatchCollection.Count;
			ViewModel.MatchCollection.Clear();
			Logger.Log(LogLevel.Warning, $"已删除 {count} 场对局");
		}

		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			if (sfdMatches.ShowDialog(this) == true)
			{
				using (var s = sfdMatches.OpenFile())
				{
					var f = new BinaryFormatter();
					f.Serialize(s, ViewModel.MatchCollection.ToArray());
				}
				Logger.Log(LogLevel.OK, $"已保存 {ViewModel.MatchCollection.Count} 场对局");
			}
		}

		private void btnLoad_Click(object sender, RoutedEventArgs e)
		{
			if (ofdMatches.ShowDialog(this) == true)
			{
				try
				{
					using (var s = ofdMatches.OpenFile())
					{
						var f = new BinaryFormatter();
						var collection = f.Deserialize(s) as Match[];
						if (collection != null)
						{
							ViewModel.MatchCollection = new ObservableCollection<Match>(collection);
							Logger.Log(LogLevel.OK, $"已读取 {ViewModel.MatchCollection.Count} 场对局");
						}
					}
				}
				catch (Exception ex)
				{
					Logger.Log(LogLevel.Error, ex.Message);
					Logger.Log(LogLevel.Error, StringResources.BAD_MATCH_COLLECTION_FORMAT);
				}
			}
		}

		private async void List_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (ViewModel.MatchStarted)
			{
				MessageBox.Show(this, StringResources.MATCH_RUNNING_NO_REPLAY, StringResources.MESSAGE,
					MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return;
			}
			var list = sender as ListView;
			var match = list.SelectedItem as Match;
			if (match != null)
			{
				match.Configuration.Game = ViewModel.AllGames.First(g => g.Name == match.Configuration.Game.Name);
				await Task.Delay(500);
				ViewModel.CurrentConfiguration = match.Configuration;
				match.ReplayMatch(WebBrowser);
			}
		}

		private async Task AbortMatch()
		{
			if (ViewModel.MatchStarted)
			{
				var match = Match.ActiveMatch;
				await Match.ActiveMatch.AbortMatch();
				ViewModel.MatchStarted = false;
				ViewModel.MatchCollection.Add(match);
			}
		}

		private async void btnAbortMatch_Click(object sender, RoutedEventArgs e)
		{
			await AbortMatch();
		}
	}
}
