using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using Xunit.Abstractions;

namespace Timeless.Testing.Xunit
{
    public class TestOutputLogger : ILogger
    {
        private readonly ITestOutputHelper log;
        private readonly TestOutputLoggerOptions opt;

        public TestOutputLogger(ITestOutputHelper log, IOptions<TestOutputLoggerOptions> opt)
        {
            this.log = log;
            this.opt = opt.Value;
        }

        public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException("Scopes are not supported");

        public bool IsEnabled(LogLevel logLevel) => logLevel >= opt.MinimumLogLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            log.WriteLine($"[{logLevel,-11}]: {formatter(state, exception)}");
        }
    }
}
