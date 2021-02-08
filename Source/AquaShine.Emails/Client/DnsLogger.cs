using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using System;

namespace AquaShine.Emails.Client
{
    public class LoggerFactoryWrapper : DnsClient.Internal.ILoggerFactory
    {
        private readonly Microsoft.Extensions.Logging.ILoggerFactory _microsoftLoggerFactory;

        public LoggerFactoryWrapper(Microsoft.Extensions.Logging.ILoggerFactory microsoftLoggerFactory)
        {
            _microsoftLoggerFactory = microsoftLoggerFactory ?? throw new ArgumentNullException(nameof(microsoftLoggerFactory));
        }

        public DnsClient.Internal.ILogger CreateLogger(string categoryName)
        {
            return new DnsLogger(_microsoftLoggerFactory.CreateLogger(categoryName));
        }

        private class DnsLogger : DnsClient.Internal.ILogger
        {
            private readonly Microsoft.Extensions.Logging.ILogger _logger;

            public DnsLogger(Microsoft.Extensions.Logging.ILogger logger)
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public bool IsEnabled(DnsClient.Internal.LogLevel logLevel)
            {
                return _logger.IsEnabled((Microsoft.Extensions.Logging.LogLevel)logLevel);
            }

            public void Log(DnsClient.Internal.LogLevel logLevel, int eventId, Exception exception, string message, params object[] args)
            {
                _logger.Log((Microsoft.Extensions.Logging.LogLevel)logLevel, eventId, exception, message, args);
            }
        }
    }
}