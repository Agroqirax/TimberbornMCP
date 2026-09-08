# Building tools and resources

TimberbornMCP only provides the protocol plumbing. Tools and resources — the things an MCP
client can actually call — are meant to come from other mods, including your own.

## Extension point

The entire extension mechanism is `MultiBind<IMcpTool>()` / `MultiBind<IMcpResource>()` inside
your own Bindito `Configurator`. `McpToolRegistrationBridge` walks every scene's container
(`MainMenu`, `Game`, `MapEditor`) and registers whatever it finds there — your mod never needs to
reference any TimberbornMCP registry type directly.

### `manifest.json`

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

### `*.asmdef`

```json
{
  ...,
  "precompiledReferences": [
    "TimberbornMCP.dll"
  ]
}
```

## Writing a tool

A tool is a class implementing `IMcpTool`:

```csharp
public interface IMcpTool {
  string Name { get; }
  string Description { get; }
  JObject InputSchema { get; }
  McpToolAnnotations Annotations { get; }   // optional, return null for none
  Task<McpToolResult> InvokeAsync(JObject arguments, McpToolContext context);
}
```

- `Name` — the tool's identifier as seen by MCP clients.
- `Description` — shown to the LLM; describe what it does and when to use it.
- `InputSchema` — a JSON Schema object describing `arguments`.
- `Annotations` — hints from the [MCP tool annotations spec](https://modelcontextprotocol.io)
  (`ReadOnlyHint`, `DestructiveHint`, `IdempotentHint`, `OpenWorldHint`, `Title`). These are hints,
  not guarantees — a well-behaved client may use them to decide whether to prompt for
  confirmation, but must not rely on them for security.
- `InvokeAsync` — do the work and return an `McpToolResult`.

Example:

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

Bind in additional `[Context(...)]` blocks (`MainMenu`, `MapEditor`) if a tool should also work
outside an active save.

### Returning results

`McpToolResult` wraps the content sent back to the client:

```csharp
McpToolResult.Text("some string");         // success
McpToolResult.Error("what went wrong");    // IsError = true
```

For structured data, serialize a `JObject`/`JArray` to a string with `Newtonsoft.Json` and pass it
to `McpToolResult.Text`, as `GetVersionTool` does:

```csharp
var payload = new JObject { ["version"] = version.Full };
return Task.FromResult(McpToolResult.Text(payload.ToString(Formatting.None)));
```

### Main-thread access

The MCP HTTP server runs entirely on background threads, but Unity/game state is only safe to
touch from the main thread. Use `context.MainThread` from inside `InvokeAsync`:

```csharp
public async Task<McpToolResult> InvokeAsync(JObject arguments, McpToolContext context) {
  var result = await context.MainThread.RunOnMainThread(() => {
    // touch Unity/game state here
    return SomeGameState.Value;
  });
  return McpToolResult.Text(result.ToString());
}
```

`RunOnMainThread` also has an `Action`-returning overload for work with no return value.

`McpToolContext` additionally exposes a `CancellationToken` for the current call.

## Writing a resource

Resources are read-only, addressed by URI, and don't take arguments:

```csharp
public interface IMcpResource {
  string Uri { get; }
  string Name { get; }
  string Description { get; }
  string MimeType { get; }
  Task<McpResourceContents> ReadAsync(McpResourceContext context);
}
```

`ReadAsync` returns an `McpResourceContents(uri, mimeType, text)`. `McpResourceContext` mirrors
`McpToolContext` (`MainThread`, `CancellationToken`).

Register the same way, via `MultiBind<IMcpResource>().To<YourResource>().AsSingleton()`.

## Built-in tools

TimberbornMCP ships a few tools of its own as both working examples and general-purpose
utilities — see `Scripts/Tools/` (`get_version`, `get_modlist`, `get_game_scene`, and friends).
Read one of those for a complete, working reference implementation.
