using ModelContextProtocol.NET.Core.Models.Protocol.Common;
using ModelContextProtocol.NET.Server.Builder;
using Serilog;

namespace DotNetMetadataMcpServer;

public class Program
{
    public static async Task Main(string[] args)
    {
        // todo: add settings
        
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
                Name = "DonNet Projects Metadata MCP Server",
                Version = "1.0.0"
            };
        
            var builder = new McpServerBuilder(serverInfo).AddStdioTransport();
            builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(logger));
            
            builder.Services.AddScoped<MsBuildHelper>();
            builder.Services.AddScoped<MyReflectionHelper>();
            builder.Services.AddScoped<MyProjectScanner>();
            builder.Services.AddScoped<MyMetadataToolHandler>();
            
            builder.Tools.AddHandler<MyMetadataToolHandler>();
            
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
            //.WriteTo.File("log.txt")
            .WriteTo.Console()
            .MinimumLevel.Debug()
            .CreateLogger();
        Log.Logger = logger; 
        
        try
        {
            var serverInfo = new Implementation
            {
                Name = "DonNet Projects Metadata MCP Server",
                Version = "1.0.0"
            };
        
            var builder = Host.CreateApplicationBuilder();
            
            builder.Services.AddMcpServer(serverInfo, mcp => {
                mcp.AddStdioTransport();
                mcp.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(logger));
                mcp.Tools.AddHandler<MyMetadataToolHandler>();
                // same as without hosting
            }, keepDefaultLogging: false); // clear default console logging
            
            //var builder = new McpServerBuilder(serverInfo).AddStdioTransport();
            builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(logger));
            
            builder.Services.AddScoped<MsBuildHelper>();
            builder.Services.AddScoped<MyReflectionHelper>();
            builder.Services.AddScoped<MyProjectScanner>();
            builder.Services.AddScoped<MyMetadataToolHandler>();

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
