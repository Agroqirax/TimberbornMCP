using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Timberborn.SingletonSystem;
using TimberbornMCP.Api;
using TimberbornMCP.Api.JsonRpc;
using UnityEngine;

namespace TimberbornMCP.Server {

  // Process-lifetime (Bootstrapper-scoped) owner of the MCP HTTP endpoint. Mirrors
  // Timberborn.HttpApiSystem.HttpApi's own HttpListener/background-task pattern: the request loop
  // runs entirely off the Unity main thread and never touches Unity APIs directly.
  public class McpServerHost : ILoadableSingleton, IUnloadableSingleton {

    private const string SessionHeader = "Mcp-Session-Id";

    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);

    private readonly McpServerSettingsState _settingsState;

    private readonly McpSessionStore _sessions;

    private readonly McpProtocolHandler _protocolHandler;

    private readonly object _listenerLock = new();

    private HttpListener _listener;

    private Task _pumpTask;

    private volatile bool _stopping;

    public McpServerHost(McpToolRegistry registry, McpServerSettingsState settingsState,
        McpSessionStore sessions, MainThreadDispatcher mainThreadDispatcher) {
      _settingsState = settingsState;
      _sessions = sessions;
      _protocolHandler = new McpProtocolHandler(registry, sessions, mainThreadDispatcher);
    }

    public void Load() {
      _settingsState.PortChanged += OnPortChanged;
      StartAt(_settingsState.Port);
    }

    public void Unload() {
      _settingsState.PortChanged -= OnPortChanged;
      Stop();
    }

    private void OnPortChanged(object sender, EventArgs e) {
      var port = _settingsState.Port;
      Stop();
      StartAt(port);
    }

    private void StartAt(int port) {
      lock (_listenerLock) {
        try {
          var listener = new HttpListener();
          listener.Prefixes.Add($"http://localhost:{port}/");
          listener.Start();
          _listener = listener;
          _stopping = false;
          _pumpTask = Task.Run(() => ProcessRequests(listener));
          Debug.Log($"[TimberbornMCP] Listening on http://localhost:{port}/mcp");
        } catch (Exception e) {
          Debug.LogError($"[TimberbornMCP] Failed to start MCP server on port {port}: {e.Message}");
          _listener = null;
        }
      }
    }

    private void Stop() {
      lock (_listenerLock) {
        if (_listener == null) {
          return;
        }
        _stopping = true;
        _listener.Stop();
        _listener.Close();
        if (!(_pumpTask?.Wait(StopTimeout) ?? true)) {
          Debug.LogWarning("[TimberbornMCP] Timed out waiting for the MCP server task to stop.");
        }
        _listener = null;
        _pumpTask = null;
      }
    }

    private async Task ProcessRequests(HttpListener listener) {
      while (true) {
        HttpListenerContext context;
        try {
          context = await listener.GetContextAsync();
        } catch (Exception e) {
          if (!_stopping) {
            Debug.LogException(e);
          }
          break;
        }
        try {
          try {
            await HandleRequestAsync(context);
          } catch (Exception e) {
            Debug.LogException(e);
            await context.WriteText("Internal server error.", 500);
          }
        } finally {
          try {
            context.Response.OutputStream.Close();
          } catch {
            // Connection may already be gone - nothing to do.
          }
        }
      }
    }

    private async Task HandleRequestAsync(HttpListenerContext context) {
      var request = context.Request;

      if (request.Url.AbsolutePath != "/mcp") {
        await context.WriteText("Not found.", 404);
        return;
      }
      if (request.HttpMethod != "POST") {
        await context.WriteText("Method not allowed.", 405);
        return;
      }
      if (!IsAuthorized(request)) {
        context.Response.AddHeader("WWW-Authenticate", "Bearer");
        await context.WriteText("Unauthorized.", 401);
        return;
      }

      JObject requestJson;
      try {
        var body = await context.ReadBodyAsync();
        requestJson = JObject.Parse(body);
      } catch (Exception) {
        await context.WriteJson(JsonRpcMessages.Error(null, JsonRpcErrorCodes.ParseError, "Invalid JSON."), 400);
        return;
      }

      var method = (string) requestJson["method"];
      if (method != "initialize") {
        var sessionId = request.Headers[SessionHeader];
        if (!_sessions.TryGet(sessionId, out _)) {
          await context.WriteJson(
              JsonRpcMessages.Error(requestJson["id"], JsonRpcErrorCodes.SessionNotFound, "Unknown or missing session."),
              404);
          return;
        }
      }

      var result = await _protocolHandler.DispatchAsync(requestJson, CancellationToken.None);

      if (result.NewSession != null) {
        context.Response.AddHeader(SessionHeader, result.NewSession.Id);
      }

      if (result.IsNotification) {
        context.Response.StatusCode = 202;
        context.Response.Close();
        return;
      }

      await context.WriteJson(result.ResponseBody);
    }

    private bool IsAuthorized(HttpListenerRequest request) {
      var token = _settingsState.AuthToken;
      if (string.IsNullOrEmpty(token)) {
        return true;
      }
      var header = request.Headers["Authorization"];
      if (header == null || !header.StartsWith("Bearer ", StringComparison.Ordinal)) {
        return false;
      }
      var provided = header.Substring("Bearer ".Length);
      return FixedTimeEquals(provided, token);
    }

    private static bool FixedTimeEquals(string a, string b) {
      var aBytes = Encoding.UTF8.GetBytes(a);
      var bBytes = Encoding.UTF8.GetBytes(b);
      if (aBytes.Length != bBytes.Length) {
        return false;
      }
      var diff = 0;
      for (var i = 0; i < aBytes.Length; i++) {
        diff |= aBytes[i] ^ bBytes[i];
      }
      return diff == 0;
    }

  }

}
