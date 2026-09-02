# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
- MCP (Model Context Protocol) server harness, running over Streamable HTTP, auto-started for the
  whole game session (main menu, in a save, map editor) and configurable via ModSettings (port,
  optional bearer-token auth).
- Extensibility API (`IMcpTool`, `IMcpResource`) so other mods can register their own tools the
  same way this mod registers its own.
- Example tool: `get_game_scene`, returning the currently active Unity scene.
- Tool annotations (`readOnlyHint`, `destructiveHint`, `idempotentHint`, `openWorldHint`, `title`)
  via `IMcpTool.Annotations`, surfaced in `tools/list`.
- Localized ModSettings labels/tooltips (`Data/Localizations/enUS.csv`).

[unreleased]: https://github.com/agroqirax/timberbornmcp/compare/v1.1.2.0.1...HEAD
[1.1.2.0.1]: https://github.com/agroqirax/timberbornmcp/releases/tag/1.1.2.0.1
