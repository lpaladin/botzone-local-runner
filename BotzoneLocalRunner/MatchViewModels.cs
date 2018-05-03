using System;
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
	public enum PlayerType
	{
		[Description("本地AI程序")]
		LocalAI,
		[Description("你自己（人类）")]
		LocalHuman,
		[Description("Botzone上的AI")]
		BotzoneBot
	}
	
	public class Game
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public int PlayerCount { get; set; }
	}

	[Serializable]
	public class PlainPlayerConfiguration
	{
		public PlayerType Type { get; set; }
		public string ID { get; set; }
	}

	public class PlayerConfiguration : INotifyPropertyChanged, IDataErrorInfo
	{
		private PlayerType _Type = PlayerType.LocalAI;
		public PlayerType Type
		{
			get
			{
				return _Type;
			}
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
					NotifyPropertyChanged();
				}
			}
		}

		public int SlotID { get; set; }

		private string _ID = StringResources.LOCALAI_PLACEHOLDER;
		public string ID
		{
			get
			{
				return _ID;
			}
			set
			{
				if (value != _ID)
				{
					_ID = value;
					if (ID.Length != 0 &&
						ID != StringResources.LOCALAI_PLACEHOLDER &&
						ID != StringResources.BOTZONEBOT_PLACEHOLDER)
						IsValid = true;
					else
					{
						IsValid = false;
						ValidationString = Type == PlayerType.LocalAI ?
							StringResources.LOCALAI_PATH_EMPTY :
							StringResources.ID_EMPTY;
					}
					NotifyPropertyChanged();
				}
			}
		}


		private string _LogContent;
		public string LogContent
		{
			get
			{
				return _LogContent;
			}
			set
			{
				if (value != _LogContent)
				{
					_LogContent = value;
					NotifyPropertyChanged();
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
					NotifyPropertyChanged();
				}
			}
		}

		private string _ValidationString = StringResources.LOCALAI_PATH_EMPTY;
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
					NotifyPropertyChanged();
				}
			}
		}

		public string Error => "";

		public string this[string columnName]
		{
			get
			{
				if (columnName == "ID" && !IsValid)
					return ValidationString;
				return null;
			}
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public event PropertyChangedEventHandler PropertyChanged;
	}
	
	public partial class MatchConfiguration : ObservableCollection<PlayerConfiguration>, IValidationBubbling
	{
		public MatchConfiguration() : base() { }

		private Game _Game;
		public Game Game
		{
			get
			{
				return _Game;
			}
			set
			{
				if (value != _Game)
				{
					if (value == null)
					{
						Clear();
					}
					else
					{
						while (value.PlayerCount > Count)
							Add(new PlayerConfiguration { SlotID = Count });
						while (value.PlayerCount < Count)
							RemoveAt(Count - 1);
					}

					_Game = value;
					OnPropertyChanged(new PropertyChangedEventArgs("Game"));
					PlayerConfigurationPropertyChanged(null, null);
				}
			}
		}

		private bool _IsLocalMatch;
		public bool IsLocalMatch
		{
			get
			{
				return _IsLocalMatch;
			}
			set
			{
				if (value != _IsLocalMatch)
				{
					_IsLocalMatch = value;
					OnPropertyChanged(new PropertyChangedEventArgs("IsLocalMatch"));
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
					OnPropertyChanged(new PropertyChangedEventArgs("IsValid"));
				}
			}
		}


		private string _ValidationString = StringResources.CHOOSE_GAME_FIRST;

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
					OnPropertyChanged(new PropertyChangedEventArgs("ValidationString"));
				}
			}
		}

		public dynamic Initdata { get; set; } = "";

		public event EventHandler ValidationChanged;

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

		private void ValidationFailDueTo(string message)
		{
			IsValid = false;
			ValidationString = message;
			ValidationChanged?.Invoke(this, null);
		}

		public void PlayerConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			int botzoneAICount = 0, localAICount = 0, humanCount = 0;
			if (Game == null || Game.PlayerCount == 0)
			{
				ValidationFailDueTo(StringResources.CHOOSE_GAME_FIRST);
				return;
			}

			IsLocalMatch = this.All(x => x.Type != PlayerType.BotzoneBot);
			foreach (var config in this)
			{
				if (!config.IsValid)
				{
					ValidationFailDueTo(config.ValidationString);
					return;
				}

				if (config.Type == PlayerType.BotzoneBot)
					botzoneAICount++;
				else if (config.Type == PlayerType.LocalAI)
					localAICount++;
				else if (config.Type == PlayerType.LocalHuman)
					humanCount++;
			}
			if (humanCount > 1)
			{
				ValidationFailDueTo(StringResources.TOO_MANY_HUMAN);
				return;
			}
			if (!IsLocalMatch && humanCount > 0)
			{
				ValidationFailDueTo(StringResources.BOTZONE_MATCH_NO_HUMAN);
				return;
			}
			if (!IsLocalMatch && localAICount != 1)
			{
				ValidationFailDueTo(StringResources.BOTZONE_MATCH_ONE_LOCALAI);
				return;
			}
			IsValid = true;
			ValidationChanged?.Invoke(this, null);
		}

		internal async Task<Match> CreateMatch()
		{
			if (!IsLocalMatch)
			{
				var matchID = await this.RequestMatch();
				return new BotzoneMatch(this, matchID);
			}
			return new LocalMatch(this);
		}
	}

}
