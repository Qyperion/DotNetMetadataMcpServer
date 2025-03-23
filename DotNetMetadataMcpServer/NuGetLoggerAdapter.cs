namespace DotNetMetadataMcpServer;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Common;

public class MicrosoftLoggerAdapter : NuGet.Common.ILogger
{
    private readonly Microsoft.Extensions.Logging.ILogger _logger;

    public MicrosoftLoggerAdapter(Microsoft.Extensions.Logging.ILogger logger)
    {
        _logger = logger;
    }

    public void LogDebug(string data)
    {
        _logger.LogDebug(data);
    }

    public void LogVerbose(string data)
    {
        _logger.LogTrace(data);
    }

    public void LogInformation(string data)
    {
        _logger.LogInformation(data);
    }

    public void LogMinimal(string data)
    {
        _logger.LogInformation(data);
    }

    public void LogWarning(string data)
    {
        _logger.LogWarning(data);
    }

    public void LogError(string data)
    {
        _logger.LogError(data);
    }

    public void LogInformationSummary(string data)
    {
        _logger.LogInformation(data);
    }

    public void Log(NuGet.Common.LogLevel level, string data)
    {
        throw new NotImplementedException();
    }

    public Task LogAsync(NuGet.Common.LogLevel level, string data)
    {
        throw new NotImplementedException();
    }

    public void Log(Microsoft.Extensions.Logging.LogLevel level, string data)
    {
        _logger.Log(level, data);
    }

    public Task LogAsync(Microsoft.Extensions.Logging.LogLevel level, string data)
    {
        _logger.Log(level, data);
        return Task.CompletedTask;
    }

    public void Log(ILogMessage message)
    {
        _logger.Log(MapLogLevel(message.Level), message.Message);
    }

    public Task LogAsync(ILogMessage message)
    {
        _logger.Log(MapLogLevel(message.Level), message.Message);
        return Task.CompletedTask;
    }

    private Microsoft.Extensions.Logging.LogLevel MapLogLevel(NuGet.Common.LogLevel level)
    {
        return level switch
        {
            NuGet.Common.LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
            NuGet.Common.LogLevel.Verbose => Microsoft.Extensions.Logging.LogLevel.Trace,
            NuGet.Common.LogLevel.Information => Microsoft.Extensions.Logging.LogLevel.Information,
            NuGet.Common.LogLevel.Minimal => Microsoft.Extensions.Logging.LogLevel.Information,
            NuGet.Common.LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
            NuGet.Common.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
            _ => Microsoft.Extensions.Logging.LogLevel.Information // Default case for any future enum values
        };
    }
}