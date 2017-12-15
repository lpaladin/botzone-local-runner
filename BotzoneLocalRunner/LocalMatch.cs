using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;

namespace BotzoneLocalRunner
{
	class LocalMatch : Match
	{
		public LocalProgramRunner[] Runners { get; }
		public IWebBrowser Browser { get; }
		private TaskCompletionSource<string> JudgeTask { get; set; }

		#region 开放给浏览器的回调方法
		internal void JudgeReady() => JudgeTask.SetResult("");
		internal void JudgeResponse(string str) => JudgeTask.SetResult(str);
		internal void JudgeFail(string str) => JudgeTask.SetException(new Exception(str));
		#endregion

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
		internal void EmitEvent(string eventName, dynamic data) =>
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
		internal Task<JavascriptResponse> TransformRequestToSimpleIO(dynamic request) =>
			Browser.GetMainFrame().EvaluateScriptAsync(
				$"transformDetail.req2simple({JsonConvert.SerializeObject(request)});"
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

		public override void OnFinish(bool aborted)
		{
			base.OnFinish(aborted);
			if (aborted)
			{
				SetStatus("aborted");
				EmitEvent("match.end");
			}
			else
			{
				SetStatus("finished");
				EmitEvent("match.end", Scores);
			}
		}

		public override async Task RunMatch()
		{
			Status = MatchStatus.Waiting;
			JudgeTask = new TaskCompletionSource<string>();
			Browser.RegisterJsObject("cSharpNotifier", this);
			await JudgeTask.Task;

			SetStatus("waiting");
			Status = MatchStatus.Running;

			// 开始对局！
			while (true)
			{
				JudgeTask = new TaskCompletionSource<string>();

				// Judge 请求处理
				var judgeItem = new JudgeLogItem();
				SendToJudge();
				Logs.Add(judgeItem);
				try
				{
					var judgeRaw = await JudgeTask.Task;
					var output = JsonConvert.DeserializeObject<JudgeOutput>(judgeRaw);
					judgeItem.output = output;
					judgeItem.verdict = "OK";
					if (Logs.Count == 0 && output.initdata?.Length > 0)
						Initdata = output.initdata;
				}
				catch (Exception ex)
				{
					judgeItem.response = ex.Message;
					judgeItem.verdict = "RE";
					OnFinish(true);
					return;
				}

				// Judge 返回处理
				EmitEvent("match.newlog", judgeItem.output.display ?? "");
				if (judgeItem.output.command == "finish")
				{
					// 判定游戏结束
					foreach (var pair in judgeItem.output.content)
						Scores[int.Parse(pair.Key)] = double.Parse(pair.Value);
					OnFinish(false);
					return;
				}
				
				// 玩家请求与返回处理
				foreach (var pair in judgeItem.output.content)
				{
					int id = int.Parse(pair.Key);
				}
			}
		}
	}
}
