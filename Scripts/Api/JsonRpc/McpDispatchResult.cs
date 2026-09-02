using Newtonsoft.Json.Linq;
using TimberbornMCP.Server;

namespace TimberbornMCP.Api.JsonRpc {

  // Transport-agnostic result of dispatching one JSON-RPC message. McpServerHost turns this into
  // the actual HTTP response (status code, Mcp-Session-Id header, body).
  internal sealed class McpDispatchResult {

    public JObject ResponseBody;

    public bool IsNotification;

    public McpSession NewSession;

  }

}
