using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace JBSS261A
{
    public class UpdateInfo
    {
        public bool Checked { get; set; }          // 网络请求成功且找到了正式 release
        public bool HasNewVersion { get; set; }
        public string CurrentVersion { get; set; }
        public string LatestVersion { get; set; }
        public string ReleaseName { get; set; }
        public string ReleaseBody { get; set; }
        public string PublishedAt { get; set; }
        public string HtmlUrl { get; set; }
        public string Error { get; set; }
        public List<string> AppliedPatches { get; set; } = new List<string>();
    }

    public static class UpdateChecker
    {
        private const string Owner = "Lx934";
        private const string Repo = "McBotConsole";
        private const string ApiUrl =
            "https://api.github.com/repos/" + Owner + "/" + Repo + "/releases?per_page=30";

        private static readonly HttpClient _client = CreateClient();

        private static UpdateInfo _cache = null;
        private static DateTime _cacheTime = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
        private static readonly object _lock = new object();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("McBotConsole-UpdateChecker/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            return client;
        }

        public static async Task<UpdateInfo> CheckAsync(bool forceRefresh = false)
        {
            if (!forceRefresh)
            {
                lock (_lock)
                {
                    if (_cache != null && (DateTime.UtcNow - _cacheTime) < CacheDuration)
                        return _cache;
                }
            }

            var result = new UpdateInfo { CurrentVersion = VersionInfo.CURRENT_VERSION };

            try
            {
                var resp = await _client.GetAsync(ApiUrl);

                if ((int)resp.StatusCode == 403)
                {
                    result.Error = "GitHub API 速率限制（60 次/小时），请稍后重试";
                    return result;
                }
                if ((int)resp.StatusCode == 404)
                {
                    result.Error = "仓库不存在";
                    return result;
                }
                if (!resp.IsSuccessStatusCode)
                {
                    result.Error = $"GitHub API 返回 {(int)resp.StatusCode}";
                    return result;
                }

                var json = await resp.Content.ReadAsStringAsync();
                using (var doc = JsonDocument.Parse(json))
                {
                    if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    {
                        result.Error = "GitHub 返回格式异常";
                        return result;
                    }

                    JsonElement? latest = null;
                    string latestTag = null;

                    // 遍历，找第一个主版本 tag（26H1 / 26H2），跳过：
                    //   · draft / prerelease
                    //   · 补丁 tag（含 u，如 26H1u1）
                    //   · 特殊版 tag（"正式发行版" 之类）
                    foreach (var rel in doc.RootElement.EnumerateArray())
                    {
                        if (rel.TryGetProperty("draft", out var d) && d.GetBoolean()) continue;
                        if (rel.TryGetProperty("prerelease", out var p) && p.GetBoolean()) continue;

                        if (!rel.TryGetProperty("tag_name", out var t)) continue;
                        string tag = t.GetString();
                        if (string.IsNullOrEmpty(tag)) continue;

                        // 只认主版本（26H1 / 26H2）
                        if (!VersionInfo.IsMainVersion(tag)) continue;

                        latest = rel;
                        latestTag = tag;
                        break;
                    }

                    if (latest == null)
                    {
                        result.Error = "未找到正式版本 release（tag 需为 26H1 / 26H2 格式）";
                        return result;
                    }

                    var root = latest.Value;
                    result.Checked = true;
                    result.LatestVersion = latestTag;
                    result.ReleaseName = root.TryGetProperty("name", out var n) ? n.GetString() : latestTag;
                    result.ReleaseBody = root.TryGetProperty("body", out var b) ? b.GetString() : "";
                    result.PublishedAt = root.TryGetProperty("published_at", out var pa) ? pa.GetString() : "";
                    result.HtmlUrl = root.TryGetProperty("html_url", out var h) ? h.GetString() : "";

                    result.HasNewVersion = VersionInfo.IsNewer(VersionInfo.CURRENT_VERSION, latestTag);

                    if (result.HasNewVersion)
                        result.AppliedPatches = CollectAppliedPatches();
                }

                lock (_lock)
                {
                    _cache = result;
                    _cacheTime = DateTime.UtcNow;
                }
                return result;
            }
            catch (TaskCanceledException)
            {
                result.Error = "请求超时（GitHub 可能不可达）";
                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                return result;
            }
        }

        public static void ClearCache()
        {
            lock (_lock)
            {
                _cache = null;
                _cacheTime = DateTime.MinValue;
            }
        }

        /// <summary>
        /// 读 applied.jsonl，列出与当前主版本同年、H 号 ≤ 当前的已应用补丁。
        /// 这些在升级到新版本后可能失效。
        /// </summary>
        private static List<string> CollectAppliedPatches()
        {
            var list = new List<string>();
            try
            {
                string programDir = AppDomain.CurrentDomain.BaseDirectory;
                string appliedPath = System.IO.Path.Combine(programDir, "patches", "applied.jsonl");
                if (!System.IO.File.Exists(appliedPath)) return list;

                var current = VersionInfo.Parse(VersionInfo.CURRENT_VERSION);
                if (current == null) return list;

                foreach (var line in System.IO.File.ReadAllLines(appliedPath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        using (var doc = JsonDocument.Parse(line))
                        {
                            var root = doc.RootElement;
                            if (!root.TryGetProperty("patchId", out var pid)) continue;
                            string patchId = pid.GetString();
                            if (string.IsNullOrEmpty(patchId)) continue;

                            var bv = VersionInfo.Parse(VersionInfo.ExtractBaseVersion(patchId));
                            if (bv == null) continue;

                            if (bv.Year == current.Year && bv.Half <= current.Half)
                            {
                                if (!list.Contains(patchId))
                                    list.Add(patchId);
                            }
                        }
                    }
                    catch { /* 坏行跳过 */ }
                }
            }
            catch { /* 读不到就算了 */ }

            return list;
        }
    }
}