using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Timberborn.Modding;
using TimberbornMCP.Api;

namespace TimberbornMCP.Tools {

  // Reads Timberborn.Modding.ModRepository, whose Mods array is already load-order-sorted by
  // ModSorter - so load_index here is exactly the order the game itself loaded mods in.
  public class GetModListTool : IMcpTool {

    private readonly ModRepository _modRepository;

    public GetModListTool(ModRepository modRepository) {
      _modRepository = modRepository;
    }

    public string Name => "get_modlist";

    public string Description => "Returns every installed mod (enabled or not), in load order, with its manifest details.";

    public JObject InputSchema => new() {
      ["type"] = "object",
      ["properties"] = new JObject()
    };

    public McpToolAnnotations Annotations => new() {
      Title = "Get Mod List",
      ReadOnlyHint = true,
      DestructiveHint = false,
      IdempotentHint = true,
      OpenWorldHint = false
    };

    public Task<McpToolResult> InvokeAsync(JObject arguments, McpToolContext context) {
      var mods = new JArray();
      var modArray = _modRepository.Mods;
      for (var i = 0; i < modArray.Length; i++) {
        mods.Add(ToJson(modArray[i], i));
      }
      var payload = new JObject { ["mods"] = mods };
      return Task.FromResult(McpToolResult.Text(payload.ToString(Formatting.None)));
    }

    private static JObject ToJson(Mod mod, int loadIndex) {
      var manifest = mod.Manifest;
      return new JObject {
        ["load_index"] = loadIndex,
        ["enabled"] = mod.IsEnabled,
        ["name"] = manifest.Name,
        ["id"] = manifest.Id,
        ["version"] = manifest.Version.Full,
        ["description"] = manifest.Description,
        ["minimum_game_version"] = manifest.MinimumGameVersion.Full,
        ["is_user_mod"] = mod.ModDirectory.IsUserMod,
        ["required_mods"] = ToVersionedModArray(manifest.RequiredMods),
        ["optional_mods"] = ToVersionedModArray(manifest.OptionalMods)
      };
    }

    private static JArray ToVersionedModArray(System.Collections.Immutable.ImmutableArray<VersionedMod> versionedMods) {
      var array = new JArray();
      foreach (var versionedMod in versionedMods) {
        array.Add(new JObject {
          ["id"] = versionedMod.Id,
          ["minimum_version"] = versionedMod.MinimumVersion.Full
        });
      }
      return array;
    }

  }

}
