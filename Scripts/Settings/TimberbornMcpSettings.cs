using System;
using ModSettings.Core;
using Timberborn.Modding;
using Timberborn.SettingsSystem;
using TimberbornMCP.Server;

namespace TimberbornMCP.Settings {

  public class TimberbornMcpSettings : ModSettingsOwner {

    private const string ModIdValue = "Agroqirax.TimberbornMCP";

    private readonly McpServerSettingsState _serverSettingsState;

    public ModSetting<int> Port { get; }

    public ModSetting<string> AuthToken { get; }

    protected override string ModId => ModIdValue;

    // Default is MainMenu-only; we want the port/token editable while a save is loaded too, since
    // the server keeps running through Game/MapEditor.
    public override ModSettingsContext ChangeableOn => ModSettingsContext.All;

    public TimberbornMcpSettings(ISettings settings, ModSettingsOwnerRegistry modSettingsOwnerRegistry,
        ModRepository modRepository, McpServerSettingsState serverSettingsState)
        : base(settings, modSettingsOwnerRegistry, modRepository) {
      _serverSettingsState = serverSettingsState;

      // ModSettingsOwner reads ISettings.GetString/GetInt(key, default) but only starts persisting
      // on future *changes* - a PlayerPrefs-backed default is never itself written back. Since this
      // object is reconstructed every MainMenu/Game/MapEditor scene load, a token generated as a
      // plain field-initializer default would regenerate every single reload. So the token is
      // generated and persisted here, exactly once, before ModSettingsOwner ever reads the key.
      var tokenKey = BuildKey(nameof(AuthToken));
      if (!settings.Has(tokenKey)) {
        settings.SetString(tokenKey, Guid.NewGuid().ToString("N"));
      }
      AuthToken = new ModSetting<string>(settings.GetString(tokenKey, ""),
          ModSettingDescriptor.CreateLocalized("TimberbornMCP.Settings.AuthToken")
              .SetLocalizedTooltip("TimberbornMCP.Settings.AuthToken.Tooltip"));
      Port = new ModSetting<int>(settings.GetInt(BuildKey(nameof(Port)), McpServerSettingsState.DefaultPort),
          ModSettingDescriptor.CreateLocalized("TimberbornMCP.Settings.Port")
              .SetLocalizedTooltip("TimberbornMCP.Settings.Port.Tooltip"));
    }

    protected override void OnAfterLoad() {
      // Runs once ModSettingsOwner.Load() has populated Port/AuthToken from persisted storage -
      // guaranteed ordering (unlike wiring this from a second, independently-Load()ed singleton).
      Port.ValueChanged += (_, value) => _serverSettingsState.UpdatePort(value);
      AuthToken.ValueChanged += (_, value) => _serverSettingsState.UpdateToken(value);
      _serverSettingsState.UpdatePort(Port.Value);
      _serverSettingsState.UpdateToken(AuthToken.Value);
    }

    // Must match the key ModSettingsOwner.InitializePropertyModSetting computes internally:
    // $"ModSetting.{ModId}.{type.Name}.{propertyInfo.Name}".
    private static string BuildKey(string propertyName) {
      return $"ModSetting.{ModIdValue}.{nameof(TimberbornMcpSettings)}.{propertyName}";
    }

  }

}
