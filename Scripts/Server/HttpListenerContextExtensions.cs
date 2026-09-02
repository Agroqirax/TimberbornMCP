using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TimberbornMCP.Server {

  internal static class HttpListenerContextExtensions {

    public static async Task<string> ReadBodyAsync(this HttpListenerContext context) {
      using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8);
      return await reader.ReadToEndAsync();
    }

    public static async Task WriteJson(this HttpListenerContext context, JToken json, int statusCode = 200) {
      context.Response.StatusCode = statusCode;
      var bytes = Encoding.UTF8.GetBytes(json.ToString(Formatting.None));
      await context.Write("application/json; charset=utf-8", bytes);
    }

    public static async Task WriteText(this HttpListenerContext context, string text, int statusCode) {
      context.Response.StatusCode = statusCode;
      await context.Write("text/plain; charset=utf-8", Encoding.UTF8.GetBytes(text));
    }

    private static async Task Write(this HttpListenerContext context, string contentType, byte[] bytes) {
      context.Response.ContentType = contentType;
      context.Response.ContentLength64 = bytes.Length;
      await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
      context.Response.Close();
    }

  }

}
