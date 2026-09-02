using System.Threading.Tasks;

namespace TimberbornMCP.Api {

  public interface IMcpResource {

    string Uri { get; }

    string Name { get; }

    string Description { get; }

    string MimeType { get; }

    Task<McpResourceContents> ReadAsync(McpResourceContext context);

  }

  public sealed class McpResourceContents {

    public string Uri { get; }

    public string MimeType { get; }

    public string Text { get; }

    public McpResourceContents(string uri, string mimeType, string text) {
      Uri = uri;
      MimeType = mimeType;
      Text = text;
    }

  }

}
