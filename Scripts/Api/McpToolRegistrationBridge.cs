using System.Collections.Generic;
using Timberborn.SingletonSystem;

namespace TimberbornMCP.Api {

  // Bound once per scene context (MainMenu/Game/MapEditor). Registers every IMcpTool/IMcpResource
  // MultiBind-ed into that scene's own container - ours and any third-party mod's alike - into the
  // process-lifetime registry on Load(), and removes them again on Unload(). This is the entire
  // extensibility mechanism: nothing else needs to know this class exists.
  public class McpToolRegistrationBridge : ILoadableSingleton, IUnloadableSingleton {

    private readonly McpToolRegistry _registry;

    private readonly IEnumerable<IMcpTool> _tools;

    private readonly IEnumerable<IMcpResource> _resources;

    public McpToolRegistrationBridge(McpToolRegistry registry, IEnumerable<IMcpTool> tools,
        IEnumerable<IMcpResource> resources) {
      _registry = registry;
      _tools = tools;
      _resources = resources;
    }

    public void Load() {
      foreach (var tool in _tools) {
        _registry.RegisterTool(tool);
      }
      foreach (var resource in _resources) {
        _registry.RegisterResource(resource);
      }
    }

    public void Unload() {
      foreach (var tool in _tools) {
        _registry.UnregisterTool(tool);
      }
      foreach (var resource in _resources) {
        _registry.UnregisterResource(resource);
      }
    }

  }

}
