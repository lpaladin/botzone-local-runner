// Copyright © 2010-2016 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using CefSharp.Filters;
using CefSharp;
using System.Security.Cryptography.X509Certificates;

namespace BotzoneLocalRunner
{
	public class BotzoneCefRequestHandler : IRequestHandler
	{
		public static FindReplaceResponseFilter MatchInjectFilter;
		public static readonly string VersionNumberString = String.Format("Chromium: {0}, CEF: {1}, CefSharp: {2}",
			Cef.ChromiumVersion, Cef.CefVersion, Cef.CefSharpVersion);

		bool IRequestHandler.OnBeforeBrowse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, bool isRedirect)
		{
			return false;
		}

		bool IRequestHandler.OnOpenUrlFromTab(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
		{
			return false;
		}

		bool IRequestHandler.OnCertificateError(IWebBrowser browserControl, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback)
		{
			//NOTE: If you do not wish to implement this method returning false is the default behaviour
			// We also suggest you explicitly Dispose of the callback as it wraps an unmanaged resource.
			//callback.Dispose();
			//return false;

			//NOTE: When executing the callback in an async fashion need to check to see if it's disposed
			if (!callback.IsDisposed)
			{
				using (callback)
				{
					//To allow certificate
					callback.Continue(true);
					return true;
				}
			}

			return false;
		}

		void IRequestHandler.OnPluginCrashed(IWebBrowser browserControl, IBrowser browser, string pluginPath)
		{
			// TODO: Add your own code here for handling scenarios where a plugin crashed, for one reason or another.
		}

		CefReturnValue IRequestHandler.OnBeforeResourceLoad(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
		{
			//Example of how to set Referer
			// Same should work when setting any header

			// For this example only set Referer when using our custom scheme

			//Example of setting User-Agent in every request.
			//var headers = request.Headers;

			//var userAgent = headers["User-Agent"];
			//headers["User-Agent"] = userAgent + " CefSharp";

			//request.Headers = headers;

			//NOTE: If you do not wish to implement this method returning false is the default behaviour
			// We also suggest you explicitly Dispose of the callback as it wraps an unmanaged resource.
			//callback.Dispose();
			//return false;

			//NOTE: When executing the callback in an async fashion need to check to see if it's disposed
			callback.Dispose();

			return CefReturnValue.Continue;
		}

		bool IRequestHandler.GetAuthCredentials(IWebBrowser browserControl, IBrowser browser, IFrame frame, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
		{
			//NOTE: If you do not wish to implement this method returning false is the default behaviour
			// We also suggest you explicitly Dispose of the callback as it wraps an unmanaged resource.

			callback.Dispose();
			return false;
		}

		void IRequestHandler.OnRenderProcessTerminated(IWebBrowser browserControl, IBrowser browser, CefTerminationStatus status)
		{
			// TODO: Add your own code here for handling scenarios where the Render Process terminated for one reason or another.
		}

		bool IRequestHandler.OnQuotaRequest(IWebBrowser browserControl, IBrowser browser, string originUrl, long newSize, IRequestCallback callback)
		{
			//NOTE: If you do not wish to implement this method returning false is the default behaviour
			// We also suggest you explicitly Dispose of the callback as it wraps an unmanaged resource.
			//callback.Dispose();
			//return false;

			//NOTE: When executing the callback in an async fashion need to check to see if it's disposed
			if (!callback.IsDisposed)
			{
				using (callback)
				{
					//Accept Request to raise Quota
					//callback.Continue(true);
					//return true;
				}
			}

			return false;
		}

		bool IRequestHandler.OnProtocolExecution(IWebBrowser browserControl, IBrowser browser, string url)
		{
			return url.StartsWith("mailto");
		}

		void IRequestHandler.OnRenderViewReady(IWebBrowser browserControl, IBrowser browser)
		{

		}

		bool IRequestHandler.OnResourceResponse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response)
		{
			//NOTE: You cannot modify the response, only the request
			// You can now access the headers
			//var headers = response.ResponseHeaders;

			return false;
		}

		IResponseFilter IRequestHandler.GetResourceResponseFilter(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response)
		{
			var url = new Uri(request.Url);
			if (url.AbsoluteUri.StartsWith(BotzoneProtocol.Credentials.BotzoneLocalMatchURL(""),
				StringComparison.OrdinalIgnoreCase))
			{
				return MatchInjectFilter;
			}

			return null;
		}

		void IRequestHandler.OnResourceLoadComplete(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
		{

		}

		bool IRequestHandler.OnSelectClientCertificate(IWebBrowser browserControl, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback)
		{
			callback.Dispose();
			return false;
		}

		void IRequestHandler.OnResourceRedirect(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response, ref string newUrl)
		{

		}
	}
}