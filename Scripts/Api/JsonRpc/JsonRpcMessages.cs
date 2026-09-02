using Newtonsoft.Json.Linq;

namespace TimberbornMCP.Api.JsonRpc {

  internal static class JsonRpcMessages {

    public static JObject Success(JToken id, JToken result) {
      return new JObject {
        ["jsonrpc"] = "2.0",
        ["id"] = id,
        ["result"] = result
      };
    }

    public static JObject Error(JToken id, int code, string message) {
      return new JObject {
        ["jsonrpc"] = "2.0",
        ["id"] = id ?? JValue.CreateNull(),
        ["error"] = new JObject {
          ["code"] = code,
          ["message"] = message
        }
      };
    }

  }

}
