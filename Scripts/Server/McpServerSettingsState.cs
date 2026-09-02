using System;

namespace TimberbornMCP.Server {

  // Thread-safe live copy of the configured port/token. TimberbornMcpSettings (a per-scene
  // ModSettingsOwner) pushes values in here; McpServerHost (process-lifetime) reacts to changes.
  public class McpServerSettingsState {

    public const int DefaultPort = 8787;

    private readonly object _lock = new();

    private int _port = DefaultPort;

    private string _authToken = "";

    public event EventHandler PortChanged;

    public event EventHandler TokenChanged;

    public int Port {
      get { lock (_lock) { return _port; } }
    }

    public string AuthToken {
      get { lock (_lock) { return _authToken; } }
    }

    public void UpdatePort(int port) {
      bool changed;
      lock (_lock) {
        changed = _port != port;
        _port = port;
      }
      if (changed) {
        PortChanged?.Invoke(this, EventArgs.Empty);
      }
    }

    public void UpdateToken(string token) {
      token ??= "";
      bool changed;
      lock (_lock) {
        changed = _authToken != token;
        _authToken = token;
      }
      if (changed) {
        TokenChanged?.Invoke(this, EventArgs.Empty);
      }
    }

  }

}
