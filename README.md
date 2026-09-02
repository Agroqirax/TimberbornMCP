# Timberborn MCP

Runs an [MCP](https://modelcontextprotocol.io) (Model Context Protocol) server inside Timberborn,
so an LLM/agent can inspect and drive the game over HTTP. The server is a harness: this mod owns
the transport, protocol handling, sessions, and auth, and ships one example tool
(`get_game_scene`) — everything else, first-party or third-party, plugs in the same way.

Requires [ModSettings](https://github.com/eMkaQQ/timberborn-modding) (`eMka.ModSettings`).

## Configuration

Open the mod's settings (gear icon in ModSettings, or in-game — settings here are editable both
from the main menu and mid-save):

- **Port** — the MCP server's listen port. Defaults to `8787` (the base game's own HTTP API uses
  `8080`, so this deliberately isn't that or `8081`). Changing it live-restarts the listener, no
  game restart needed.
- **AuthToken** — a random token is generated the first time the mod ever runs. Leave it as-is (or
  edit/clear it) directly in the text field:
  - **blank** → no authentication required.
  - **non-blank** → every request must send `Authorization: Bearer <token>`.

The server starts automatically as soon as the mod loads and keeps running for the whole session —
main menu, in a save, map editor — until you quit the game.

## Talking to it

Single endpoint, `POST http://localhost:<port>/mcp`, using the MCP **Streamable HTTP** transport.

```bash
# 1. Initialize - grab the Mcp-Session-Id response header.
curl -si http://localhost:8787/mcp \
  -H 'Content-Type: application/json' \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{}}}'

# 2. List tools (include the session id from step 1).
curl -s http://localhost:8787/mcp \
  -H 'Content-Type: application/json' -H 'Mcp-Session-Id: <id-from-step-1>' \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list"}'

# 3. Call one.
curl -s http://localhost:8787/mcp \
  -H 'Content-Type: application/json' -H 'Mcp-Session-Id: <id-from-step-1>' \
  -d '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"get_game_scene","arguments":{}}}'
```

If a token is configured, add `-H 'Authorization: Bearer <token>'` to every call, `initialize`
included.

## Adding your own tool (from another mod)

No special API beyond implementing an interface and a standard Bindito `MultiBind` — the harness
doesn't treat its own tools any differently.

**Your `manifest.json`:**

```json
{
  "RequiredMods": [
    { "Id": "Agroqirax.TimberbornMCP", "MinimumVersion": "1.0.0.0" }
  ]
}
```

**Your `.asmdef`:** add `"TimberbornMCP.dll"` to `precompiledReferences` (it sits alongside the
other built mod DLLs once TimberbornMCP is built/deployed, same as any `Timberborn.*.dll`).

**Your tool + Configurator:**

```csharp
using Bindito.Core;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using TimberbornMCP.Api;

public class WeatherTool : IMcpTool {
  public string Name => "get_weather";
  public string Description => "Returns the current in-game weather state.";
  public JObject InputSchema => new() { ["type"] = "object", ["properties"] = new JObject() };

  // Optional (return null for none) - descriptive hints per the MCP tool annotations spec, surfaced
  // in tools/list. Not security-enforced, just a courtesy to clients (e.g. whether to confirm before calling).
  public McpToolAnnotations Annotations => new() { ReadOnlyHint = true, OpenWorldHint = false };

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

That's the whole integration surface. Bind in `[Context("MainMenu")]`/`[Context("MapEditor")]` too
if your tool should be available outside an active save. `IMcpResource` works the same way via
`MultiBind<IMcpResource>()`.

If your tool needs to touch Unity or game state (which is only safe from the main thread, while
the MCP server itself runs on a background thread), use `context.MainThread.RunOnMainThread(...)`.

See `ARCHITECTURE.md` for how this all fits into Timberborn's Bindito/scene lifecycle, and for
known spec simplifications (static bearer-token auth instead of MCP's OAuth flow, no SSE/streaming,
no `prompts` capability).
