using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.CloudMine.Core.Collectors.Tests.Telemetry
{
    public class NoopLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            // do nothing
        }
    }
}
