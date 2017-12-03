using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static BotzoneLocalRunner.Util;

#pragma warning disable 0649

namespace BotzoneLocalRunner
{
	internal class BotzoneCredentials : INotifyPropertyChanged, IDataErrorInfo, IValidationBubbling
	{
		internal string UserID { get; set; }
		internal string Secret { get; set; }

		private string _BotzoneCopiedURL = "<在此粘贴Botzone本地AI的URL>";
		public string BotzoneCopiedURL
		{
			get => _BotzoneCopiedURL;
			set
			{
				if (value != _BotzoneCopiedURL)
				{
					_BotzoneCopiedURL = value;
					var m = Regex.Matches(BotzoneCopiedURL, StringResources.BOTZONE_LOCALAI_URL_REGEX);
					if (m.Count != 1)
					{
						IsValid = false;
						ValidationString = StringResources.BAD_LOCALAI_URL;
						ValidationChanged(this, null);
					}
					else
					{
						UserID = m[0].Groups[1].Value;
						Secret = m[0].Groups[2].Value;
						IsValid = true;
						ValidationChanged(this, null);
					}

					NotifyPropertyChanged("BotzoneCopiedURL");
				}
			}
		}

		internal string BotzoneLocalAIURL() => $"{UserID}/{Secret}/localai";
		internal string BotzoneRunMatchURL() => $"{UserID}/{Secret}/runmatch";

		private bool _IsValid = false;
		public bool IsValid
		{
			get => _IsValid;
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
			get => _ValidationString;
			set
			{
				if (value != _ValidationString)
				{
					_ValidationString = value;
					NotifyPropertyChanged("ValidationString");
				}
			}
		}

		public string Error => "";

		public string this[string columnName]
		{
			get
			{
				if (columnName == "BotzoneCopiedURL" && !IsValid)
					return ValidationString;

				return null;
			}
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public event PropertyChangedEventHandler PropertyChanged;
		public event EventHandler ValidationChanged;
	}

	internal static class BotzoneProtocol
	{
		static BotzoneCredentials Credentials;

		static readonly string BotzoneGameURL = "public/games";

		static HttpClient client = new HttpClient
		{
			BaseAddress = new Uri(StringResources.BOTZONE_API_BASE)
		};

		internal static async Task<IEnumerable<Game>> GetGames()
		{
			JArray raw = null;
			do
			{
				Logger.Log(LogLevel.Info, "尝试从 Botzone 读取游戏列表……");
				try
				{
					var res = await client.GetAsync(BotzoneGameURL);
					var str = await res.Content.ReadAsStringAsync();
					raw = JArray.Parse(str);
				}
				catch (Exception ex)
				{
					Logger.Log(LogLevel.Warning, "请求过程中发生错误：" + ex.Message);
					Logger.Log(LogLevel.Info, "5秒后重试……");
					await Task.Delay(5000);
				}
			} while (raw == null);

			Logger.Log(LogLevel.OK, "游戏列表加载成功！");
			return from game in raw.Children()
				   select new Game
				   {
					   Name = (string)game["name"],
					   PlayerCount = (int)game["min_player_num"],
					   Description = (string)game["desc"]
				   };
		}
	}
}
