﻿
namespace StatLight.Core.Reporting.Providers.TeamCity
{
    using System;
    using System.Text;
    using Events;
    using StatLight.Core.Properties;

    public class TeamCityTestResultHandler : ITestingReportEvents
    {
        private readonly ICommandWriter _messageWriter;
        private readonly string _assemblyName;

        public TeamCityTestResultHandler(ICommandWriter messageWriter, string assemblyName)
        {
            _messageWriter = messageWriter;
            _assemblyName = assemblyName;
        }

        public void PublishStart()
        {
            _messageWriter.Write(
                CommandFactory.TestSuiteStarted(_assemblyName));
        }

        public void PublishStop()
        {
            _messageWriter.Write(
                CommandFactory.TestSuiteFinished(_assemblyName));
        }

        private void WrapTestWithStartAndEnd(Command command, string name, long durationMilliseconds)
        {
            WrapTestWithStartAndEnd(() => _messageWriter.Write(command), name, durationMilliseconds);
        }

        private void WrapTestWithStartAndEnd(Action action, string name, long durationMilliseconds)
        {
            _messageWriter.Write(CommandFactory.TestStarted(name));
            action();
            _messageWriter.Write(CommandFactory.TestFinished(name, durationMilliseconds));
        }

        public void Handle(TraceClientEvent message)
        {
            if (message == null) throw new ArgumentNullException("message");
            _messageWriter.Write(message.Message);
        }

        public void Handle(DialogAssertionServerEvent message)
        {
            if (message == null) throw new ArgumentNullException("message");
            string writeMessage = message.Message;
            WriteServerEventFailure("DialogAssertionServerEvent", writeMessage);
        }

        private void WriteServerEventFailure(string name, string writeMessage)
        {
            const int durationMilliseconds = 0;

            WrapTestWithStartAndEnd(() => _messageWriter.Write(
                CommandFactory.TestFailed(
                    name,
                    writeMessage,
                    writeMessage)),
                name,
                durationMilliseconds);
        }

        public void Handle(BrowserHostCommunicationTimeoutServerEvent message)
        {
            if (message == null) throw new ArgumentNullException("message");
            string writeMessage = message.Message;
            WriteServerEventFailure("BrowserHostCommunicationTimeoutServerEvent", writeMessage);
        }

        public void Handle(TestCaseResultServerEvent message)
        {
            if (message == null) throw new ArgumentNullException("message");
            var name = message.FullMethodName();
            var durationMilliseconds = message.TimeToComplete.Milliseconds;

            switch (message.ResultType)
            {
                case ResultType.Ignored:
                    WrapTestWithStartAndEnd(CommandFactory.TestIgnored(message.MethodName, string.Empty), message.MethodName, 0);
                    break;
                case ResultType.Passed:

                    WrapTestWithStartAndEnd(() =>
                    {
                    }, name, durationMilliseconds);
                    break;
                case ResultType.Failed:

                    var sb = new StringBuilder();

                    sb.Append("Test Namespace:  ");
                    sb.AppendLine(message.NamespaceName);

                    sb.Append("Test Class:      ");
                    sb.AppendLine(message.ClassName);

                    sb.Append("Test Method:     ");
                    sb.AppendLine(message.MethodName);

                    if (!string.IsNullOrEmpty(message.OtherInfo))
                    {
                        sb.Append("Other Info:      ");
                        sb.AppendLine(message.OtherInfo);
                    }

                    foreach (var metaData in message.Metadata)
                    {
                        sb.Append("{0,-17}".FormatWith(metaData.Classification + ": "));
                        sb.Append(metaData.Name + " - ");
                        sb.AppendLine(metaData.Value);
                    }

                    sb.AppendLine(message.ExceptionInfo.FullMessage);

                    var msg = sb.ToString();

                    WrapTestWithStartAndEnd(() => _messageWriter.Write(
                        CommandFactory.TestFailed(
                            name,
                            message.ExceptionInfo.FullMessage,
                            msg)),
                        name,
                        durationMilliseconds);
                    break;
                case ResultType.SystemGeneratedFailure:
                    WrapTestWithStartAndEnd(() => _messageWriter.Write(
                        CommandFactory.TestFailed(
                            name,
                            "StatLight generated test failure",
                            message.OtherInfo)),
                        name,
                        durationMilliseconds);
                    break;

                default:
                    "Unknown TestCaseResultServerEvent (to StatLight) - {0}".FormatWith(message.ResultType)
                        .WrapConsoleMessageWithColor(Settings.Default.ConsoleColorError, true);
                    break;
            }

        }

        public void Handle(FatalSilverlightExceptionServerEvent message)
        {
            if (message == null) throw new ArgumentNullException("message");
            string writeMessage = message.Message;
            WriteServerEventFailure("FatalSilverlightExceptionServerEvent", writeMessage);
        }

        public void Handle(UnhandledExceptionClientEvent message)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (message.ExceptionInfo == null) return;
            string writeMessage = message.ExceptionInfo.FullMessage;
            WriteServerEventFailure("FatalSilverlightExceptionServerEvent", writeMessage);
        }
    }
}
