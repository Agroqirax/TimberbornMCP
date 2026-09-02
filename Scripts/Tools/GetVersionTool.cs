using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Timberborn.Versioning;
using TimberbornMCP.Api;

namespace TimberbornMCP.Tools {

  public class GetVersionTool : IMcpTool {

    public string Name => "get_version";

    public string Description => "Returns the running Timberborn game version.";

    public JObject InputSchema => new() {
      ["type"] = "object",
      ["properties"] = new JObject()
    };

    public McpToolAnnotations Annotations => new() {
      Title = "Get Version",
      ReadOnlyHint = true,
      DestructiveHint = false,
      IdempotentHint = true,
      OpenWorldHint = false
    };

    public Task<McpToolResult> InvokeAsync(JObject arguments, McpToolContext context) {
      var version = GameVersions.CurrentVersion;
      var payload = new JObject {
        ["version"] = version.Full,
        // Version has no public IsExperimental - NumericWithBranch appends "-x" iff experimental
        // (see Timberborn.Versioning.Version), so this mirrors that without duplicating the parse.
        ["experimental"] = version.NumericWithBranch.EndsWith("-x")
      };
      return Task.FromResult(McpToolResult.Text(payload.ToString(Formatting.None)));
    }

  }

}
