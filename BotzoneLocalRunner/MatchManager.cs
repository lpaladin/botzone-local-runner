using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BotzoneLocalRunner
{
	internal enum PlayerType
	{
		[Description("本地AI程序")]
		LocalAI,
		[Description("你自己（人类）")]
		LocalHuman,
		[Description("Botzone上的AI")]
		BotzoneBot
	}

	internal class Game
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public int PlayerCount { get; set; }
	}

	internal class PlayerConfiguration : INotifyPropertyChanged, IDataErrorInfo
	{
		private PlayerType _Type = PlayerType.LocalAI;
		public PlayerType Type
		{
			get => _Type;
			set
			{
				if (value != _Type)
				{
					_Type = value;
					if (value == PlayerType.LocalHuman)
						ID = StringResources.LOCALHUMAN_PLACEHOLDER;
					else if (value == PlayerType.LocalAI)
						ID = StringResources.LOCALAI_PLACEHOLDER;
					else if (value == PlayerType.BotzoneBot)
						ID = StringResources.BOTZONEBOT_PLACEHOLDER;
					NotifyPropertyChanged("Type");
				}
			}
		}


		private string _ID = StringResources.LOCALAI_PLACEHOLDER;
		public string ID
		{
			get => _ID;
			set
			{
				if (value != _ID)
				{
					_ID = value;
					NotifyPropertyChanged("ID");
				}
			}
		}

		public bool IsValid
		{
			get
			{
				return ID.Length != 0 &&
					ID != StringResources.LOCALAI_PLACEHOLDER &&
					ID != StringResources.BOTZONEBOT_PLACEHOLDER;
			}
		}

		public string Error => throw new NotImplementedException();

		public string this[string columnName]
		{
			get
			{
				if (columnName == "ID" && !IsValid)
					return StringResources.ID_EMPTY;
				return null;
			}
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public event PropertyChangedEventHandler PropertyChanged;
	}

	internal class MatchConfiguration : ObservableCollection<PlayerConfiguration>
	{
		private Game _Game;
		public Game Game
		{
			get => _Game;
			set
			{
				if (value != _Game)
				{
					while (value.PlayerCount > Count)
						Add(new PlayerConfiguration());
					while (value.PlayerCount < Count)
						RemoveAt(Count - 1);
					_Game = value;
					OnPropertyChanged(new PropertyChangedEventArgs("Game"));
				}
			}
		}

		private bool _IsValid = false;
		public bool IsValid
		{
			get => _IsValid;
			set
			{
				if (value != _IsValid)
				{
					_IsValid = value;
					OnPropertyChanged(new PropertyChangedEventArgs("IsValid"));
				}
			}
		}


		private string _ValidationString;
		public string ValidationString
		{
			get => _ValidationString;
			set
			{
				if (value != _ValidationString)
				{
					_ValidationString = value;
					OnPropertyChanged(new PropertyChangedEventArgs("ValidationString"));
				}
			}
		}

		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			base.OnCollectionChanged(e);
			PlayerConfigurationPropertyChanged(null, null);
			if (e.Action == NotifyCollectionChangedAction.Remove)
				foreach (PlayerConfiguration item in e.OldItems)
					item.PropertyChanged -= PlayerConfigurationPropertyChanged;
			else if (e.Action == NotifyCollectionChangedAction.Add)
				foreach (PlayerConfiguration item in e.NewItems)
					item.PropertyChanged += PlayerConfigurationPropertyChanged;
		}

		public void PlayerConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			int botzoneAICount = 0;
			foreach (var config in this)
			{
				if (!config.IsValid)
				{
					IsValid = false;
					ValidationString = StringResources.ID_EMPTY;
					return;
				}
				if (config.Type == PlayerType.BotzoneBot)
					botzoneAICount++;
			}
			if (botzoneAICount != 0 && botzoneAICount != Count - 1)
			{
				IsValid = false;
				ValidationString = String.Format(StringResources.WRONG_BOTZONE_AI_COUNT, Count - 1);
				return;
			}
			IsValid = true;
		}
	}

	internal class Match
	{
		public MatchConfiguration Configuration { get; set; }
		public ArrayList DisplayLogs { get; set; }
		public dynamic[] Logs { get; set; }
		public double[] Scores { get; set; }
	}
}
