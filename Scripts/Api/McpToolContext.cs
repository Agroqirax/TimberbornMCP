using System.Threading;

namespace TimberbornMCP.Api {

  public sealed class McpToolContext {

    public IMainThreadDispatcher MainThread { get; }

    public CancellationToken CancellationToken { get; }

    public McpToolContext(IMainThreadDispatcher mainThread, CancellationToken cancellationToken) {
      MainThread = mainThread;
      CancellationToken = cancellationToken;
    }

  }

  public sealed class McpResourceContext {

    public IMainThreadDispatcher MainThread { get; }

    public CancellationToken CancellationToken { get; }

    public McpResourceContext(IMainThreadDispatcher mainThread, CancellationToken cancellationToken) {
      MainThread = mainThread;
      CancellationToken = cancellationToken;
    }

  }

}
