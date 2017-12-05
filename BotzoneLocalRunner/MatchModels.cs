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
	internal class JudgeOutput
	{
		public string command;
		public dynamic display;
		public Dictionary<string, dynamic> content;
	}

	internal class ProgramLogItem
	{
		public int time;
		public int memory;
		public string verdict;
		public JudgeOutput output;
		public dynamic response;
	}

	internal class BotLogItem : Dictionary<string, ProgramLogItem>, ILogItem { }
	internal class JudgeLogItem : ProgramLogItem, ILogItem { }

	internal interface ILogItem { }

	internal enum MatchStatus
	{
		Waiting,
		Running,
		Finished,
		Aborted
	}

	internal class Match
	{
		public MatchConfiguration Configuration { get; set; }
		public List<dynamic> DisplayLogs { get; set; }
		public List<ILogItem> Logs { get; set; }
		public double[] Scores { get; set; }
		public MatchStatus Status { get; set; }

		public virtual void Finish(bool aborted)
		{
			if (aborted)
				Status = MatchStatus.Aborted;
			else
				Status = MatchStatus.Finished;
		}
	}

	internal partial class BotzoneMatch : Match
	{
		public static BotzoneMatch ActiveMatch;
		public int MySlot { get; }
		public string MatchID { get; }
		public PlayerConfiguration MyConf { get; }
		public LocalProgramRunner Runner { get; }

		public BotzoneMatch(MatchConfiguration conf, string matchID)
		{
			Configuration = conf;
			MatchID = matchID;
			for (int i = 0; i < conf.Count; i++)
				if (conf[i].Type != PlayerType.BotzoneBot)
					MySlot = i;
			MyConf = Configuration[MySlot];
			Runner = new LocalProgramRunner
			{
				ProgramPath = conf[MySlot].ID
			};
			if (ActiveMatch != null)
				throw new Exception("不应当有多个对局同时进行！");
			ActiveMatch = this;
		}

		public override void Finish(bool aborted)
		{
			base.Finish(aborted);
			ActiveMatch = null;
		}
	}
}
