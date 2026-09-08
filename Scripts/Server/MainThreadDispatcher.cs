using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Timberborn.SingletonSystem;
using TimberbornMCP.Api;

namespace TimberbornMCP.Server {

  // Bootstrapper-scoped. Bound by its concrete type only (never as IMainThreadDispatcher) so there
  // is exactly one instance and no ambiguity about which one the per-frame tick drains - tools get
  // handed the interface view of this same instance via McpToolContext.
  //
  // Unity does not call Update/UpdateSingleton on an unfocused window unless
  // Application.runInBackground is true (it defaults to false, and we deliberately don't touch it -
  // that's a global player setting affecting the whole game's simulation, not just this mod, so it's
  // the user's call, not ours to override). That means anything genuinely routed through here still
  // stalls until the window regains focus. The fix for our own tools was to stop routing plain
  // in-memory-data tools through here at all - see ARCHITECTURE.md §5.
  public class MainThreadDispatcher : IMainThreadDispatcher, IUpdatableSingleton {

    private readonly ConcurrentQueue<Action> _pending = new();

    public Task<T> RunOnMainThread<T>(Func<T> work) {
      var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
      _pending.Enqueue(() => {
        try {
          tcs.SetResult(work());
        } catch (Exception e) {
          tcs.SetException(e);
        }
      });
      return tcs.Task;
    }

    public Task RunOnMainThread(Action work) {
      return RunOnMainThread(() => {
        work();
        return true;
      });
    }

    public void UpdateSingleton() {
      while (_pending.TryDequeue(out var action)) {
        action();
      }
    }

  }

}
