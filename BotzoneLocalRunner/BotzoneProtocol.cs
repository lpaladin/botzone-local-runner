using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static BotzoneLocalRunner.Util;
using CefSharp;
using System.Diagnostics;

#pragma warning disable 0649

namespace BotzoneLocalRunner
{
	public class BotzoneCredentials : INotifyPropertyChanged, IDataErrorInfo, IValidationBubbling
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
						Properties.Settings.Default.LastBotzoneLocalAIURL = value;
						Properties.Settings.Default.Save();
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

	internal class BotzoneRequestFailException : Exception
	{
		internal BotzoneRequestFailException(int statusCode, string message) : base($"[HTTP {statusCode}] {message}") { }
	}

	internal static class BotzoneProtocol
	{
		internal static BotzoneCredentials Credentials { get; set; }
		internal static IWebBrowser CurrentBrowser { get; set; }

		static HttpClient client = new HttpClient
		{
			BaseAddress = new Uri(Properties.Settings.Default.BotzoneAPIBase)
		};

		/// <summary>
		/// 检查请求的返回情况并记录
		/// </summary>
		/// <param name="res">请求的返回消息</param>
		/// <returns>是否成功</returns>
		/// <exception cref="BotzoneRequestFailException"></exception>
		private static bool CheckResponse(HttpResponseMessage res, string message)
		{
			switch (res.StatusCode)
			{
				case HttpStatusCode.OK:
					return true;
				case HttpStatusCode.BadGateway:
					Logger.Log(LogLevel.Warning, "到 Botzone 的连接异常，可能要稍后再试");
					return false;
				case HttpStatusCode.GatewayTimeout:
					Logger.Log(LogLevel.Warning, "到 Botzone 的连接不畅，可能要稍后再试");
					return false;
				case HttpStatusCode.InternalServerError:
					Logger.Log(LogLevel.Block, "Botzone 出现了技术性问题，可能要稍后再试");
					break;
				case HttpStatusCode.ServiceUnavailable:
					Logger.Log(LogLevel.Block, "Botzone 正在维护，请稍后再试");
					break;
				case HttpStatusCode.Unauthorized:
					if (message.Contains("secret"))
						Logger.Log(LogLevel.Error, "用户不存在或密钥错误！");
					else if (message.Contains("level"))
						Logger.Log(LogLevel.Error, "用户等级不够，无法使用该工具。");
					else
						Logger.Log(LogLevel.Error, message);
					break;
				case HttpStatusCode.BadRequest:
					Logger.Log(LogLevel.Error, message);
					break;
				default:
					Logger.Log(LogLevel.Block, "请求发生错误：" + message);
					return false;
			}
			throw new BotzoneRequestFailException((int)res.StatusCode, message);
		}

		internal static async Task FetchNextMatchRequest(this BotzoneMatch match)
		{
			string raw;
			do
			{
				Logger.Log(LogLevel.InfoTip, "连接 Botzone，并等待新 request");
				var req = new HttpRequestMessage(HttpMethod.Get, Credentials.BotzoneLocalAIURL());
				req.Headers.Add("X-Match-" + match.MatchID, match.Runner.Responses.Last());
				var res = await client.SendAsync(req);
				raw = await res.Content.ReadAsStringAsync();
				if (CheckResponse(res, raw))
					break;

				Logger.Log(LogLevel.InfoTip, "5秒后重试……");
				await Task.Delay(5000);
			} while (true);

			var lines = raw.Split('\n');
			var counts = lines[0].Split(' ');
			int reqCount = int.Parse(counts[0]), finishCount = int.Parse(counts[1]), i, j;
			Debug.Assert(reqCount + finishCount <= 1);
			for (i = 1, j = 0; j < reqCount; i += 2, j++)
			{
				Debug.Assert(lines[i] == match.MatchID);
				match.Runner.Requests.Add(lines[i + 1]);
				match.Status = MatchStatus.Running;
				Logger.Log(LogLevel.InfoTip, $"对局 {match.MatchID} 获得一条新 request");
				return;
			}
			for (i = 2 * reqCount + 1, j = 0; j < finishCount; i++, j++)
			{
				var parts = lines[i].Split(' ');
				Debug.Assert(parts[0] == match.MatchID);
				Debug.Assert(parts[1] == match.MySlot.ToString());
				if (parts[2] == "0")
				{
					Logger.Log(LogLevel.Warning, $"对局 {match.MatchID} 中止");
					match.Finish(true);
				}
				else
				{
					match.Scores = parts.Skip(3).Select(x => double.Parse(x)).ToArray();
					Logger.Log(LogLevel.OK, $"对局 {match.MatchID} 结束，比分为 {parts.Skip(3)}，本地AI分数 {parts[3 + match.MySlot]}");
					match.Finish(false);
				}
				return;
			}
		}

		internal static async Task<string> RequestMatch(MatchConfiguration conf)
		{
			if (!conf.IsValid)
				return null;
			string matchID = null;
			do
			{
				Logger.Log(LogLevel.Info, "尝试向 Botzone 发起对局请求……");
				var req = new HttpRequestMessage(HttpMethod.Get, Credentials.BotzoneRunMatchURL());
				req.Headers.Add("X-Game", conf.Game.Name);
				for (int i = 0; i < conf.Count; i++)
					req.Headers.Add("X-Player-" + i, conf[i].Type != PlayerType.BotzoneBot ? conf[i].ID : "me");
				var res = await client.SendAsync(req);
				matchID = await res.Content.ReadAsStringAsync();
				if (CheckResponse(res, matchID))
					break;

				Logger.Log(LogLevel.InfoTip, "5秒后重试……");
				await Task.Delay(5000);
			} while (true);

			Logger.Log(LogLevel.OK, "成功创建了新对局：" + matchID);
			return matchID;
		}

		internal static async Task<IEnumerable<Game>> GetGames()
		{
			JArray raw = null;
			do
			{
				Logger.Log(LogLevel.Info, "尝试从 Botzone 读取游戏列表……");
				try
				{
					var res = await client.GetAsync(Properties.Settings.Default.BotzoneGamesPath);
					var str = await res.Content.ReadAsStringAsync();
					raw = JArray.Parse(str);
				}
				catch (Exception ex)
				{
					Logger.Log(LogLevel.Warning, "请求过程中发生错误：" + ex.Message);
					Logger.Log(LogLevel.InfoTip, "5秒后重试……");
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
