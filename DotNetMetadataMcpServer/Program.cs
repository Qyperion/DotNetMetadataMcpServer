using ModelContextProtocol.NET.Core.Models.Protocol.Common;
using ModelContextProtocol.NET.Server.Builder;
using ModelContextProtocol.NET.Server.Hosting;
using Serilog;

namespace DotNetMetadataMcpServer;

public class Program
{
    public static async Task Main(string[] args)
    {
        // todo: add settings
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
        
        // todo: collect constructor parameter names
        
        //Environment.SetEnvironmentVariable("HOME", "/home/vladimir");
        
        var logger = new LoggerConfiguration()
            .WriteTo.File("log.txt")
            //.WriteTo.Console()
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
        
            var builder = new McpServerBuilder(serverInfo).AddStdioTransport();
            builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(logger));
            
            builder.Services.Configure<ToolsConfiguration>(configuration.GetSection(ToolsConfiguration.SectionName));
            
            builder.Services.AddScoped<MsBuildHelper>();
            builder.Services.AddScoped<ReflectionTypesCollector>();
            builder.Services.AddScoped<DependenciesScanner>();

            builder.Services.AddScoped<AssemblyToolService>();
            builder.Services.AddScoped<NamespaceToolService>();
            builder.Services.AddScoped<TypeToolService>();
            

            builder.Tools.AddHandler<AssemblyToolHandler>();
            builder.Tools.AddHandler<NamespaceToolHandler>();
            builder.Tools.AddHandler<TypeToolHandler>();
            
            var host = builder.Build();
            host.Start();
            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
    
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
