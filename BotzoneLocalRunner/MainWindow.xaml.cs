using CefSharp;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace BotzoneLocalRunner
{
	internal class MainWindowViewModel : INotifyPropertyChanged
	{
		public BotzoneProtocol.BotzoneCredentials Credentials { get; set; }

		private MatchConfiguration _CurrentConfiguration;
		public MatchConfiguration CurrentConfiguration {
			get => _CurrentConfiguration;
			set
			{
				if (value != _CurrentConfiguration)
				{
					_CurrentConfiguration = value;
					NotifyPropertyChanged("CurrentConfiguration");
				}
			}
		}


		private RangeObservableCollection<Game> _AllGames;
		public RangeObservableCollection<Game> AllGames
		{
			get => _AllGames;
			set
			{
				if (value != _AllGames)
				{
					_AllGames = value;
					NotifyPropertyChanged("AllGames");
				}
			}
		}

		private ObservableCollection<ViewModelLogger.LogItem> _Logs;
		public ObservableCollection<ViewModelLogger.LogItem> Logs
		{
			get => _Logs;
			set
			{
				if (value != _Logs)
				{
					_Logs = value;
					NotifyPropertyChanged("Logs");
				}
			}
		}

		private bool _GamesLoaded = false;
		public bool GamesLoaded
		{
			get => _GamesLoaded;
			set
			{
				if (value != _GamesLoaded)
				{
					_GamesLoaded = value;
					NotifyPropertyChanged("GamesLoaded");
				}
			}
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public event PropertyChangedEventHandler PropertyChanged;
	}

	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window
	{
		OpenFileDialog ofd = new OpenFileDialog
		{
			ValidateNames = true,
			Title = StringResources.OFD_TITLE,
			Filter = StringResources.OFD_FILTER
		};

		public MainWindow()
		{
			InitializeComponent();

			ViewModel.Credentials = new BotzoneProtocol.BotzoneCredentials();
			ViewModel.AllGames = new RangeObservableCollection<Game>(new Game[] { new Game { Name = "..." } });
			ViewModel.CurrentConfiguration = new MatchConfiguration();
			ViewModel.Logs = new ObservableCollection<ViewModelLogger.LogItem>();

			Util.Logger = new ViewModelLogger(ViewModel.Logs);
			WebBrowser.Load("https://www.botzone.org");
		}

		private async void btnLocalAI_Click(object sender, RoutedEventArgs e)
		{
			if (ofd.ShowDialog() == true)
				MessageBox.Show(await LocalProgramRunner
					.RunProgram(ofd.FileName)
					.StandardOutput
					.ReadToEndAsync());
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			Cef.Shutdown();
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
			s.Focus();
			s.SelectAll();
		}

		private void txtLocalAIURL_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			var s = (sender as TextBox);
			s.Focus();
			s.SelectAll();
		}
	}
}
