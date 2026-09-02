# Architecture

Notes from building this mod, kept here because most of it isn't discoverable just by reading this
mod's own code — it's about how the Timberborn/Bindito host behaves underneath it. Traced against
the decompiled game (`Timberborn.SingletonSystem`, `Bindito.Core`, `Bindito.Unity`,
`Timberborn.HttpApiSystem`) and against `eMkaQQ/timberborn-modding` (ModSettings).

## 1. Bindito context lifecycle

Mods write `Configurator : Bindito.Core.Configurator` classes tagged with one or more
`[Context("Bootstrapper"|"MainMenu"|"Game"|"MapEditor")]` attributes (`AllowMultiple = true`, so one
class can stack several when its `Configure()` body is identical across them).

**`Bootstrapper` is a true process-lifetime root container**, not just "the first context to load":
`Bindito.Unity.ProjectConfigurator.Awake()` calls `Object.DontDestroyOnLoad(gameObject)` *before*
building the container, and its `SingletonLifecycleUnityAdapter` (the MonoBehaviour that drives
`Load()`/`Unload()`/`UpdateSingleton()`) lives on that same surviving GameObject. So a singleton
bound in `[Context("Bootstrapper")]` has `Load()` called exactly once per process, before Main Menu
even loads, and `Unload()` only at process exit.

`MainMenu`/`Game`/`MapEditor` are **child** containers, rebuilt fresh every time that scene loads
(`Bindito.Unity.SceneConfigurator` does `projectContainer.CreateChildContainer(this)`). A singleton
bound there gets a new `Load()`/`Unload()` cycle every scene transition.

**Child containers don't automatically see parent bindings.** A Bootstrapper binding is only
resolvable from a child context if declared `.AsExported()` (`Bindito.Core.IExportAssignee`,
returned by `.AsSingleton()`/`.AsTransient()`). This mod's `BootstrapperConfigurator` exports
`McpToolRegistry`, `McpServerSettingsState`, and `GameSceneCache` for exactly this reason —
everything else it binds is Bootstrapper-internal only.

This is why the MCP server itself (`McpServerHost`) is bound in `Bootstrapper`, not `Game`: the user
wants it running continuously across Main Menu ↔ Game ↔ Map Editor, not restarted per save session.

## 2. Singleton lifecycle interfaces

`Timberborn.SingletonSystem`: `ILoadableSingleton.Load()`, `IUnloadableSingleton.Unload()`,
`IUpdatableSingleton.UpdateSingleton()` (once per Unity frame), `IPostLoadableSingleton.PostLoad()`.
A container's `SingletonLifecycleService` discovers these by reflecting on the constructed
*instance*, so binding `Bind<Foo>().AsSingleton()` is enough — no separate registration call needed,
as long as `Foo` is the type you `Bind<>()`, not a different interface key pointing at it. (We rely
on `IEnumerable<T>` constructor injection working with zero bindings too — confirmed in
`Bindito.Core.Internal.MultiBindingService`: any `IEnumerable<T>` parameter is treated as
multi-bound regardless of whether `MultiBind<T>()` was ever called for that container, resolving to
an empty collection rather than throwing.)

## 3. HTTP server pattern

`McpServerHost` mirrors `Timberborn.HttpApiSystem.HttpApi` deliberately: a plain
`System.Net.HttpListener`, requests pulled one at a time via `await listener.GetContextAsync()`
inside a `Task.Run(...)` background loop, so it never touches Unity APIs directly. JSON via
`Newtonsoft.Json` (already used elsewhere in the game — no new dependency). The one place Unity
state is needed (the active scene name, for `get_game_scene`) is read on the main thread once and
cached into a lock-free field (`GameSceneCache`) rather than reached for from the HTTP thread —
`IMainThreadDispatcher` exists for future tools that need to do more than that (see §5).

## 4. ModSettings integration — the persistence gotcha

`ModSettings.Core.ModSettingsOwner.Load()` does, in order: `OnBeforeLoad()` →
`InitializePropertyModSettings()` → `RegisterModSettingOwner()` → `OnAfterLoad()`.
`InitializePropertyModSettings()` reads each `ModSetting<T>` via
`_settings.GetString(key, modSetting.DefaultValue)` (etc.) and **only afterwards** subscribes
`modSetting.ValueChanged += (_, value) => valueSetter(key, value)`.

`Timberborn.SettingsSystem.Settings.GetString(key, defaultValue)` is a bare
`PlayerPrefs.GetString(key, defaultValue)` pass-through — it does **not** persist the default when
the key is missing. Combined with the ordering above: **a default value is never itself written
back**, only later *changes* are. Since a `ModSettingsOwner` subclass is reconstructed fresh every
Main Menu / Game / Map Editor scene load, a token computed as `Guid.NewGuid()` in a C# field
initializer would silently regenerate on every single scene transition, never settling.

`TimberbornMcpSettings` works around this itself, in its own constructor, before deferring to the
base class: check `ISettings.Has(key)` and `SetString` a freshly generated token exactly once if
missing, using the *same* key format `ModSettingsOwner` computes internally
(`$"ModSetting.{ModId}.{type.Name}.{propertyInfo.Name}"`) — so the base class's own later read just
confirms the value we already persisted, rather than being a second source of truth.

## 5. Extensibility contract

`Api/IMcpTool` and `Api/IMcpResource` are the entire extension surface. A mod — ours or a
third-party one — implements one of these and `MultiBind<IMcpTool>().To<TheirTool>().AsSingleton()`
in its own `[Context("Game")]` (or `MainMenu`/`MapEditor`) `Configurator`. `McpToolRegistrationBridge`
(bound once per context by `McpToolsConfigurator`) constructor-injects `IEnumerable<IMcpTool>` /
`IEnumerable<IMcpResource>` from that same container and registers/unregisters them with the
process-lifetime `McpToolRegistry` on `Load()`/`Unload()`. Our own shipped tool
(`Tools/GetGameSceneTool`) goes through this exact same path — there is no first-party-only
registration route. See `README.md` for the third-party integration snippet.

`IMainThreadDispatcher` (`Api/IMainThreadDispatcher`, implemented by `Server/MainThreadDispatcher`)
is shipped now, unused by `get_game_scene`, so that future tools needing to touch Unity/game state
(saves, main-menu navigation, entity queries) have a ready-made bridge without inventing their own
main-thread queue.

## 6. Deliberate spec deviations

- **Auth**: a static `Authorization: Bearer <token>` check (constant-time compare), not the MCP
  spec's OAuth-based auth flow — this is a local single-player game, and the user explicitly wanted
  a simple reconfigurable token, not an OAuth server.
- **No SSE / server-initiated notifications**: `GET /mcp` returns `405`. Every MCP interaction here
  is a synchronous tool call; nothing currently needs to push unsolicited notifications to the
  client.
- **No `prompts` capability, no resource subscriptions.**
- Default port **8787** — deliberately not 8080/8081 (the base game's own `HttpApi` listen port and
  its default outbound-webhook target, respectively).
