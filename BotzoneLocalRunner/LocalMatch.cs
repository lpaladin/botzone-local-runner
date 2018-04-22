using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;
using static BotzoneLocalRunner.Util;
using System.Runtime.Serialization;

namespace BotzoneLocalRunner
{

	class BrowserJSObject
	{
		public TaskCompletionSource<string> JudgeTask { get; set; }
		public TaskCompletionSource<string> HumanTask { get; set; }

		#region 开放给浏览器的回调方法
		public void JudgeReady() => JudgeTask?.SetResult("");
		public void JudgeResponse(string str) => JudgeTask?.SetResult(str);
		public void JudgeFail(string str) => JudgeTask?.SetException(new Exception(str));
		public void HumanResponse(string str)
		{
			HumanTask?.SetResult(str);
			HumanTask = null;
		}
		#endregion

		public static BrowserJSObject Instance;

		public static void Init()
		{
			Instance = new BrowserJSObject();
			BotzoneProtocol.CurrentBrowser.RegisterJsObject("cSharpNotifier", Instance);
		}
	}

	[Serializable]
	public class LocalMatch : Match
	{
		[NonSerialized]
		public readonly LocalProgramRunner[] Runners;

		[NonSerialized]
		public readonly IWebBrowser Browser;

		#region 调用JS方法
		internal void SendToJudge() =>
			Browser.GetMainFrame().ExecuteJavaScriptAsync(
				$@"emulated_gio.sendToJudge({
					JsonConvert.SerializeObject(new
					{
						log = Logs,
						initdata = Initdata
					})
				});");
		internal void EmitEvent(string eventName, string data) =>
			Browser.GetMainFrame().ExecuteJavaScriptAsync(
				$"emulated_gio.emit('{eventName}', {JsonConvert.ToString(data)});"
			);
		internal void EmitEvent(string eventName, object data) =>
			Browser.GetMainFrame().ExecuteJavaScriptAsync(
				$"emulated_gio.emit('{eventName}', {JsonConvert.SerializeObject(data)});"
			);
		internal void EmitEvent(string eventName) =>
			Browser.GetMainFrame().ExecuteJavaScriptAsync(
				$"emulated_gio.emit('{eventName}');"
			);
		internal void AddFullLogItem(ILogItem item) =>
			Browser.GetMainFrame().ExecuteJavaScriptAsync(
				$"emulated_gio.loglist.push({JsonConvert.SerializeObject(item)});"
			);
		internal void SetStatus(string status) =>
			Browser.GetMainFrame().ExecuteJavaScriptAsync(
				$"emulated_gio.status = '{status}';"
			);
		internal void SetIsSimpleIO(int playerID, bool to) =>
			Browser.GetMainFrame().ExecuteJavaScriptAsync(
				$"transformDetail.simpleio[{playerID}] = {to};"
			);
		internal Task<JavascriptResponse> TransformRequestToSimpleIO(object request) =>
			Browser.GetMainFrame().EvaluateScriptAsync(
				$"transformDetail.req2simple({JsonConvert.SerializeObject(request)});"
			);
		internal Task<JavascriptResponse> TransformSimpleIOToResponse(string response) =>
			Browser.GetMainFrame().EvaluateScriptAsync(
				$"transformDetail.simple2res({JsonConvert.ToString(response)});"
			);
		#endregion

		public LocalMatch(MatchConfiguration conf) : base(conf)
		{
			Browser = BotzoneProtocol.CurrentBrowser;
			Runners = conf.Select(x =>
			{
				var runner = new LocalProgramRunner();
				if (x.Type == PlayerType.LocalAI)
					runner.ProgramPath = x.ID;
				return runner;
			}).ToArray();
			Scores = new double[conf.Count];
			DisplayLogs = new List<dynamic>();
			Logs = new List<ILogItem>();
		}

		public override async Task OnFinish(bool aborted)
		{
			await base.OnFinish(aborted);
			if (aborted)
			{
				SetStatus("aborted");
				Status = MatchStatus.Aborted;
				EmitEvent("match.end");
			}
			else
			{
				SetStatus("finished");
				Status = MatchStatus.Finished;
				EmitEvent("match.end", Scores);
			}
		}

		/// <summary>
		/// 实现了一个 Controller 逻辑
		/// </summary>
		/// <returns></returns>
		public override async Task RunMatch()
		{
			Logger.Log(LogLevel.Info, "正在从 Botzone 载入 Judge 程序...");
			Status = MatchStatus.Waiting;
			BrowserJSObject.Instance.JudgeTask = new TaskCompletionSource<string>();
			Browser.Load(BotzoneProtocol.Credentials.BotzoneLocalMatchURL(Configuration.Game.Name));

			for (int i = 0; i < Configuration.Count; i++)
				SetIsSimpleIO(i, Configuration[i].Type == PlayerType.LocalAI);

			await BrowserJSObject.Instance.JudgeTask.Task;

			SetStatus("waiting");
			Status = MatchStatus.Running;
			Logger.Log(LogLevel.OK, "Judge 程序加载成功，开始本地对局");
			foreach (var conf in Configuration)
				conf.LogContent = "";

			// 开始对局！
			while (true)
			{
				Logger.Log(LogLevel.Info, $"回合{Logs.Count / 2} - Judge 开始执行");
				BrowserJSObject.Instance.JudgeTask = new TaskCompletionSource<string>();

				// Judge 请求处理
				var judgeItem = new JudgeLogItem();
				SendToJudge();
				Logs.Add(judgeItem);
				try
				{
					var judgeRaw = await BrowserJSObject.Instance.JudgeTask.Task;
					var output = JsonConvert.DeserializeObject<JudgeOutput>(judgeRaw);
					judgeItem.output = output;
					judgeItem.verdict = "OK";
					if (Logs.Count == 0 && output.initdata?.Length > 0)
						Initdata = output.initdata;
					AddFullLogItem(judgeItem);
				}
				catch (Exception ex)
				{
					judgeItem.response = ex.Message;
					judgeItem.verdict = "RE";
					AddFullLogItem(judgeItem);
					await OnFinish(true);
					return;
				}

				// Judge 返回处理
				EmitEvent("match.newlog", judgeItem.output.display ?? "");
				DisplayLogs.Add(judgeItem.output.display);
				if (judgeItem.output.command == "finish")
				{
					// 判定游戏结束
					foreach (var pair in judgeItem.output.content)
						Scores[int.Parse(pair.Key)] = 
							pair.Value is string ? double.Parse(pair.Value) : pair.Value;
					Logger.Log(LogLevel.OK, $"Judge 判定游戏结束，比分：{String.Join(", ", Scores)}");
					await OnFinish(false);
					return;
				}

				// 玩家请求与返回处理
				var humanID = -1;
				foreach (var pair in judgeItem.output.content)
				{
					int id = int.Parse(pair.Key);
					if (Configuration[id].Type == PlayerType.LocalHuman)
					{
						humanID = id;
						Logger.Log(LogLevel.InfoTip, $"Judge 向{id}号玩家（人类）发起请求");

						// 人类玩家
						BrowserJSObject.Instance.HumanTask = new TaskCompletionSource<string>();
						EmitEvent("match.playerturn");
						EmitEvent("match.newrequest", pair.Value);
					}
				}

				var botItem = new BotLogItem();
				Logs.Add(botItem);
				foreach (var pair in judgeItem.output.content)
				{
					int id = int.Parse(pair.Key);
					var conf = Configuration[id];
					if (conf.Type != PlayerType.LocalHuman)
					{
						// 本地 AI
						var runner = Runners[id];
						conf.LogContent += ">>> REQUEST" + Environment.NewLine;
						if (LocalProgramRunner.IsSimpleIO)
						{
							JavascriptResponse req = await TransformRequestToSimpleIO(pair.Value);
							Debug.Assert(req.Success);
							runner.Requests.Add(req.Result);
							conf.LogContent += req.Result + Environment.NewLine;
							Logger.Log(LogLevel.InfoTip, $"Judge 向{id}号玩家（本地AI）发起请求：{req.Result}");
						}
						else
						{
							runner.Requests.Add(pair.Value);
							conf.LogContent += JsonConvert.SerializeObject(pair.Value) + Environment.NewLine;
							Logger.Log(LogLevel.InfoTip, $"Judge 向{id}号玩家（本地AI）发起请求：{pair.Value}");
						}
						ProgramLogItem resp = null;
						try
						{
							if (Status == MatchStatus.Aborted)
								return;
							resp = await runner.RunForResponse();
							if (LocalProgramRunner.IsSimpleIO)
							{
								JavascriptResponse req = await TransformSimpleIOToResponse(resp.raw);
								Debug.Assert(req.Success);
								resp.response = req.Result;
							}
							Logger.Log(LogLevel.OK, $"{id}号玩家（本地AI）给出了反馈：{resp.raw}");
						}
						catch (TimeoutException)
						{
							resp = new ProgramLogItem
							{
								verdict = "TLE"
							};
							Logger.Log(LogLevel.Warning, $"{id}号玩家（本地AI）超时了……");
						}
						catch (RuntimeException e)
						{
							resp = new ProgramLogItem
							{
								verdict = "RE",
								response = e.Message
							};
							Logger.Log(LogLevel.Warning, $"{id}号玩家（本地AI）崩溃了：{e.Message}");
						}
						catch (Exception e)
						{
							resp = new ProgramLogItem
							{
								verdict = "RE",
								response = e.Message
							};
							Logger.Log(LogLevel.No, $"{id}号玩家（本地AI）无法正常启动：{e.Message}");
						}
						finally
						{
							botItem.Add(pair.Key, resp);
							conf.LogContent += "<<< RESPONSE" + Environment.NewLine +
								(resp.raw ?? JsonConvert.SerializeObject(resp.response)) + Environment.NewLine;
						}
					}
				}
				if (humanID != -1)
				{
					var resp = new ProgramLogItem();
					var raw = await BrowserJSObject.Instance.HumanTask.Task;
					try
					{
						resp.response = JsonConvert.DeserializeObject(raw);
					}
					catch
					{
						resp.response = raw;
					}
					botItem.Add(humanID.ToString(), resp);
					Logger.Log(LogLevel.OK, $"{humanID}号玩家（人类）给出了反馈");
				}
				AddFullLogItem(botItem);
			}
		}

		public override void ReplayMatch(IWebBrowser Browser)
		{
			// 将对局 log 插入网页中
			BotzoneCefRequestHandler.MatchInjectFilter = new CefSharp.Filters.FindReplaceResponseFilter(
				"<!-- INJECT_FINISHED_MATCH_LOGS_HERE -->",
				$@"
<script>
	live = false;
	initdata = {JsonConvert.ToString(Initdata)};
	loglist = {JsonConvert.SerializeObject(Logs)};
</script>
");
			Browser.Load(BotzoneProtocol.Credentials.BotzoneLocalMatchURL(Configuration.Game.Name));
		}

		public override Task AbortMatch()
		{
			foreach (var runner in Runners)
				if (runner.CurrentProcess != null)
					runner.CurrentProcess.Kill();
			return base.AbortMatch();
		}
	}
}
