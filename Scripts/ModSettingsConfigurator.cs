using Bindito.Core;
using TimberbornMCP.Settings;

namespace TimberbornMCP {

  // Same context set as ModSettings' own ModSettingsCoreConfigurator - ModSettingsOwnerRegistry
  // (required to construct a ModSettingsOwner) is only bound in these three child contexts, not
  // in Bootstrapper.
  [Context("MainMenu")]
  [Context("Game")]
  [Context("MapEditor")]
  public class ModSettingsConfigurator : Configurator {

    protected override void Configure() {
      Bind<TimberbornMcpSettings>().AsSingleton();
    }

  }

}
