using DotNetMetadataMcpServer.Configuration;
using DotNetMetadataMcpServer.Services;
using DotNetMetadataMcpServer.Tools;
using Microsoft.Build.Locator;
using Serilog;
using System.Reflection;

namespace DotNetMetadataMcpServer;

// ReSharper disable once UnusedType.Global
public class Program
{
    /// <summary>
    /// DotNet Metadata MCP Server
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code</returns>
    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 2 || string.IsNullOrWhiteSpace(args[0]) || args[0] != "--homeEnvVariable" || string.IsNullOrWhiteSpace(args[1]))
        {
            Console.WriteLine("The --homeEnvVariable argument with a value is required");
            return 1;
        }

        // Register MSBuild defaults as early as possible so that MSBuild-shipped assemblies (notably NuGet.Frameworks, required by NuGet.Protocol at runtime) can be resolved even when no MSBuild-based tool is invoked.
        // NuGet.Frameworks is intentionally excluded from app output (MSBL001) to avoid conflicts with the MSBuild-provided version.
        if (!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var homeEnvVariable = args[1];
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

            var builder = Host.CreateApplicationBuilder(args);

            // Configure logging
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(logger);
            builder.Logging.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
            });

            // Configure MCP server
            builder.Services.AddMcpServer(options =>
            {
                options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
                {
                    Name = "DotNet Projects Types Explorer MCP Server",
                    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"
                };
            })
            .WithStdioServerTransport()
            .WithTools<AssemblyTools>()
            .WithTools<NamespaceTools>()
            .WithTools<TypeTools>()
            .WithTools<TypeSearchTools>()
            .WithTools<InheritanceTools>()
            .WithTools<DependencyGraphTools>()
            .WithTools<NuGetTools>();

            // Register configuration
            builder.Services.Configure<ToolsConfiguration>(configuration.GetSection(ToolsConfiguration.SectionName));

            // Register project metadata cache as singleton (shared across all scoped services)
            builder.Services.AddSingleton<IProjectMetadataCache, ProjectMetadataCache>();
            // Register services as scoped (per request)
            builder.Services.AddScoped<MsBuildHelper>();
            builder.Services.AddScoped<ReflectionTypesCollector>();
            builder.Services.AddScoped<IDependenciesScanner, DependenciesScanner>();
            builder.Services.AddScoped<AssemblyToolService>();
            builder.Services.AddScoped<NamespaceToolService>();
            builder.Services.AddScoped<TypeToolService>();
            builder.Services.AddScoped<TypeSearchToolService>();
            builder.Services.AddScoped<InheritanceToolService>();
            builder.Services.AddScoped<DependencyGraphToolService>();
            builder.Services.AddScoped<NuGetToolService>();

            var host = builder.Build();
            await host.RunAsync();

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
}
