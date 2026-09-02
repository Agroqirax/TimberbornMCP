using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TimberbornMCP.Server;

namespace TimberbornMCP.Api.JsonRpc {

  // Implements the MCP JSON-RPC methods on top of the tool/resource registry. Deliberately not a
  // Bindito singleton - it's a plain helper constructed once by McpServerHost, which already holds
  // the (Bindito-injected) collaborators it needs.
  internal class McpProtocolHandler {

    private const string ProtocolVersion = "2025-06-18";

    private readonly McpToolRegistry _registry;

    private readonly McpSessionStore _sessions;

    private readonly IMainThreadDispatcher _mainThread;

    public McpProtocolHandler(McpToolRegistry registry, McpSessionStore sessions, IMainThreadDispatcher mainThread) {
      _registry = registry;
      _sessions = sessions;
      _mainThread = mainThread;
    }

    public async Task<McpDispatchResult> DispatchAsync(JObject request, CancellationToken cancellationToken) {
      var id = request["id"];
      var method = (string) request["method"];
      var @params = request["params"] as JObject ?? new JObject();

      if (string.IsNullOrEmpty(method)) {
        return Failure(id, JsonRpcErrorCodes.InvalidRequest, "Missing method.");
      }

      try {
        switch (method) {
          case "initialize":
            return HandleInitialize(id, @params);
          case "notifications/initialized":
            return new McpDispatchResult { IsNotification = true };
          case "ping":
            return Ok(id, new JObject());
          case "tools/list":
            return HandleToolsList(id);
          case "tools/call":
            return await HandleToolsCallAsync(id, @params, cancellationToken);
          case "resources/list":
            return HandleResourcesList(id);
          case "resources/read":
            return await HandleResourcesReadAsync(id, @params, cancellationToken);
          case "resources/templates/list":
            return Ok(id, new JObject { ["resourceTemplates"] = new JArray() });
          default:
            return Failure(id, JsonRpcErrorCodes.MethodNotFound, $"Unknown method '{method}'.");
        }
      } catch (Exception e) {
        return Failure(id, JsonRpcErrorCodes.InternalError, e.Message);
      }
    }

    private McpDispatchResult HandleInitialize(JToken id, JObject @params) {
      var clientProtocolVersion = (string) @params["protocolVersion"];
      var session = _sessions.Create(clientProtocolVersion ?? ProtocolVersion);
      var result = new JObject {
        ["protocolVersion"] = ProtocolVersion,
        ["serverInfo"] = new JObject { ["name"] = "TimberbornMCP", ["version"] = "1.0.0" },
        ["capabilities"] = new JObject {
          ["tools"] = new JObject(),
          ["resources"] = new JObject { ["subscribe"] = false, ["listChanged"] = false }
        }
      };
      return new McpDispatchResult { ResponseBody = JsonRpcMessages.Success(id, result), NewSession = session };
    }

    private McpDispatchResult HandleToolsList(JToken id) {
      var tools = new JArray();
      foreach (var tool in _registry.SnapshotTools()) {
        var entry = new JObject {
          ["name"] = tool.Name,
          ["description"] = tool.Description,
          ["inputSchema"] = (JToken) tool.InputSchema ?? new JObject { ["type"] = "object", ["properties"] = new JObject() }
        };
        var annotations = BuildAnnotations(tool.Annotations);
        if (annotations != null) {
          entry["annotations"] = annotations;
        }
        tools.Add(entry);
      }
      return Ok(id, new JObject { ["tools"] = tools });
    }

    private static JObject BuildAnnotations(McpToolAnnotations hints) {
      if (hints == null) {
        return null;
      }
      var json = new JObject();
      if (hints.Title != null) {
        json["title"] = hints.Title;
      }
      if (hints.ReadOnlyHint.HasValue) {
        json["readOnlyHint"] = hints.ReadOnlyHint.Value;
      }
      if (hints.DestructiveHint.HasValue) {
        json["destructiveHint"] = hints.DestructiveHint.Value;
      }
      if (hints.IdempotentHint.HasValue) {
        json["idempotentHint"] = hints.IdempotentHint.Value;
      }
      if (hints.OpenWorldHint.HasValue) {
        json["openWorldHint"] = hints.OpenWorldHint.Value;
      }
      return json;
    }

    private async Task<McpDispatchResult> HandleToolsCallAsync(JToken id, JObject @params, CancellationToken cancellationToken) {
      var name = (string) @params["name"];
      if (string.IsNullOrEmpty(name) || !_registry.TryGetTool(name, out var tool)) {
        return Failure(id, JsonRpcErrorCodes.InvalidParams, $"Unknown tool '{name}'.");
      }
      var arguments = @params["arguments"] as JObject ?? new JObject();
      var context = new McpToolContext(_mainThread, cancellationToken);

      McpToolResult result;
      try {
        result = await tool.InvokeAsync(arguments, context);
      } catch (Exception e) {
        result = McpToolResult.Error(e.Message);
      }

      var content = new JArray();
      foreach (var block in result.Content) {
        content.Add(new JObject { ["type"] = block.Type, ["text"] = block.Text });
      }
      return Ok(id, new JObject { ["content"] = content, ["isError"] = result.IsError });
    }

    private McpDispatchResult HandleResourcesList(JToken id) {
      var resources = new JArray();
      foreach (var resource in _registry.SnapshotResources()) {
        resources.Add(new JObject {
          ["uri"] = resource.Uri,
          ["name"] = resource.Name,
          ["description"] = resource.Description,
          ["mimeType"] = resource.MimeType
        });
      }
      return Ok(id, new JObject { ["resources"] = resources });
    }

    private async Task<McpDispatchResult> HandleResourcesReadAsync(JToken id, JObject @params, CancellationToken cancellationToken) {
      var uri = (string) @params["uri"];
      if (string.IsNullOrEmpty(uri) || !_registry.TryGetResource(uri, out var resource)) {
        return Failure(id, JsonRpcErrorCodes.InvalidParams, $"Unknown resource '{uri}'.");
      }
      var context = new McpResourceContext(_mainThread, cancellationToken);
      var contents = await resource.ReadAsync(context);
      return Ok(id, new JObject {
        ["contents"] = new JArray(new JObject {
          ["uri"] = contents.Uri,
          ["mimeType"] = contents.MimeType,
          ["text"] = contents.Text
        })
      });
    }

    private static McpDispatchResult Ok(JToken id, JObject result) {
      return new McpDispatchResult { ResponseBody = JsonRpcMessages.Success(id, result) };
    }

    private static McpDispatchResult Failure(JToken id, int code, string message) {
      return new McpDispatchResult { ResponseBody = JsonRpcMessages.Error(id, code, message) };
    }

  }

}
