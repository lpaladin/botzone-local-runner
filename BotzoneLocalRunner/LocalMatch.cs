using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotzoneLocalRunner
{
	class LocalMatch : Match
	{
		public List<LocalProgramRunner> Runners { get; }
		public IWebBrowser Browser { get; }

		public LocalMatch(MatchConfiguration conf) : base(conf)
		{
			Browser = BotzoneProtocol.CurrentBrowser;
			Runners = conf.Select(x =>
			{
				var runner = new LocalProgramRunner();
				if (x.Type == PlayerType.LocalAI)
					runner.ProgramPath = x.ID;
				return runner;
			}).ToList();
		}

		public override Task RunMatch()
		{
			Status = MatchStatus.Running;
			Browser.FrameLoadEnd += Browser_FrameLoadEnd;
			throw new NotImplementedException();
		}

		private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
		{
			throw new NotImplementedException();
			Browser.FrameLoadEnd -= Browser_FrameLoadEnd;
		}
	}
}
