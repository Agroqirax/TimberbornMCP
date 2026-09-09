using System;
using ModSettings.Core;

namespace TimberbornMCP.Settings {

  // ModSetting<T>.Reset() reverts to DefaultValue, which is captured once at construction time
  // from whatever token was already persisted - so the base behavior is a no-op for a security
  // token. Resetting a token should mint a fresh random one instead.
  public class AuthTokenModSetting : ModSetting<string> {

    public AuthTokenModSetting(string defaultValue, ModSettingDescriptor descriptor)
        : base(defaultValue, descriptor) {
    }

    public override void Reset() {
      SetValue(Guid.NewGuid().ToString("N"));
    }

  }

}
