using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static BotzoneLocalRunner.Util;

#pragma warning disable 0649

namespace BotzoneLocalRunner
{
	internal static class BotzoneProtocol
	{
		internal struct BotzoneCredentials
		{
			internal string UserID { get; set; }
			internal string Secret { get; set; }
			internal string BotzoneCopiedURL
			{
				set
				{
					var m = Regex.Matches(value, @"/([0-9a-f]+)/([^/]+)/localai");
					UserID = m[0].Value;
					Secret = m[1].Value;
				}
			}

			internal string BotzoneLocalAIURL() => $"{UserID}/{Secret}/localai";
			internal string BotzoneRunMatchURL() => $"{UserID}/{Secret}/runmatch";
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
