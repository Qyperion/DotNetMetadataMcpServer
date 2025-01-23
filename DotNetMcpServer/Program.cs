using ModelContextProtocol.NET.Core.Models.Protocol.Common;
using ModelContextProtocol.NET.Server.Hosting;
using MySolution.McpServer.Handlers;
using MySolution.ProjectScanner.Core;
using Serilog;

namespace DotNetMcpServer;

public class Program
{
    public static async Task Main(string[] args)
    {
        await using var logger = new LoggerConfiguration()
            //.WriteTo.File("log.txt")
            .WriteTo.Console()
            .MinimumLevel.Debug()
            .CreateLogger();
        Log.Logger = logger;
        

        // Информация о сервере
        var serverInfo = new Implementation
        {
            Name = "Metadata MCP Server",
            Version = "1.0.0"
        };

        // Создаём GenericHost
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSerilog(logger);
        builder.Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(logger);
        });
        
        // Подключаем MCP с stdio-транспортом
        builder.Services.AddMcpServer(serverInfo, mcp =>
        {
            mcp.AddStdioTransport();
            mcp.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(logger);
            });
            mcp.Tools.AddHandler<MyMetadataToolHandler>();
        }, keepDefaultLogging: false);

        
        /*builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();  // убираем дефолтный ConsoleLogger
            logging.AddSerilog(seriLogger);
            logging.SetMinimumLevel(LogLevel.Debug);
        });*/

        // Регистрируем наши сервисы
        builder.Services.AddScoped<MsBuildHelper>();
        builder.Services.AddScoped<MyReflectionHelper>();
        builder.Services.AddScoped<MyProjectScanner>();
        builder.Services.AddScoped<MyMetadataToolHandler>();

        // Добавляем ToolHandler
        /*builder.Services.Configure<McpServerBuilderOptions>(options =>
        {
            options.ToolHandlers.Add(typeof(MyMetadataToolHandler));
        });*/

        var host = builder.Build();

        // Запускаем
        await host.RunAsync();
    }
}
