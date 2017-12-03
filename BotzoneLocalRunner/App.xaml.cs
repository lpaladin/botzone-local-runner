using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CefSharp;

namespace BotzoneLocalRunner
{
	/// <summary>
	/// App.xaml 的交互逻辑
	/// </summary>
	public partial class App : Application
	{
		private App()
		{
			Cef.EnableHighDPISupport();
			Cef.Initialize(new CefSettings
			{
				Locale = CultureInfo.CurrentCulture.Name
			});
		}
	}
}
