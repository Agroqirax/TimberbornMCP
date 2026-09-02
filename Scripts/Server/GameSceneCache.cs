using System.Text.RegularExpressions;
using Timberborn.SingletonSystem;
using UnityEngine.SceneManagement;

namespace TimberbornMCP.Server {

  // Bootstrapper-scoped. UnityEngine.SceneManagement is main-thread-only, so this caches the
  // current scene name into a lock-free field that the MCP HTTP background thread can read
  // directly, instead of every tool needing its own main-thread hop just to know the scene.
  public class GameSceneCache : ILoadableSingleton {

    // Timberborn's own build scenes are named "<buildIndex>-<Name>Scene" (e.g. "1-MainMenuScene",
    // confirmed against the decompiled scene indices: MainMenu=1, Game=2, MapEditor=4). Stripped
    // generically rather than mapped name-by-name, so it also normalizes any scene we don't
    // explicitly know about (a scene that doesn't match the pattern is returned unchanged).
    private static readonly Regex ScenePrefixPattern = new(@"^\d+-");

    private const string SceneSuffix = "Scene";

    private volatile string _currentSceneName = "";

    public string CurrentSceneName => _currentSceneName;

    public void Load() {
      _currentSceneName = Normalize(SceneManager.GetActiveScene().name);
      SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene previous, Scene next) {
      _currentSceneName = Normalize(next.name);
    }

    private static string Normalize(string rawSceneName) {
      var name = ScenePrefixPattern.Replace(rawSceneName, "");
      if (name.Length > SceneSuffix.Length && name.EndsWith(SceneSuffix)) {
        name = name.Substring(0, name.Length - SceneSuffix.Length);
      }
      return name;
    }

  }

}
