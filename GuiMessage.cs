using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JBSS261A
{
    public class GuiMessage
    {
        [JsonPropertyName("type")] public string Type { get; set; }

        // log
        [JsonPropertyName("level")] public string Level { get; set; }
        [JsonPropertyName("time")] public string Time { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; }

        // status
        [JsonPropertyName("started")] public bool? Started { get; set; }
        [JsonPropertyName("stopped")] public bool? Stopped { get; set; }
        [JsonPropertyName("paused")] public bool? Paused { get; set; }
        [JsonPropertyName("success")] public int? Success { get; set; }
        [JsonPropertyName("failure")] public int? Failure { get; set; }
        [JsonPropertyName("attempts")] public int? Attempts { get; set; }
        [JsonPropertyName("active")] public int? Active { get; set; }
        [JsonPropertyName("retryDelay")] public int? RetryDelay { get; set; }
        [JsonPropertyName("connectionInterval")] public int? ConnectionInterval { get; set; }
        [JsonPropertyName("nameDictEnabled")] public bool? NameDictEnabled { get; set; }
        [JsonPropertyName("playerPoolEnabled")] public bool? PlayerPoolEnabled { get; set; }

        // plugin-list
        [JsonPropertyName("node")] public PluginInfo[] Node { get; set; }
        [JsonPropertyName("python")] public PluginInfo[] Python { get; set; }

        // bot-result
        [JsonPropertyName("botId")] public int? BotId { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("botSuccess")] public bool? BotSuccess { get; set; }
        [JsonPropertyName("botErrorType")] public string BotErrorType { get; set; }
        [JsonPropertyName("botError")] public string BotError { get; set; }
        [JsonPropertyName("botDurationMs")] public long? BotDurationMs { get; set; }
        [JsonPropertyName("reconnectCount")] public int? ReconnectCount { get; set; }

        // run-finished
        [JsonPropertyName("totalAttempts")] public int? TotalAttempts { get; set; }
        [JsonPropertyName("successCount")] public int? SuccessCount { get; set; }
        [JsonPropertyName("failureCount")] public int? FailureCount { get; set; }

        // config-state
        [JsonPropertyName("cfg")] public ConfigState Cfg { get; set; }

        // debug-result
        // (message / level 已在上方定义)

        // plugin-probe-result
        [JsonPropertyName("host")] public string Host { get; set; }
        [JsonPropertyName("port")] public int? Port { get; set; }
        [JsonPropertyName("probeSuccess")] public bool? ProbeSuccess { get; set; }
        [JsonPropertyName("plugins")] public string[] Plugins { get; set; }
        [JsonPropertyName("cmdCount")] public int? CmdCount { get; set; }
        [JsonPropertyName("probeError")] public string ProbeError { get; set; }

        // ★ 已删除 antibot-detected 相关字段
    }

    public class PluginInfo
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("version")] public string Version { get; set; }
        [JsonPropertyName("author")] public string Author { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("usage")] public string Usage { get; set; }
        [JsonPropertyName("permission")] public int Permission { get; set; }
    }

    public class ConfigState
    {
        [JsonPropertyName("ip")] public string Ip { get; set; }
        [JsonPropertyName("port")] public int? Port { get; set; }
        [JsonPropertyName("version")] public string Version { get; set; }
        [JsonPropertyName("count")] public int? Count { get; set; }
        [JsonPropertyName("concurrency")] public int? Concurrency { get; set; }
        [JsonPropertyName("retryDelay")] public int? RetryDelay { get; set; }
        [JsonPropertyName("connectionInterval")] public int? ConnectionInterval { get; set; }
        [JsonPropertyName("connectionTimeout")] public int? ConnectionTimeout { get; set; }
        [JsonPropertyName("prefix")] public string Prefix { get; set; }
        [JsonPropertyName("stayConnected")] public bool? StayConnected { get; set; }
        [JsonPropertyName("autoDisconnectAfter")] public int? AutoDisconnectAfter { get; set; }
        [JsonPropertyName("proxyMode")] public string ProxyMode { get; set; }
        [JsonPropertyName("proxyList")] public List<string> ProxyList { get; set; }
        [JsonPropertyName("useNameDict")] public bool? UseNameDict { get; set; }
        [JsonPropertyName("usePlayerPool")] public bool? UsePlayerPool { get; set; }
        [JsonPropertyName("playerPoolExhausted")] public string PlayerPoolExhausted { get; set; }
    }
}