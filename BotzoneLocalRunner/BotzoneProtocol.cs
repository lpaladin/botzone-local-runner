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

		internal string BotzoneDomain { get; set; } = "https://www.botzone.org";

		private string _BotzoneCopiedURL = "<在此粘贴Botzone本地AI的URL>";
		public string BotzoneCopiedURL
		{
			get
			{
				return _BotzoneCopiedURL;
			}
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
						ValidationChanged?.Invoke(this, null);
					}
					else
					{
						BotzoneDomain = m[0].Groups[1].Value;
						UserID = m[0].Groups[3].Value;
						Secret = m[0].Groups[4].Value;
						IsValid = true;
						Properties.Settings.Default.LastBotzoneLocalAIURL = value;
						ValidationChanged?.Invoke(this, null);
					}

					NotifyPropertyChanged();
				}
			}
		}

		internal string BotzoneLocalAIURL() =>
			$"{BotzoneDomain}/api/{UserID}/{Secret}/localai";
		internal string BotzoneRunMatchURL() =>
			$"{BotzoneDomain}/api/{UserID}/{Secret}/runmatch";
		internal string BotzoneAbortMatchURL() =>
			$"{BotzoneDomain}/api/{UserID}/{Secret}/abortmatch";
		internal string BotzoneGamesURL() =>
			$"{BotzoneDomain}/api/public/games";
		internal string BotzoneMatchURL(string matchid, bool lite) =>
			$"{BotzoneDomain}/match/{matchid}" + (lite ? "?lite=true" : "");
		internal string BotzoneLocalMatchURL(string gamename) =>
			$"{BotzoneDomain}/localmatch/{gamename}";

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
					NotifyPropertyChanged();
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

	internal class MatchLogConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return (objectType == typeof(List<ILogItem>));
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			JArray arr;
			if (reader.TokenType == JsonToken.StartObject)
				arr = (JArray)JObject.Load(reader)["logs"];
			else
				arr = JArray.Load(reader);

			List<ILogItem> logs = new List<ILogItem>(arr.Count);
			int i, n = arr.Count;
			for (i = 0; i < n; i++)
			{
				if (i % 2 == 0)
					// JudgeLog
					logs.Add(arr[i].ToObject<JudgeLogItem>());
				else
					// BotLog
					logs.Add(arr[i].ToObject<BotLogItem>());
			}
			return logs;
		}
		
		public override bool CanWrite
		{
			get { return false; }
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}

	internal static class BotzoneProtocol
	{
		internal static BotzoneCredentials Credentials { get; set; } = new BotzoneCredentials();

		private static IWebBrowser _CurrentBrowser;
		internal static IWebBrowser CurrentBrowser
		{
			get
			{
				return _CurrentBrowser;
			}
			set
			{
				if (value != _CurrentBrowser)
				{
					_CurrentBrowser = value;
					_CurrentBrowser.RequestHandler = new BotzoneCefRequestHandler();
					BrowserJSObject.Init();
				}
			}
		}

		static HttpClient client = new HttpClient();
		internal static MatchLogConverter logConverter = new MatchLogConverter();

		/// <summary>
		/// 检查请求的返回情况并记录
		/// </summary>
		/// <param name="res">请求的返回体</param>
		/// <param name="message">请求返回的消息内容</param>
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

		internal static async Task<bool> FetchNextMatchRequest(this BotzoneMatch match)
		{
			string raw;
			do
			{
				Logger.Log(LogLevel.InfoTip, "连接 Botzone，并等待新 request");
				var req = new HttpRequestMessage(HttpMethod.Get, Credentials.BotzoneLocalAIURL());
				if (match.Runner.Responses.Count > 0)
				{
					var last = match.Runner.Responses.Last();
					if (LocalProgramRunner.IsSimpleIO)
						req.Headers.Add("X-Match-" + match.MatchID, last);
					else
						req.Headers.Add("X-Match-" + match.MatchID, JsonConvert.SerializeObject(last, Formatting.None));
				}
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
			// Debug.Assert(reqCount + finishCount <= 1);
			for (i = 1, j = 0; j < reqCount; i += 2, j++)
			{
				if (lines[i] != match.MatchID)
					continue;
				string req = lines[i + 1];
				if (!LocalProgramRunner.IsSimpleIO)
					match.Runner.Requests.Add(JsonConvert.DeserializeObject(req));
				else
					match.Runner.Requests.Add(req);
				match.Status = MatchStatus.Running;
				Logger.Log(LogLevel.InfoTip, $"对局 {match.MatchID} 获得一条新 request");
				return true;
			}
			for (i = 2 * reqCount + 1, j = 0; j < finishCount; i++, j++)
			{
				var parts = lines[i].Split(' ');
				if (parts[0] != match.MatchID)
					continue;
				Debug.Assert(parts[1] == match.MySlot.ToString());
				if (parts[2] == "0")
				{
					Logger.Log(LogLevel.Warning, $"对局 {match.MatchID} 中止");
					await match.OnFinish(true);
				}
				else
				{
					match.Scores = parts.Skip(3).Select(double.Parse).ToArray();
					Logger.Log(LogLevel.OK, $"对局 {match.MatchID} 结束，比分为 {string.Join(", ", parts.Skip(3))}，本地AI分数 {parts[3 + match.MySlot]}");
					await match.OnFinish(false);
				}
				return true;
			}
			return false;
		}

		internal static async Task<string> RequestMatch(this MatchConfiguration conf)
		{
			if (!conf.IsValid)
				return null;
			string matchID = null;
			do
			{
				Logger.Log(LogLevel.Info, "尝试向 Botzone 发起对局请求……");
				var req = new HttpRequestMessage(HttpMethod.Get, Credentials.BotzoneRunMatchURL());
				req.Headers.Add("X-Game", conf.Game.Name);
				req.Headers.Add("X-Initdata", conf.Initdata is string ? conf.Initdata : JsonConvert.SerializeObject(conf.Initdata));
				req.Headers.Add("X-UseSimpleIO", LocalProgramRunner.IsSimpleIO ? "true" : "false");
				req.Headers.Add("X-Timelimit", Properties.Settings.Default.TimeLimit.TotalSeconds.ToString());
				for (int i = 0; i < conf.Count; i++)
					req.Headers.Add("X-Player-" + i, conf[i].Type == PlayerType.BotzoneBot ? conf[i].ID : "me");
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

		internal static async Task AbortMatch(string matchID)
		{
			do
			{
				Logger.Log(LogLevel.Info, "尝试向 Botzone 发起中止对局请求……");
				var req = new HttpRequestMessage(HttpMethod.Get, Credentials.BotzoneAbortMatchURL());
				req.Headers.Add("X-Matchid", matchID);
				var res = await client.SendAsync(req);
				if (CheckResponse(res, await res.Content.ReadAsStringAsync()))
					break;

				Logger.Log(LogLevel.InfoTip, "5秒后重试……");
				await Task.Delay(5000);
			} while (true);

			Logger.Log(LogLevel.Warning, "成功中止了对局：" + matchID);
		}

		internal static async Task FetchFullLogs(this BotzoneMatch match)
		{
			do
			{
				Logger.Log(LogLevel.Info, "尝试从 Botzone 读取对局 Log……");
				try
				{
					var str = await client.GetStringAsync(Credentials.BotzoneMatchURL(match.MatchID, true));
					match.Logs = JsonConvert.DeserializeObject<List<ILogItem>>(str, logConverter);
					match.DisplayLogs = (from item in match.Logs.OfType<JudgeLogItem>()
										select item.output.display).ToList();
				}
				catch (Exception ex)
				{
					Logger.Log(LogLevel.Warning, "请求过程中发生错误：" + ex.Message);
					Logger.Log(LogLevel.InfoTip, "5秒后重试……");
					await Task.Delay(5000);
				}
			} while (match.Logs == null);

			Logger.Log(LogLevel.OK, "对局 Log 加载成功！");
		}

		internal static async Task<IEnumerable<Game>> GetGames()
		{
			JArray raw = null;
			do
			{
				Logger.Log(LogLevel.Info, "尝试从 Botzone 读取游戏列表……");
				try
				{
					var str = await client.GetStringAsync(Credentials.BotzoneGamesURL());
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
