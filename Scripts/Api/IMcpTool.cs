using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TimberbornMCP.Api {

  // The extension point for tools. Implement this, MultiBind<IMcpTool>().To<YourTool>().AsSingleton()
  // in your own [Context("Game")] (or MainMenu/MapEditor) Configurator, and it is picked up by the
  // harness automatically - no reference to any TimberbornMCP registry/bridge type required.
  public interface IMcpTool {

    string Name { get; }

    string Description { get; }

    JObject InputSchema { get; }

    Task<McpToolResult> InvokeAsync(JObject arguments, McpToolContext context);

  }

}
