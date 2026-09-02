using System;

namespace TimberbornMCP.Server {

  public sealed class McpSession {

    public string Id { get; }

    public string ProtocolVersion { get; }

    public DateTime CreatedAtUtc { get; }

    public McpSession(string id, string protocolVersion) {
      Id = id;
      ProtocolVersion = protocolVersion;
      CreatedAtUtc = DateTime.UtcNow;
    }

  }

}
