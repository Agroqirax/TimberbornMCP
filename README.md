# Timberborn MCP

Give an LLM a socket into your Timberborn game.

This mod adds a [Model Context Protocol](https://modelcontextprotocol.io) server. Point any MCP client (claude, opencode, etc...) at `http://localhost:8787/mcp`, and it can query and act on the running game over plain HTTP.

TimberbornMCP itself only provides the plumbing (protocol, sessions, auth, demo tools) and a public extension point. The tools are meant to come from other mods.

## Features

- **Everything built in**: no external process to run.
- **Always running**: main menu, in a save, map editor. Started automatically on load, stopped on quit.
- **Live-configurable**: change the port or token via [Mod Settings](https://steamcommunity.com/sharedfiles/filedetails/?id=3283831040) at any point.
- **Optional bearer-token auth**: On by default with a randomly generated token.
- **Open extension API**: any mod can register `IMcpTool` & `IMcpResource` with a couple lines of Bindito.

## Quick start

### Installation

- [Steam workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=xxxxxxx): Click subscribe
- [Mod.io](https://mod.io/g/timberborn/m/timberborn-mcp): Download & extract to `~/Documents/Timberborn/Mods/timberbornmcp`.
- [GitHub](https://github.com/agroqirax/timberbornmcp/releases/latest): Download & extract to `~/Documents/Timberborn/Mods/timberbornmcp`.

Open ModSettings and note the port (default `8787`) and auth token under "Timberborn MCP".

Point your MCP client at `http://localhost:8787/mcp` (Streamable HTTP transport).

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

TimberbornMCP itself only provides the plumbing. See [docs/building-tools.md](docs/building-tools.md)
for how to write and register your own `IMcpTool`/`IMcpResource` implementations.

## License

GPL-3.0 — see [LICENSE](LICENSE).
