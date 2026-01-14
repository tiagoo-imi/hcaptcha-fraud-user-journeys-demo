using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

public static class HttpHelpers
{
    private static readonly JsonSerializerOptions Opts = new(JsonSerializerDefaults.Web);

    public static async Task<T?> ReadJson<T>(this HttpRequestData req)
        => await JsonSerializer.DeserializeAsync<T>(req.Body, Opts);

    public static HttpResponseData Json(this HttpRequestData req, object obj, HttpStatusCode code = HttpStatusCode.OK)
    {
        var res = req.CreateResponse(code);
        res.Headers.Add("Content-Type", "application/json");
        res.WriteString(JsonSerializer.Serialize(obj));
        return res;
    }
}
