using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TimberbornMCP.Api;
using TimberbornMCP.Server;

namespace TimberbornMCP.Tools {

  // Ships as the reference example: registered through the exact same MultiBind<IMcpTool>() +
  // McpToolRegistrationBridge path a third-party mod would use, with no special-casing.
  public class GetGameSceneTool : IMcpTool {

    private readonly GameSceneCache _sceneCache;

    public GetGameSceneTool(GameSceneCache sceneCache) {
      _sceneCache = sceneCache;
    }

    public string Name => "get_game_scene";

    public string Description => "Returns the currently active Unity scene (e.g. MainMenu, Game, MapEditor).";

    public JObject InputSchema => new() {
      ["type"] = "object",
      ["properties"] = new JObject()
    };

    public McpToolAnnotations Annotations => new() {
      Title = "Get Game Scene",
      ReadOnlyHint = true,
      DestructiveHint = false,
      IdempotentHint = true,
      OpenWorldHint = false
    };

    public Task<McpToolResult> InvokeAsync(JObject arguments, McpToolContext context) {
      var payload = new JObject { ["scene"] = _sceneCache.CurrentSceneName };
      return Task.FromResult(McpToolResult.Text(payload.ToString(Formatting.None)));
    }

  }

}
