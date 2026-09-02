using Bindito.Core;
using TimberbornMCP.Api;
using TimberbornMCP.Server;

namespace TimberbornMCP {

  // Fires exactly once per process, before Main Menu even loads (Bindito's "Bootstrapper" context
  // lives on the DontDestroyOnLoad root container - see ARCHITECTURE.md). This is what makes the
  // MCP server an always-on, process-lifetime service instead of something tied to a save session.
  [Context("Bootstrapper")]
  public class BootstrapperConfigurator : Configurator {

    protected override void Configure() {
      // Exported: resolved directly by singletons bound in child (MainMenu/Game/MapEditor)
      // contexts - the tool registration bridge and the ModSettings owner.
      Bind<McpToolRegistry>().AsSingleton().AsExported();
      Bind<McpServerSettingsState>().AsSingleton().AsExported();
      Bind<GameSceneCache>().AsSingleton().AsExported();

      // Not exported: only consumed internally by McpServerHost, itself Bootstrapper-scoped.
      Bind<McpSessionStore>().AsSingleton();
      Bind<MainThreadDispatcher>().AsSingleton();

      Bind<McpServerHost>().AsSingleton();
    }

  }

}
