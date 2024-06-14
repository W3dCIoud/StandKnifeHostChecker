using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

class Program
{
    private static readonly string apiKey = "yourapikey";
    private static readonly string apiUrl = "https://api.uptimerobot.com/v2/getMonitors";
    private static readonly string logsUrl = "https://api.uptimerobot.com/v2/getMonitors";
    private static readonly string monitorUrl = "standknife.store";
    private static readonly string discordWebhookUrl = "yourdiscordwebhook";
    private static readonly HttpClient client = new HttpClient();

    static async Task Main(string[] args)
    {
        while (true)
        {
            var status = await CheckMonitorStatus();
            await SendLogToDiscord(status);
            await Task.Delay(60000);
        }
    }

    static async Task<MonitorStatus> CheckMonitorStatus()
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("api_key", apiKey),
            new KeyValuePair<string, string>("format", "json"),
            new KeyValuePair<string, string>("logs", "1")
        });

        HttpResponseMessage response = await client.PostAsync(apiUrl, content);
        string responseBody = await response.Content.ReadAsStringAsync();

        var json = JsonConvert.DeserializeObject<UptimeRobotResponse>(responseBody);
        var monitor = Array.Find(json.Monitors, m => m.Url.Contains(monitorUrl));

        string reason = null;
        if (monitor.Status != 2)
        {
            var logContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("api_key", apiKey),
                new KeyValuePair<string, string>("format", "json"),
                new KeyValuePair<string, string>("logs", "1"),
                new KeyValuePair<string, string>("monitor_id", monitor.Id.ToString())
            });

            HttpResponseMessage logResponse = await client.PostAsync(logsUrl, logContent);
            string logResponseBody = await logResponse.Content.ReadAsStringAsync();
            var logJson = JsonConvert.DeserializeObject<UptimeRobotResponse>(logResponseBody);
            var monitorLog = Array.Find(logJson.Monitors, m => m.Id == monitor.Id);
            reason = monitorLog.Logs.Length > 0 ? monitorLog.Logs[0].Reason.Detail : "Unknown reason";
        }

        MonitorStatus status = new MonitorStatus
        {
            Name = monitor.FriendlyName,
            Url = monitor.Url,
            IsOnline = monitor.Status == 2,
            StatusText = monitor.Status == 2 ? "Online" : "Offline",
            Reason = reason,
            Color = monitor.Status == 2 ? 0x00FF00 : 0xFF0000
        };

        return status;
    }

    static async Task SendLogToDiscord(MonitorStatus status)
    {
        var embed = new
        {
            title = $"StandKnife server status",
            description = $"StandKnife host is currently {status.StatusText}!",
            color = status.Color,
            fields = new[]
            {
                new { name = "Reason:", value = status.Reason ?? "No details available", inline = false }
            },
            timestamp = DateTime.UtcNow.ToString("o")
        };

        var payload = new
        {
            embeds = new[] { embed }
        };

        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        await client.PostAsync(discordWebhookUrl, content);
    }
}

public class UptimeRobotResponse
{
    [JsonProperty("stat")]
    public string Stat { get; set; }

    [JsonProperty("monitors")]
    public Monitor[] Monitors { get; set; }
}

public class Monitor
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("friendly_name")]
    public string FriendlyName { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("status")]
    public int Status { get; set; }

    [JsonProperty("logs")]
    public Log[] Logs { get; set; }
}

public class Log
{
    [JsonProperty("type")]
    public int Type { get; set; }

    [JsonProperty("datetime")]
    public string DateTime { get; set; }

    [JsonProperty("reason")]
    public Reason Reason { get; set; }
}

public class Reason
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("detail")]
    public string Detail { get; set; }
}

public class MonitorStatus
{
    public string Name { get; set; }
    public string Url { get; set; }
    public bool IsOnline { get; set; }
    public string StatusText { get; set; }
    public string Reason { get; set; }
    public int Color { get; set; }
}
