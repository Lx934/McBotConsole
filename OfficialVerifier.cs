using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace JBSS261A.Patching
{
    /// <summary>
    /// 官方来源验证器：检查补丁是否存在于 GitHub 仓库的标签列表中
    /// </summary>
    public static class OfficialVerifier
    {
        private const string REPO_OWNER = "Lx934";
        private const string REPO_NAME = "McBotConsole";
        private const string GITHUB_API = "https://api.github.com";

        public enum VerifyStatus
        {
            Official,        // 官方认证
            NotFound,        // 不在官方仓库
            NetworkError,    // 网络问题
            InvalidResponse  // 响应异常
        }

        public class VerifyResult
        {
            public VerifyStatus Status;
            public string OfficialCommitSha;
            public string OfficialTagName;
            public string Message;
            public DateTime? CheckedAt;
        }

        public static async Task<VerifyResult> VerifyAsync(string patchId, int timeoutMs = 10000)
        {
            var result = new VerifyResult
            {
                CheckedAt = DateTime.UtcNow,
                Status = VerifyStatus.NetworkError
            };

            if (string.IsNullOrWhiteSpace(patchId))
            {
                result.Status = VerifyStatus.NotFound;
                result.Message = "补丁 ID 为空";
                return result;
            }

            try
            {
                var tags = await FetchAllTagsAsync(timeoutMs);

                var matched = tags.Find(t =>
                    string.Equals(t.Name, patchId, StringComparison.OrdinalIgnoreCase));

                if (matched == null)
                {
                    result.Status = VerifyStatus.NotFound;
                    result.Message = $"补丁 {patchId} 不在官方仓库标签列表中";
                    return result;
                }

                result.Status = VerifyStatus.Official;
                result.OfficialTagName = matched.Name;
                result.OfficialCommitSha = matched.CommitSha;
                result.Message = $"官方认证通过（标签 {matched.Name}）";
                return result;
            }
            catch (TaskCanceledException)
            {
                result.Status = VerifyStatus.NetworkError;
                result.Message = "连接 GitHub 超时";
                return result;
            }
            catch (HttpRequestException ex)
            {
                result.Status = VerifyStatus.NetworkError;
                result.Message = $"网络错误：{ex.Message}";
                return result;
            }
            catch (Exception ex)
            {
                result.Status = VerifyStatus.InvalidResponse;
                result.Message = $"验证异常：{ex.Message}";
                return result;
            }
        }

        private static async Task<List<TagInfo>> FetchAllTagsAsync(int timeoutMs)
        {
            var allTags = new List<TagInfo>();
            int page = 1;
            const int perPage = 100;

            using (var http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
                http.DefaultRequestHeaders.Add("User-Agent", "McBotConsole-PatchVerifier/1.0");
                http.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
                http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

                while (true)
                {
                    string url = $"{GITHUB_API}/repos/{REPO_OWNER}/{REPO_NAME}/tags?per_page={perPage}&page={page}";
                    var response = await http.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                            throw new Exception("GitHub API 速率限制，请稍后重试");
                        throw new Exception($"GitHub API 返回 {(int)response.StatusCode}");
                    }

                    string json = await response.Content.ReadAsStringAsync();
                    var pageTags = ParseTags(json);
                    if (pageTags.Count == 0) break;

                    allTags.AddRange(pageTags);
                    if (pageTags.Count < perPage) break;
                    page++;
                }
            }

            return allTags;
        }

        private static List<TagInfo> ParseTags(string json)
        {
            var list = new List<TagInfo>();
            try
            {
                using (var doc = JsonDocument.Parse(json))
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        var tag = new TagInfo();
                        if (item.TryGetProperty("name", out var name))
                            tag.Name = name.GetString();

                        if (item.TryGetProperty("commit", out var commit) &&
                            commit.TryGetProperty("sha", out var sha))
                            tag.CommitSha = sha.GetString();

                        if (!string.IsNullOrEmpty(tag.Name))
                            list.Add(tag);
                    }
                }
            }
            catch { }
            return list;
        }

        private class TagInfo
        {
            public string Name;
            public string CommitSha;
        }
    }
}