using System;
using StatLight.Core.Common;
using StatLight.Core.Common.Abstractions.Timing;
using StatLight.Core.Events;

namespace StatLight.Core.Monitoring
{
    public class BrowserCommunicationTimeoutMonitor : 
        IListener<TestRunCompletedServerEvent>,
        IListener<DialogAssertionServerEvent>,
        IListener<MessageReceivedFromClientServerEvent>
    {
        private readonly ILogger _logger;
        private readonly IEventPublisher _eventPublisher;
        private readonly ITimer _maxTimeoutTimer;
        private readonly TimeSpan _maxTimeAllowedBeforeCommunicationErrorSent;
        private DateTime _lastTimeAnyEventArrived;
        private bool _hasPublishedEvent;

        public BrowserCommunicationTimeoutMonitor(ILogger logger, IEventPublisher eventPublisher,
            ITimer maxTimeoutTimer, TimeSpan maxTimeAllowedBeforeCommunicationErrorSent)
        {
            if (maxTimeoutTimer == null) throw new ArgumentNullException("maxTimeoutTimer");
            _logger = logger;
            _eventPublisher = eventPublisher;
            _maxTimeoutTimer = maxTimeoutTimer;
            _maxTimeAllowedBeforeCommunicationErrorSent = maxTimeAllowedBeforeCommunicationErrorSent;

            _maxTimeoutTimer.Elapsed += MaxTimeoutTimerElapsed;

            _lastTimeAnyEventArrived = DateTime.Now;
            _maxTimeoutTimer.Start();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "BrowserHostCommunicationTimeoutServerEvent")]
        void MaxTimeoutTimerElapsed(object sender, TimerWrapperElapsedEventArgs e)
        {
            var ticksElapsedSinceLastMessage = e.SignalTime.Ticks - _lastTimeAnyEventArrived.Ticks;

            //_logger.Debug("_hasPublishedEvent=[{0}] ticksElapsedSinceLastMessage=[{1}] _maxTimeAllowedBeforeCommunicationErrorSent.Ticks=[{2}]".FormatWith(_hasPublishedEvent, ticksElapsedSinceLastMessage, _maxTimeAllowedBeforeCommunicationErrorSent.Ticks));

            if (!_hasPublishedEvent)
            {
                if (ticksElapsedSinceLastMessage > _maxTimeAllowedBeforeCommunicationErrorSent.Ticks)
                {
                    _hasPublishedEvent = true;
                    _logger.Debug("Starting publish of BrowserHostCommunicationTimeoutServerEvent");
                    _eventPublisher
                        .SendMessage(
                            new BrowserHostCommunicationTimeoutServerEvent
                                {
                                    Message = "No communication from the web browser has been detected. We've waited longer than the configured time of {0}".FormatWith(new TimeSpan(_maxTimeAllowedBeforeCommunicationErrorSent.Ticks))
                                }
                        );

                    _eventPublisher
                        .SendMessage<TestRunCompletedServerEvent>();
                }
            }
        }

        public void Handle(TestRunCompletedServerEvent message)
        {
            _maxTimeoutTimer.Stop();
        }

        public void Handle(DialogAssertionServerEvent message)
        {
            ResetTimer();
        }

        public void Handle(MessageReceivedFromClientServerEvent message)
        {
            ResetTimer();
        }

        private void ResetTimer()
        {
            _lastTimeAnyEventArrived = DateTime.Now;
        }
    }
}