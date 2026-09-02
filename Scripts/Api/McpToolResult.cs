using System.Collections.Generic;

namespace TimberbornMCP.Api {

  public sealed class McpToolResult {

    public IReadOnlyList<McpContent> Content { get; }

    public bool IsError { get; }

    public McpToolResult(IReadOnlyList<McpContent> content, bool isError = false) {
      Content = content;
      IsError = isError;
    }

    public static McpToolResult Text(string text) {
      return new McpToolResult(new[] { McpContent.CreateText(text) });
    }

    public static McpToolResult Error(string message) {
      return new McpToolResult(new[] { McpContent.CreateText(message) }, isError: true);
    }

  }

  public sealed class McpContent {

    public string Type { get; }

    public string Text { get; }

    private McpContent(string type, string text) {
      Type = type;
      Text = text;
    }

    public static McpContent CreateText(string text) {
      return new McpContent("text", text);
    }

  }

}
