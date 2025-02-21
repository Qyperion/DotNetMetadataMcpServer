using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Services;
using DotNetMetadataMcpServer.ToolHandlers;
using ModelContextProtocol.NET.Core.Models.Protocol.Common;
using ModelContextProtocol.NET.Server.Builder;
using Serilog;

namespace DotNetMetadataMcpServer;

// ReSharper disable once UnusedType.Global
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class Program
{
    /// <summary>
    /// DotNet Metadata MCP Server
    /// </summary>
    /// <param name="homeEnvVariable">The home environment variable</param>
    /// <returns></returns>
    public static async Task<int> Main(string homeEnvVariable)
    {
        if (string.IsNullOrWhiteSpace(homeEnvVariable))
        {
            Console.WriteLine("The --homeEnvVariable argument is required");
            return 1;
        }
        
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
        
        Environment.SetEnvironmentVariable("HOME", homeEnvVariable);
        
        var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("RunId", Guid.NewGuid())
            .CreateLogger();
        
        Log.Logger = logger; 
        
        try
        {
            logger.Information("Starting the server");
            
            var serverInfo = new Implementation
            {
                Name = "DotNet Projects Types Explorer MCP Server",
                Version = "1.0.0"
            };
        
            var builder = new McpServerBuilder(serverInfo).AddStdioTransport();
            builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(logger));
            
            builder.Services.Configure<ToolsConfiguration>(configuration.GetSection(ToolsConfiguration.SectionName));
            
            builder.Services.AddScoped<MsBuildHelper>();
            builder.Services.AddScoped<ReflectionTypesCollector>();
            builder.Services.AddScoped(typeof(IDependenciesScanner), typeof(DependenciesScanner));

            builder.Services.AddScoped<AssemblyToolService>();
            builder.Services.AddScoped<NamespaceToolService>();
            builder.Services.AddScoped<TypeToolService>();
            

            builder.Tools.AddHandler<AssemblyToolHandler>();
            builder.Tools.AddHandler<NamespaceToolHandler>();
            builder.Tools.AddHandler<TypeToolHandler>();
            
            var host = builder.Build();
            host.Start();
            await Task.Delay(-1);

            return 0;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "An error occurred while running the server");
            Console.WriteLine(ex);
            
            return 1;
        }
        finally
        {
            logger.Information("Shutting down the server");
            await Log.CloseAndFlushAsync();
        }
    }
    
    // todo: refactor to use the new builder
    /*public static async Task Main(string[] args)
    {
        var logger = new LoggerConfiguration()
            .WriteTo.File("log.txt")
            .MinimumLevel.Debug()
            .CreateLogger();
        Log.Logger = logger; 
        
        try
        {
            var serverInfo = new Implementation
            {
                Name = "DotNet Projects Types Explorer MCP Server",
                Version = "1.0.0"
            };
        
            var builder = Host.CreateApplicationBuilder();
            
            builder.Services.AddMcpServer(serverInfo, mcp => {
                mcp.AddStdioTransport();
                mcp.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(logger));
                mcp.Tools.AddHandler<DotNetProjectTypesExplorerToolHandler>();
            }, keepDefaultLogging: false);
            
            builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(logger));
            
            builder.Services.AddScoped<MsBuildHelper>();
            builder.Services.AddScoped<ReflectionTypesCollector>();
            builder.Services.AddScoped<DependenciesScanner>();
            builder.Services.AddScoped<DotNetProjectTypesExplorerToolHandler>();

            var host = builder.Build();
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }*/
}
