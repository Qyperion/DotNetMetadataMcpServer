using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using DotNetMetadataMcpServer.Models;
using DotNetMetadataMcpServer.Services;
using ModelContextProtocol.NET.Server.Features.Tools;
using ModelContextProtocol.NET.Server.Session;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.NET.Core.Models.Protocol.Client.Responses;
using ModelContextProtocol.NET.Core.Models.Protocol.Common;
using ModelContextProtocol.NET.Core.Models.Protocol.Shared.Content;
using ModelContextProtocol.NET.Server.Contexts;

namespace DotNetMetadataMcpServer.ToolHandlers
{
    public class AssemblyToolHandler(
        IServerContext serverContext,
        ISessionFacade sessionFacade,
        DependenciesScanner scanner,
        AssemblyToolService assemblyToolService,
        ILogger<AssemblyToolHandler> logger
    ) : ToolHandlerBase<AssemblyToolParameters>(tool, serverContext, sessionFacade)
    {
        private static readonly Tool tool = new()
        {
            Name = "ReferencedAssemblies",
            Description = "Retrieves referenced assemblies based on filters and pagination.",
            InputSchema = AssemblyToolParametersJsonContext.Default.AssemblyToolParameters.GetToolSchema<AssemblyToolParameters>()!,
        };

        protected override Task<CallToolResult> HandleAsync(AssemblyToolParameters parameters, CancellationToken cancellationToken = default)
        {
            var response = assemblyToolService.GetAssemblies(parameters.ProjectFileAbsolutePath, parameters.FullTextFiltersWithWildCardSupport, parameters.PageNumber, 20);
            var json = JsonSerializer.Serialize(response);
            var content = new TextContent { Text = json };
            var callToolResult = new CallToolResult { Content = new[] { content } };
            return Task.FromResult(callToolResult);
        }

        public override JsonTypeInfo JsonTypeInfo => AssemblyToolParametersJsonContext.Default.AssemblyToolParameters;
    }
}