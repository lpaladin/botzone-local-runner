using CefSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BotzoneLocalRunner
{
	[Serializable]
	public class JudgeOutput
	{
		public string command;
		public dynamic display;
		public Dictionary<string, dynamic> content;
		public dynamic initdata;
	}

	[Serializable]
	public class ProgramLogItem
	{
		public int time;
		public int memory;
		public string verdict;
		public string raw;
		public string debug;
		public JudgeOutput output;
		public dynamic response;
	}

	[Serializable]
	public class BotLogItem : Dictionary<string, ProgramLogItem>, ILogItem { }

	[Serializable]
	public class JudgeLogItem : ProgramLogItem, ILogItem { }

	public interface ILogItem { }

	public enum MatchStatus
	{
		Waiting,
		Running,
		Finished,
		Aborted
	}

	[Serializable]
	public partial class MatchConfiguration : ISerializable
	{
		/// <summary>
		/// 用于简化带有很多ViewModel相关域从而难以序列化的PlayerConfiguration类
		/// </summary>
		[Serializable]
		public class CompactPlayerConfiguration
		{
			public PlayerType Type;
			public string ID;
			public string LogContent;
		}

		/// <summary>
		/// 反序列化构造函数
		/// </summary>
		protected MatchConfiguration(SerializationInfo info, StreamingContext context)
		{
			var conf = info.GetValue("Configuration",
				typeof(CompactPlayerConfiguration[])) as CompactPlayerConfiguration[];
			Game = new Game
			{
				Name = info.GetString("Game"),
				PlayerCount = conf.Length
			};
			for (int i = 0; i < conf.Length; i++)
			{
				this[i].Type = conf[i].Type;
				this[i].ID = conf[i].ID;
				this[i].LogContent = conf[i].LogContent;
			}
		}

		/// <summary>
		/// 序列化函数
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Game", Game.Name);
			info.AddValue("Configuration", (from player in this
											select new CompactPlayerConfiguration
											{
												Type = player.Type,
												ID = player.ID,
												LogContent = player.LogContent
											}).ToArray());
		}
	}

	[Serializable]
	public abstract class Match
	{
		public static Match ActiveMatch;

		public MatchConfiguration Configuration { get; set; }
		public DateTime BeginTime { get; set; }
		public DateTime EndTime { get; set; }
		public dynamic Initdata { get; set; } = "";
		public double[] Scores { get; set; }
		public MatchStatus Status { get; set; } = MatchStatus.Waiting;

		[NonSerialized]
		public List<dynamic> DisplayLogs;
		[NonSerialized]
		public List<ILogItem> Logs;

		private string SerializedDisplayLogs;
		private string SerializedLogs;

		protected Match(MatchConfiguration conf)
		{
			Configuration = ObjectCopier.Clone(conf);
			Initdata = conf.Initdata;
			BeginTime = DateTime.Now;
			if (ActiveMatch != null)
				throw new Exception(StringResources.NO_PARALLEL_MATCHES);
			ActiveMatch = this;
		}

		public abstract Task RunMatch();

		[OnSerializing]
		private void SerializeJSONDynamics(StreamingContext context)
		{
			SerializedDisplayLogs = JsonConvert.SerializeObject(DisplayLogs);
			SerializedLogs = JsonConvert.SerializeObject(Logs);
		}

		[OnDeserialized]
		private void DeserializeJSONDynamics(StreamingContext context)
		{
			DisplayLogs = JsonConvert.DeserializeObject<List<dynamic>>(SerializedDisplayLogs);
			Logs = JsonConvert.DeserializeObject<List<ILogItem>>(SerializedLogs, BotzoneProtocol.logConverter);
		}

		public virtual Task OnFinish(bool aborted)
		{
			EndTime = DateTime.Now;
			if (aborted)
				Status = MatchStatus.Aborted;
			else
				Status = MatchStatus.Finished;
			ActiveMatch = null;
			return Task.CompletedTask;
		}

		public abstract void ReplayMatch(IWebBrowser Browser);
		public virtual Task AbortMatch()
		{
			return OnFinish(true);
		}
	}

	[Serializable]
	public class BotzoneMatch : Match
	{
		public int MySlot { get; }
		public string MatchID { get; }

		[NonSerialized]
		public readonly PlayerConfiguration MyConf;

		[NonSerialized]
		public readonly LocalProgramRunner Runner;

		public BotzoneMatch(MatchConfiguration conf, string matchID) : base(conf)
		{
			MatchID = matchID;
			for (int i = 0; i < conf.Count; i++)
				if (conf[i].Type != PlayerType.BotzoneBot)
					MySlot = i;
			MyConf = Configuration[MySlot];
			Runner = new LocalProgramRunner
			{
				ProgramPath = conf[MySlot].ID
			};
		}

		public override async Task OnFinish(bool aborted)
		{
			await base.OnFinish(aborted);
			await this.FetchFullLogs();
			ActiveMatch = null;
		}

		public override async Task RunMatch()
		{
			MyConf.LogContent = "";
			while (true)
			{
				while (!await this.FetchNextMatchRequest()) ;
				if (Status == MatchStatus.Finished || Status == MatchStatus.Aborted)
					break;
				MyConf.LogContent += (">>> REQUEST" +
					Environment.NewLine + Runner.Requests.Last() + Environment.NewLine);
				Util.Logger.Log(LogLevel.Info, $"本地AI收到的 request：{Runner.Requests.Last()}");
				await Runner.RunForResponse();
				MyConf.LogContent += ("<<< RESPONSE" +
					Environment.NewLine + Runner.Responses.Last() + Environment.NewLine);
				Util.Logger.Log(LogLevel.Info, $"本地AI给出的 response：{Runner.Responses.Last()}");
			}
		}

		public override void ReplayMatch(IWebBrowser Browser)
		{
			Browser.Load(BotzoneProtocol.Credentials.BotzoneMatchURL(MatchID, false));
		}

		public override async Task AbortMatch()
		{
			if (Runner.CurrentProcess != null)
				Runner.CurrentProcess.Kill();
			await BotzoneProtocol.AbortMatch(MatchID);
			await base.AbortMatch();
		}
	}
}
