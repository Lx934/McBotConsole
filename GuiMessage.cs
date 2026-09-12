using System.Text.Json.Serialization;

namespace JBSS261A
{
    public class GuiMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("level")]
        public string Level { get; set; }

        [JsonPropertyName("time")]
        public string Time { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("started")]
        public bool? Started { get; set; }

        [JsonPropertyName("stopped")]
        public bool? Stopped { get; set; }

        [JsonPropertyName("paused")]
        public bool? Paused { get; set; }

        [JsonPropertyName("success")]
        public int? Success { get; set; }

        [JsonPropertyName("failure")]
        public int? Failure { get; set; }

        [JsonPropertyName("attempts")]
        public int? Attempts { get; set; }

        [JsonPropertyName("active")]
        public int? Active { get; set; }

        [JsonPropertyName("retryDelay")]
        public int? RetryDelay { get; set; }

        [JsonPropertyName("connectionInterval")]
        public int? ConnectionInterval { get; set; }

        [JsonPropertyName("nameDictEnabled")]
        public bool? NameDictEnabled { get; set; }

        [JsonPropertyName("node")]
        public PluginInfo[] Node { get; set; }

        [JsonPropertyName("python")]
        public PluginInfo[] Python { get; set; }

        [JsonPropertyName("botId")]
        public int? BotId { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("botSuccess")]
        public bool? BotSuccess { get; set; }

        [JsonPropertyName("botErrorType")]
        public string BotErrorType { get; set; }

        [JsonPropertyName("botError")]
        public string BotError { get; set; }

        [JsonPropertyName("botDurationMs")]
        public long? BotDurationMs { get; set; }

        [JsonPropertyName("reconnectCount")]
        public int? ReconnectCount { get; set; }

        [JsonPropertyName("totalAttempts")]
        public int? TotalAttempts { get; set; }

        [JsonPropertyName("successCount")]
        public int? SuccessCount { get; set; }

        [JsonPropertyName("failureCount")]
        public int? FailureCount { get; set; }

        [JsonPropertyName("host")]
        public string Host { get; set; }

        [JsonPropertyName("port")]
        public int? Port { get; set; }

        [JsonPropertyName("plugins")]
        public string[] Plugins { get; set; }

        [JsonPropertyName("cmdCount")]
        public int? CmdCount { get; set; }

        [JsonPropertyName("probeSuccess")]
        public bool? ProbeSuccess { get; set; }

        [JsonPropertyName("probeError")]
        public string ProbeError { get; set; }

        [JsonPropertyName("antiBotReason")]
        public string AntiBotReason { get; set; }

        [JsonPropertyName("proxyEnabled")]
        public bool? ProxyEnabled { get; set; }

        // ★ 配置快照
        [JsonPropertyName("cfg")]
        public ConfigState Cfg { get; set; }
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
        [JsonPropertyName("proxyList")] public string[] ProxyList { get; set; }
        [JsonPropertyName("useNameDict")] public bool? UseNameDict { get; set; }
    }

    public class PluginInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("author")]
        public string Author { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("permission")]
        public int Permission { get; set; }
    }
}