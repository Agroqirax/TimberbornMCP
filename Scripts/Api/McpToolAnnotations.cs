namespace TimberbornMCP.Api {

  // Optional descriptive hints from the MCP tool annotations spec. These are hints, not guarantees
  // - clients must not rely on them for security decisions - but a well-behaved host can use them
  // to decide things like whether to prompt for confirmation before calling a tool.
  public sealed class McpToolAnnotations {

    public string Title { get; init; }

    public bool? ReadOnlyHint { get; init; }

    public bool? DestructiveHint { get; init; }

    public bool? IdempotentHint { get; init; }

    public bool? OpenWorldHint { get; init; }

  }

}
