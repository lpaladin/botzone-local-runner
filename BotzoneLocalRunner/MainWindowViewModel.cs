using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System;

namespace BotzoneLocalRunner
{
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
}
