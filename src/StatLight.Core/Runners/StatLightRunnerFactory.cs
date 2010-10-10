﻿
namespace StatLight.Core.Runners
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Web;
	using StatLight.Client.Harness.Events;
	using StatLight.Core.Configuration;
	using StatLight.Core.Common;
	using StatLight.Core.Events.Aggregation;
	using StatLight.Core.Monitoring;
	using StatLight.Core.Reporting.Providers.Console;
	using StatLight.Core.Reporting.Providers.TeamCity;
	using StatLight.Core.Timing;
	using StatLight.Core.WebBrowser;
	using StatLight.Core.WebServer;
	using StatLight.Core.WebServer.Host;
	using StatLight.Core.WebServer.XapHost;

	public class StatLightRunnerFactory
	{
		private readonly IEventAggregator _eventAggregator;
		private BrowserCommunicationTimeoutMonitor _browserCommunicationTimeoutMonitor;
		private ConsoleResultHandler _consoleResultHandler;
		private Action<DebugClientEvent> _debugEventListener;

		public StatLightRunnerFactory()
			: this(new EventAggregator())
		{ }

		public StatLightRunnerFactory(IEventAggregator eventAggregator)
		{
			_eventAggregator = eventAggregator;
		}

		public IRunner CreateContinuousTestRunner(ILogger logger, StatLightConfiguration statLightConfiguration, bool showTestingBrowserHost)
		{
			if (logger == null) throw new ArgumentNullException("logger");
			if (statLightConfiguration == null) throw new ArgumentNullException("statLightConfiguration");
			IWebServer webServer;
			List<IWebBrowser> browserFormHosts;

			BuildAndReturnWebServiceAndBrowser(
				logger,
				showTestingBrowserHost,
				statLightConfiguration,
				out webServer,
				out browserFormHosts);

			CreateAndAddConsoleResultHandlerToEventAggregator(logger);

			IRunner runner = new ContinuousConsoleRunner(logger, _eventAggregator, statLightConfiguration.Server.XapToTestPath, statLightConfiguration.Client, webServer, browserFormHosts.First());
			return runner;
		}

		public IRunner CreateTeamCityRunner(StatLightConfiguration statLightConfiguration)
		{
			if (statLightConfiguration == null) throw new ArgumentNullException("statLightConfiguration");
			ILogger logger = new NullLogger();

			IWebServer webServer;
			List<IWebBrowser> browserFormHosts;

			BuildAndReturnWebServiceAndBrowser(
				logger,
				false,
				statLightConfiguration,
				out webServer,
				out browserFormHosts);

			var teamCityTestResultHandler = new TeamCityTestResultHandler(new ConsoleCommandWriter(), statLightConfiguration.Server.XapToTestPath);
			_eventAggregator.AddListener(teamCityTestResultHandler);
			IRunner runner = new TeamCityRunner(new NullLogger(), _eventAggregator, webServer, browserFormHosts, teamCityTestResultHandler, statLightConfiguration.Server.XapToTestPath);

			return runner;
		}

        public IRunner CreateOnetimeConsoleRunner(ILogger logger, StatLightConfiguration statLightConfiguration, bool showTestingBrowserHost)
        {
            if (logger == null) throw new ArgumentNullException("logger");
            if (statLightConfiguration == null) throw new ArgumentNullException("statLightConfiguration");
            IWebServer webServer;
            List<IWebBrowser> browserFormHosts;

			BuildAndReturnWebServiceAndBrowser(
				logger,
				showTestingBrowserHost,
				statLightConfiguration,
				out webServer,
				out browserFormHosts);

			CreateAndAddConsoleResultHandlerToEventAggregator(logger);
			IRunner runner = new OnetimeRunner(logger, _eventAggregator, webServer, browserFormHosts, statLightConfiguration.Server.XapToTestPath);
			return runner;
		}

		public IRunner CreateWebServerOnlyRunner(ILogger logger, StatLightConfiguration statLightConfiguration)
		{
			if (logger == null) throw new ArgumentNullException("logger");
			if (statLightConfiguration == null) throw new ArgumentNullException("statLightConfiguration");
			var location = new WebServerLocation();

			var webServer = CreateWebServer(logger, statLightConfiguration, location);

			CreateAndAddConsoleResultHandlerToEventAggregator(logger);
			SetupDebugClientEventListener(logger);
			IRunner runner = new WebServerOnlyRunner(logger, _eventAggregator, webServer, location.TestPageUrl, statLightConfiguration.Server.XapToTestPath);

			return runner;
		}

		private static void SetXapHostUrl(StatLightConfiguration statLightConfiguration, WebServerLocation location)
		{
			if (statLightConfiguration.Client.XapToTestUrl == null)
				statLightConfiguration.Client.XapToTestUrl = (location.BaseUrl + StatLightServiceRestApi.GetXapToTest);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		private IWebServer CreateWebServer(ILogger logger, StatLightConfiguration statLightConfiguration, WebServerLocation location)
		{
			SetXapHostUrl(statLightConfiguration, location);

			var postHandler = new PostHandler(logger, _eventAggregator, statLightConfiguration.Client);

			//var statLightService = new StatLightService(logger, statLightConfiguration.Client, statLightConfiguration.Server, postHandler);
			//return new StatLightServiceHost(logger, statLightService, location.BaseUrl);

			const string machineName = "localhost";
			int port = location.BaseUrl.Port;
			var responseFactory = new ResponseFactory(statLightConfiguration.Server.XapToTextFactory, statLightConfiguration.Server.HostXap, statLightConfiguration.Client);
			return new TestServiceEngine(logger, machineName, port, responseFactory, postHandler);

		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		private void BuildAndReturnWebServiceAndBrowser(
			ILogger logger,
			bool showTestingBrowserHost,
			StatLightConfiguration statLightConfiguration,
			out IWebServer webServer,
			out List<IWebBrowser> browserFormHosts)
		{
			ClientTestRunConfiguration clientTestRunConfiguration = statLightConfiguration.Client;
			ServerTestRunConfiguration serverTestRunConfiguration = statLightConfiguration.Server;

			var location = new WebServerLocation();
			var debugAssertMonitorTimer = new TimerWrapper(serverTestRunConfiguration.DialogSmackDownElapseMilliseconds);
			var dialogMonitors = new List<IDialogMonitor>
			{
				new DebugAssertMonitor(logger),
				new MessageBoxMonitor(logger),
			};
			var dialogMonitorRunner = new DialogMonitorRunner(logger, _eventAggregator, debugAssertMonitorTimer, dialogMonitors);
			SetupDebugClientEventListener(logger);
			webServer = CreateWebServer(logger, statLightConfiguration, location);

			showTestingBrowserHost = GetShowTestingBrowserHost(serverTestRunConfiguration, showTestingBrowserHost);

			browserFormHosts = GetBrowserFormHosts(logger, location.TestPageUrl, clientTestRunConfiguration, showTestingBrowserHost, dialogMonitorRunner, serverTestRunConfiguration.QueryString);

			StartupBrowserCommunicationTimeoutMonitor(new TimeSpan(0, 0, 5, 0));
		}

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "testPageUrlWithQueryString")]
        private static List<IWebBrowser> GetBrowserFormHosts(ILogger logger, Uri testPageUrl, ClientTestRunConfiguration clientTestRunConfiguration, bool showTestingBrowserHost, DialogMonitorRunner dialogMonitorRunner, string queryString)
        {
            var webBrowserType = WebBrowserType.SelfHostedWebBrowser;
            var webBrowserFactory = new WebBrowserFactory(logger);
            var testPageUrlWithQueryString = new Uri(testPageUrl + "?" + queryString);
            logger.Debug("testPageUrlWithQueryString = " + testPageUrlWithQueryString);
            List<IWebBrowser> browserFormHosts = Enumerable
                .Range(1, clientTestRunConfiguration.NumberOfBrowserHosts)
                .Select(browserI => webBrowserFactory.Create(webBrowserType, testPageUrlWithQueryString, showTestingBrowserHost, dialogMonitorRunner))
                //.Select(browserI => new SelfHostedWebBrowser(logger, testPageUrlWithQueryString, showTestingBrowserHost, dialogMonitorRunner))
                .Cast<IWebBrowser>()
                .ToList();
            return browserFormHosts;
        }

		private void StartupBrowserCommunicationTimeoutMonitor(TimeSpan maxTimeAllowedBeforeCommErrorSent)
		{
			if (_browserCommunicationTimeoutMonitor == null)
				_browserCommunicationTimeoutMonitor = new BrowserCommunicationTimeoutMonitor(_eventAggregator, new TimerWrapper(3000), maxTimeAllowedBeforeCommErrorSent);
		}

		private void CreateAndAddConsoleResultHandlerToEventAggregator(ILogger logger)
		{
			if (_consoleResultHandler == null)
			{
				_consoleResultHandler = new ConsoleResultHandler(logger);
				_eventAggregator.AddListener(_consoleResultHandler);
			}
		}

		private void SetupDebugClientEventListener(ILogger logger)
		{
			((EventAggregator)_eventAggregator).Logger = logger;
			if (_debugEventListener == null)
			{
				_debugEventListener = e => logger.Debug(e.Message);
				_eventAggregator.AddListener(_debugEventListener);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		public IRunner CreateRemotelyHostedRunner(ILogger logger, StatLightConfiguration statLightConfiguration, bool showTestingBrowserHost)
		{
			if (logger == null) throw new ArgumentNullException("logger");
			if (statLightConfiguration == null) throw new ArgumentNullException("statLightConfiguration");

			ClientTestRunConfiguration clientTestRunConfiguration = statLightConfiguration.Client;
			ServerTestRunConfiguration serverTestRunConfiguration = statLightConfiguration.Server;

			var urlToTestPage = statLightConfiguration.Client.XapToTestUrl.ToUri();

			var location = new RemoteSiteOverriddenLocation(urlToTestPage);
			var debugAssertMonitorTimer = new TimerWrapper(serverTestRunConfiguration.DialogSmackDownElapseMilliseconds);
			var dialogMonitors = new List<IDialogMonitor>
			{
				new DebugAssertMonitor(logger),
				new MessageBoxMonitor(logger),
			};
			var dialogMonitorRunner = new DialogMonitorRunner(logger, _eventAggregator, debugAssertMonitorTimer, dialogMonitors);
			SetupDebugClientEventListener(logger);
			IWebServer webServer = CreateWebServer(logger, statLightConfiguration, location);

			showTestingBrowserHost = GetShowTestingBrowserHost(serverTestRunConfiguration, showTestingBrowserHost);

			var querystring = "?{0}={1}".FormatWith(StatLightServiceRestApi.StatLightResultPostbackUrl,
												   HttpUtility.UrlEncode(location.BaseUrl.ToString()));
			var testPageUrlAndPostbackQuerystring = new Uri(location.TestPageUrl + querystring);
			logger.Debug("testPageUrlAndPostbackQuerystring={0}".FormatWith(testPageUrlAndPostbackQuerystring.ToString()));
			List<IWebBrowser> browserFormHosts = GetBrowserFormHosts(logger, testPageUrlAndPostbackQuerystring, clientTestRunConfiguration, showTestingBrowserHost, dialogMonitorRunner, serverTestRunConfiguration.QueryString);

			StartupBrowserCommunicationTimeoutMonitor(new TimeSpan(0, 0, 5, 0));
			CreateAndAddConsoleResultHandlerToEventAggregator(logger);

			IRunner runner = new OnetimeRunner(logger, _eventAggregator, webServer, browserFormHosts, statLightConfiguration.Server.XapToTestPath);
			return runner;
		}

		private static bool GetShowTestingBrowserHost(ServerTestRunConfiguration serverTestRunConfiguration, bool showTestingBrowserHost)
		{
			// The new March/April 2010 will fail in the "minimized mode" 
			//TODO figure out how to not get the errors when these are minimized
			if (serverTestRunConfiguration.XapHostType == XapHostType.MSTestMarch2010 ||
				serverTestRunConfiguration.XapHostType == XapHostType.MSTestApril2010)
				showTestingBrowserHost = true;
			return showTestingBrowserHost;
		}
	}
}
