using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TimberbornMCP.Api {

  // Bootstrapper-scoped (process-lifetime), name/uri-keyed and idempotent so it behaves the same
  // whether a scene contributes tools once or (defensively) more than once.
  public class McpToolRegistry {

    private readonly ConcurrentDictionary<string, IMcpTool> _tools = new();

    private readonly ConcurrentDictionary<string, IMcpResource> _resources = new();

    public void RegisterTool(IMcpTool tool) {
      _tools[tool.Name] = tool;
    }

    public void UnregisterTool(IMcpTool tool) {
      ((ICollection<KeyValuePair<string, IMcpTool>>) _tools)
          .Remove(new KeyValuePair<string, IMcpTool>(tool.Name, tool));
    }

    public bool TryGetTool(string name, out IMcpTool tool) {
      return _tools.TryGetValue(name, out tool);
    }

    public IReadOnlyCollection<IMcpTool> SnapshotTools() {
      return new List<IMcpTool>(_tools.Values);
    }

    public void RegisterResource(IMcpResource resource) {
      _resources[resource.Uri] = resource;
    }

    public void UnregisterResource(IMcpResource resource) {
      ((ICollection<KeyValuePair<string, IMcpResource>>) _resources)
          .Remove(new KeyValuePair<string, IMcpResource>(resource.Uri, resource));
    }

    public bool TryGetResource(string uri, out IMcpResource resource) {
      return _resources.TryGetValue(uri, out resource);
    }

    public IReadOnlyCollection<IMcpResource> SnapshotResources() {
      return new List<IMcpResource>(_resources.Values);
    }

  }

}
