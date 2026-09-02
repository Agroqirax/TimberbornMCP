using System;
using System.Threading.Tasks;

namespace TimberbornMCP.Api {

  // Bridge for tools/resources that need to touch Unity or game state, which is only safe from
  // Unity's main thread - the MCP HTTP server itself runs entirely on background threads.
  public interface IMainThreadDispatcher {

    Task<T> RunOnMainThread<T>(Func<T> work);

    Task RunOnMainThread(Action work);

  }

}
