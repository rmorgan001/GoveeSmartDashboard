using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GoVeeSmartDashboard.Models;

namespace GoVeeSmartDashboard.Services;

public sealed class GoVeeApiClient
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri("https://openapi.api.GoVee.com") };

    public async Task<IReadOnlyList<GoVeeRestDevice>> GetDevicesAsync(string apiKey, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/router/api/v1/user/devices");
        req.Headers.TryAddWithoutValidation("GoVee-API-Key", apiKey);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var res = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        res.EnsureSuccessStatusCode();

        var root = JsonNode.Parse(body);
        var list = new List<GoVeeRestDevice>();
        foreach (var n in root?["data"]?.AsArray() ?? [])
        {
            if (n is null) continue;
            list.Add(new GoVeeRestDevice
            {
                Sku = n["sku"]?.GetValue<string>() ?? "",
                DeviceId = n["device"]?.GetValue<string>() ?? "",
                DeviceName = n["deviceName"]?.GetValue<string>() ?? "",
                Type = n["type"]?.GetValue<string>() ?? ""
            });
        }
        return list;
    }

    public async Task<DeviceSnapshot> GetStateAsync(string apiKey, ConfiguredDevice config, CancellationToken ct)
    {
        var payload = new { requestId = Guid.NewGuid().ToString(), payload = new { sku = config.Model, device = config.DeviceId } };
        using var req = new HttpRequestMessage(HttpMethod.Post, "/router/api/v1/device/state");
        req.Headers.TryAddWithoutValidation("GoVee-API-Key", apiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var res = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        res.EnsureSuccessStatusCode();

        var caps = JsonNode.Parse(body)?["payload"]?["capabilities"]?.AsArray();
        bool online = false; bool? power = null;
        foreach (var cap in caps ?? [])
        {
            var type = cap?["type"]?.GetValue<string>();
            var instance = cap?["instance"]?.GetValue<string>();
            var value = cap?["state"]?["value"];
            if (type == "devices.capabilities.online" || instance == "online") online = value?.GetValue<bool>() ?? false;
            if (instance == "powerSwitch") power = value?.GetValue<int>() == 1;
        }
        return new DeviceSnapshot
        {
            Name=config.Name, Model=config.Model, DeviceId=config.DeviceId, Online=online,
            PowerOn=power, Transport="REST", Detail="Smart Plug", Timestamp=DateTimeOffset.Now
        };
    }

    public async Task SetPowerAsync(string apiKey, ConfiguredDevice config, bool on, CancellationToken ct)
    {
        var payload = new
        {
            requestId = Guid.NewGuid().ToString(),
            payload = new
            {
                sku = config.Model, device = config.DeviceId,
                capability = new { type = "devices.capabilities.on_off", instance = "powerSwitch", value = on ? 1 : 0 }
            }
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, "/router/api/v1/device/control");
        req.Headers.TryAddWithoutValidation("GoVee-API-Key", apiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }
}
public sealed class GoVeeRestDevice
{
    public string Sku { get; init; } = "";
    public string DeviceId { get; init; } = "";
    public string DeviceName { get; init; } = "";
    public string Type { get; init; } = "";
}
