using Discord;
using Microsoft.Extensions.Logging;

namespace MarbleBot.Services
{
    public class LogRunner
    {
        private readonly ILogger<LogRunner> _logger;

        public LogRunner(ILogger<LogRunner> logger)
        {
            _logger = logger;
        }

        public void LogInfo(string logString) => _logger.LogInformation(logString);

        public void LogMessage(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    _logger.LogCritical(message.Exception, message.Message);
                    break;
                case LogSeverity.Error:
                    _logger.LogError(message.Exception, message.Message);
                    break;
                case LogSeverity.Warning:
                    _logger.LogWarning(message.Exception, message.Message);
                    break;
                case LogSeverity.Debug:
                    _logger.LogDebug(message.Exception, message.Message);
                    break;
                case LogSeverity.Verbose:
                    _logger.LogTrace(message.Exception, message.Message);
                    break;
                default:
                    _logger.LogInformation(message.Exception, message.Message);
                    break;
            }
        }
    }
}
