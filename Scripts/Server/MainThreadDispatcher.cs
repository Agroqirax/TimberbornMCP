using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Timberborn.SingletonSystem;
using TimberbornMCP.Api;

namespace TimberbornMCP.Server {

  // Bootstrapper-scoped. Bound by its concrete type only (never as IMainThreadDispatcher) so there
  // is exactly one instance and no ambiguity about which one the per-frame tick drains - tools get
  // handed the interface view of this same instance via McpToolContext.
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
