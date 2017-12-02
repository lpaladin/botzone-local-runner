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
	internal static class BotzoneProtocol
	{
		internal class BotzoneCredentials : INotifyPropertyChanged, IDataErrorInfo
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
						NotifyPropertyChanged("BotzoneCopiedURL");
					}
				}
			}

			internal string BotzoneLocalAIURL() => $"{UserID}/{Secret}/localai";
			internal string BotzoneRunMatchURL() => $"{UserID}/{Secret}/runmatch";

			public string Error => throw new NotImplementedException();

			public string this[string columnName]
			{
				get
				{
					if (columnName == "BotzoneCopiedURL")
					{
						var m = Regex.Matches(BotzoneCopiedURL, @"/([0-9a-f]+)/([^/]+)/localai");
						if (m.Count != 2)
							return StringResources.BAD_LOCALAI_URL;
						UserID = m[0].Value;
						Secret = m[1].Value;
					}
					return null;
				}
			}

			private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") =>
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

			public event PropertyChangedEventHandler PropertyChanged;
		}

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
				Logger.Log("尝试从 Botzone 读取游戏列表……");
				try
				{
					var res = await client.GetAsync(BotzoneGameURL);
					var str = await res.Content.ReadAsStringAsync();
					raw = JArray.Parse(str);
				}
				catch (Exception ex)
				{
					Logger.Log("请求过程中发生错误：" + ex.Message);
					Logger.Log("5秒后重试……");
					await Task.Delay(5000);
				}
			} while (raw == null);

			Logger.Log("游戏列表加载成功！");
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
