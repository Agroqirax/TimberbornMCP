# Timberborn MCP

Give an LLM a socket into your Timberborn game.

This mod adds a [Model Context Protocol](https://modelcontextprotocol.io) server. Point any MCP client (claude, opencode, etc...) at `http://localhost:8787/mcp`, and it can query and act on the running game over plain HTTP.

TimberbornMCP itself only provides the plumbing (protocol, sessions, auth, one demo tool) and a public extension point. The interesting tools are meant to come from other mods.

## Features

- **Everything built in**, no external process to run.
- **Always running**: main menu, in a save, map editor. Started automatically on load, stopped on quit.
- **Live-configurable**: change the port or token via [Mod Settings](https://steamcommunity.com/sharedfiles/filedetails/?id=3283831040) at any point.
- **Optional bearer-token auth**: On by default with a randomly generated token.
- **Open extension API** with `IMcpTool` & `IMcpResource`. Any mod can register tools with a couple lines of Bindito.

## Quick start

### Installation

- [Steam workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=xxxxxxx): Click subscribe
- [Mod.io](https://mod.io/g/timberborn/m/timberborn-mcp): Download & extract to `~/Documents/Timberborn/Mods/timberbornmcp`.
- [GitHub](https://github.com/agroqirax/timberbornmcp/releases/latest): Download & extract to `~/Documents/Timberborn/Mods/timberbornmcp`.

1. Install this mod and launch the game.
2. Open ModSettings and note the port (default `8787`) and auth token under "Timberborn MCP".
3. Point your MCP client at `http://localhost:8787/mcp` (Streamable HTTP transport).

Clear the `AuthToken` field in settings if you don't want to deal with the header at all.

### Configuration

Add the following config to `.mcp.json` or your tools equivalent.

```json
{
  "mcpServers": {
    "timberborn": {
      "type": "http",
      "url": "http://localhost:8787/mcp",
      "headers": {
        "Authorization": "Bearer <your-token>"
      }
    }
  }
}
```

## Building tools

A tool is just a class implementing `IMcpTool`, bound with Bindito's `MultiBind`:

```csharp
public class WeatherTool : IMcpTool {
  public string Name => "get_weather";
  public string Description => "Returns the current in-game weather state.";
  public JObject InputSchema => new() { ["type"] = "object", ["properties"] = new JObject() };
  public McpToolAnnotations Annotations => new() { ReadOnlyHint = true };

  public Task<McpToolResult> InvokeAsync(JObject arguments, McpToolContext context) {
    return Task.FromResult(McpToolResult.Text("Sunny. Beavers are pleased."));
  }
}

[Context("Game")]
public class MyModMcpConfigurator : Configurator {
  protected override void Configure() {
    MultiBind<IMcpTool>().To<WeatherTool>().AsSingleton();
  }
}
```

`manifest.json`:

```json
{
  ...,
  "RequiredMods": [
    {
      "Id": "Agroqirax.TimberbornMCP",
      "MinimumVersion": "1.1.2.0.1"
    }
  ]
}
```

`*.asmdef`:

```json
{
  ...,
  "precompiledReferences": [
    "TimberbornMCP.dll"
  ]
}

```

`IMcpResource` follows the same pattern via `MultiBind<IMcpResource>()`. Bind in additional
`[Context(...)]` blocks (`MainMenu`, `MapEditor`) if a tool should work outside an active save.
Game-state access must happen on the main thread — call it through
`context.MainThread.RunOnMainThread(...)` from inside `InvokeAsync`.

## License

GPL-3.0 — see [LICENSE](LICENSE).
