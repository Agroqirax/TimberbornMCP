using Bindito.Core;
using TimberbornMCP.Api;
using TimberbornMCP.Tools;

namespace TimberbornMCP {

  // Third-party mods do not need to reference McpToolRegistrationBridge at all: they just
  // MultiBind<IMcpTool>() in their own [Context("Game")] (etc.) Configurator, and whichever
  // context's bridge is already bound here picks their tool up automatically.
  [Context("MainMenu")]
  [Context("Game")]
  [Context("MapEditor")]
  public class McpToolsConfigurator : Configurator {

    protected override void Configure() {
      Bind<McpToolRegistrationBridge>().AsSingleton();
      MultiBind<IMcpTool>().To<GetGameSceneTool>().AsSingleton();
    }

  }

}
