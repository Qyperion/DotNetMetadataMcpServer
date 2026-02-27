using DotNetMetadataMcpServer;
using Microsoft.Extensions.Logging;
using Moq;
using NuGet.Common;

namespace MetadataExplorerTest;

[TestFixture]
public class MicrosoftLoggerAdapterTests
{
    private Mock<Microsoft.Extensions.Logging.ILogger> _mockLogger;
    private MicrosoftLoggerAdapter _adapter;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger>();
        _mockLogger.Setup(l => l.IsEnabled(It.IsAny<Microsoft.Extensions.Logging.LogLevel>())).Returns(true);
        _adapter = new MicrosoftLoggerAdapter(_mockLogger.Object);
    }

    [Test]
    public void LogDebug_DelegatesWithDebugLevel()
    {
        _adapter.LogDebug("debug message");
        VerifyLogCalled(Microsoft.Extensions.Logging.LogLevel.Debug);
    }

    [Test]
    public void LogVerbose_DelegatesWithTraceLevel()
    {
        _adapter.LogVerbose("verbose message");
        VerifyLogCalled(Microsoft.Extensions.Logging.LogLevel.Trace);
    }

    [Test]
    public void LogInformation_DelegatesWithInformationLevel()
    {
        _adapter.LogInformation("info message");
        VerifyLogCalled(Microsoft.Extensions.Logging.LogLevel.Information);
    }

    [Test]
    public void LogMinimal_DelegatesWithInformationLevel()
    {
        _adapter.LogMinimal("minimal message");
        VerifyLogCalled(Microsoft.Extensions.Logging.LogLevel.Information);
    }

    [Test]
    public void LogWarning_DelegatesWithWarningLevel()
    {
        _adapter.LogWarning("warning message");
        VerifyLogCalled(Microsoft.Extensions.Logging.LogLevel.Warning);
    }

    [Test]
    public void LogError_DelegatesWithErrorLevel()
    {
        _adapter.LogError("error message");
        VerifyLogCalled(Microsoft.Extensions.Logging.LogLevel.Error);
    }

    [Test]
    public void LogInformationSummary_DelegatesWithInformationLevel()
    {
        _adapter.LogInformationSummary("summary message");
        VerifyLogCalled(Microsoft.Extensions.Logging.LogLevel.Information);
    }

    [Test]
    [TestCase(NuGet.Common.LogLevel.Debug, Microsoft.Extensions.Logging.LogLevel.Debug)]
    [TestCase(NuGet.Common.LogLevel.Verbose, Microsoft.Extensions.Logging.LogLevel.Trace)]
    [TestCase(NuGet.Common.LogLevel.Information, Microsoft.Extensions.Logging.LogLevel.Information)]
    [TestCase(NuGet.Common.LogLevel.Minimal, Microsoft.Extensions.Logging.LogLevel.Information)]
    [TestCase(NuGet.Common.LogLevel.Warning, Microsoft.Extensions.Logging.LogLevel.Warning)]
    [TestCase(NuGet.Common.LogLevel.Error, Microsoft.Extensions.Logging.LogLevel.Error)]
    public void Log_NuGetLevel_MapsCorrectly(NuGet.Common.LogLevel nugetLevel, Microsoft.Extensions.Logging.LogLevel expectedLevel)
    {
        _adapter.Log(nugetLevel, "test data");
        VerifyLogCalled(expectedLevel);
    }

    [Test]
    [TestCase(NuGet.Common.LogLevel.Debug, Microsoft.Extensions.Logging.LogLevel.Debug)]
    [TestCase(NuGet.Common.LogLevel.Verbose, Microsoft.Extensions.Logging.LogLevel.Trace)]
    [TestCase(NuGet.Common.LogLevel.Information, Microsoft.Extensions.Logging.LogLevel.Information)]
    [TestCase(NuGet.Common.LogLevel.Minimal, Microsoft.Extensions.Logging.LogLevel.Information)]
    [TestCase(NuGet.Common.LogLevel.Warning, Microsoft.Extensions.Logging.LogLevel.Warning)]
    [TestCase(NuGet.Common.LogLevel.Error, Microsoft.Extensions.Logging.LogLevel.Error)]
    public async Task LogAsync_NuGetLevel_MapsCorrectly(NuGet.Common.LogLevel nugetLevel, Microsoft.Extensions.Logging.LogLevel expectedLevel)
    {
        await _adapter.LogAsync(nugetLevel, "test data");
        VerifyLogCalled(expectedLevel);
    }

    [Test]
    public void Log_ILogMessage_UsesMessageLevelAndContent()
    {
        var mockMessage = new Mock<ILogMessage>();
        mockMessage.Setup(m => m.Level).Returns(NuGet.Common.LogLevel.Warning);
        mockMessage.Setup(m => m.Message).Returns("warning from message");

        _adapter.Log(mockMessage.Object);
        VerifyLogCalled(Microsoft.Extensions.Logging.LogLevel.Warning);
    }

    [Test]
    public async Task LogAsync_ILogMessage_UsesMessageLevelAndContent()
    {
        var mockMessage = new Mock<ILogMessage>();
        mockMessage.Setup(m => m.Level).Returns(NuGet.Common.LogLevel.Error);
        mockMessage.Setup(m => m.Message).Returns("error from message");

        await _adapter.LogAsync(mockMessage.Object);
        VerifyLogCalled(Microsoft.Extensions.Logging.LogLevel.Error);
    }

    private void VerifyLogCalled(Microsoft.Extensions.Logging.LogLevel expectedLevel)
    {
        _mockLogger.Verify(
            l => l.Log(
                expectedLevel,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
