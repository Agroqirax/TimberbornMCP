using System;
using System.Collections.Concurrent;

namespace TimberbornMCP.Server {

  public class McpSessionStore {

    private readonly ConcurrentDictionary<string, McpSession> _sessions = new();

    public McpSession Create(string protocolVersion) {
      var session = new McpSession(Guid.NewGuid().ToString("N"), protocolVersion);
      _sessions[session.Id] = session;
      return session;
    }

    public bool TryGet(string id, out McpSession session) {
      if (string.IsNullOrEmpty(id)) {
        session = null;
        return false;
      }
      return _sessions.TryGetValue(id, out session);
    }

  }

}
