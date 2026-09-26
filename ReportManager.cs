using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace JBSS261A
{
    public class RunReport
    {
        public string Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt { get; set; }
        public string Server { get; set; }
        public string Version { get; set; }
        public int TotalAttempts { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public double SuccessRate { get; set; }
        public double AvgLatencyMs { get; set; }
        public List<BotRecord> Bots { get; set; } = new List<BotRecord>();
    }

    public class BotRecord
    {
        public int BotId { get; set; }
        public string Username { get; set; }
        public bool Success { get; set; }
        public string ErrorType { get; set; }
        public string Error { get; set; }
        public long DurationMs { get; set; }
        public int ReconnectCount { get; set; }
        public DateTime Time { get; set; }
    }

    public static class ReportManager
    {
        public static string GetReportsDir(string backendDir)
        {
            string dir = Path.Combine(backendDir, "reports");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        public static string Save(RunReport report, string backendDir)
        {
            try
            {
                string dir = GetReportsDir(backendDir);
                string fileName = string.Format("report_{0:yyyyMMdd_HHmmss}_{1}.json",
                                                report.StartedAt, report.Id);
                string path = Path.Combine(dir, fileName);

                var opts = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(report, opts);
                File.WriteAllText(path, json, new UTF8Encoding(false));
                return path;
            }
            catch
            {
                return null;
            }
        }

        public static List<RunReport> LoadAll(string backendDir)
        {
            var list = new List<RunReport>();
            try
            {
                string dir = GetReportsDir(backendDir);
                foreach (var file in Directory.GetFiles(dir, "report_*.json"))
                {
                    try
                    {
                        var json = File.ReadAllText(file, Encoding.UTF8);
                        var r = JsonSerializer.Deserialize<RunReport>(json);
                        if (r != null)
                        {
                            r.Id = r.Id ?? Path.GetFileNameWithoutExtension(file);
                            list.Add(r);
                        }
                    }
                    catch { }
                }
                list.Sort((a, b) => b.StartedAt.CompareTo(a.StartedAt));
            }
            catch { }
            return list;
        }

        public static void Delete(RunReport report, string backendDir)
        {
            try
            {
                string dir = GetReportsDir(backendDir);
                foreach (var f in Directory.GetFiles(dir, "report_*.json"))
                {
                    if (f.Contains(report.Id))
                    {
                        File.Delete(f);
                        return;
                    }
                }
            }
            catch { }
        }

        public static void ExportCsv(RunReport report, string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BotID,Username,Success,ErrorType,Error,DurationMs,ReconnectCount,Time");

            foreach (var b in report.Bots)
            {
                sb.Append(b.BotId).Append(',');
                sb.Append(EscapeCsv(b.Username)).Append(',');
                sb.Append(b.Success ? "成功" : "失败").Append(',');
                sb.Append(EscapeCsv(b.ErrorType)).Append(',');
                sb.Append(EscapeCsv(b.Error)).Append(',');
                sb.Append(b.DurationMs).Append(',');
                sb.Append(b.ReconnectCount).Append(',');
                sb.Append(b.Time.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine();
            }

            File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(true));
        }

        public static void ExportHtml(RunReport report, string outputPath)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"zh-CN\"><head><meta charset=\"UTF-8\">");
            sb.AppendLine("<title>运行报告 - " + report.StartedAt.ToString("yyyy-MM-dd HH:mm:ss") + "</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:'Microsoft YaHei UI',sans-serif;background:#f5f5f5;margin:0;padding:24px;color:#222;}");
            sb.AppendLine(".card{background:#fff;border-radius:8px;padding:20px;margin-bottom:16px;box-shadow:0 1px 3px rgba(0,0,0,.08);}");
            sb.AppendLine("h1{margin:0 0 12px 0;font-size:20px;}");
            sb.AppendLine("h2{margin:0 0 12px 0;font-size:16px;color:#0a5f99;}");
            sb.AppendLine("table{width:100%;border-collapse:collapse;font-size:13px;}");
            sb.AppendLine("th,td{padding:8px 10px;border-bottom:1px solid #eee;text-align:left;}");
            sb.AppendLine("th{background:#f8f8f8;color:#555;font-weight:bold;}");
            sb.AppendLine("tr:hover{background:#fafafa;}");
            sb.AppendLine(".ok{color:#0a8a4e;font-weight:bold;}");
            sb.AppendLine(".err{color:#c93030;font-weight:bold;}");
            sb.AppendLine(".grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:12px;}");
            sb.AppendLine(".kv{padding:10px 14px;background:#f8f8f8;border-radius:6px;}");
            sb.AppendLine(".kv .k{color:#888;font-size:12px;}");
            sb.AppendLine(".kv .v{font-size:18px;font-weight:bold;margin-top:4px;}");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<div class=\"card\">");
            sb.AppendLine("<h1>📊 Minecraft 批量登录运行报告</h1>");
            sb.AppendLine("<div class=\"grid\">");
            AppendKv(sb, "开始时间", report.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            AppendKv(sb, "结束时间", report.FinishedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            AppendKv(sb, "服务器", report.Server ?? "");
            AppendKv(sb, "游戏版本", report.Version ?? "");
            AppendKv(sb, "总尝试", report.TotalAttempts.ToString());
            AppendKv(sb, "成功", report.SuccessCount.ToString());
            AppendKv(sb, "失败", report.FailureCount.ToString());
            AppendKv(sb, "成功率", report.SuccessRate.ToString("F2") + "%");
            AppendKv(sb, "平均耗时", report.AvgLatencyMs.ToString("F0") + " ms");
            sb.AppendLine("</div></div>");

            sb.AppendLine("<div class=\"card\">");
            sb.AppendLine("<h2>详细记录 (" + report.Bots.Count + " 条)</h2>");
            sb.AppendLine("<table><thead><tr>");
            sb.AppendLine("<th>#</th><th>玩家名</th><th>结果</th><th>错误类型</th><th>耗时(ms)</th><th>重连</th><th>时间</th>");
            sb.AppendLine("</tr></thead><tbody>");

            foreach (var b in report.Bots)
            {
                sb.Append("<tr>");
                sb.Append("<td>").Append(b.BotId).Append("</td>");
                sb.Append("<td>").Append(HtmlEscape(b.Username)).Append("</td>");
                sb.Append("<td>").Append(b.Success ? "<span class=\"ok\">成功</span>" : "<span class=\"err\">失败</span>").Append("</td>");
                sb.Append("<td>").Append(HtmlEscape(b.ErrorType ?? "")).Append("</td>");
                sb.Append("<td>").Append(b.DurationMs).Append("</td>");
                sb.Append("<td>").Append(b.ReconnectCount > 0 ? b.ReconnectCount.ToString() : "-").Append("</td>");
                sb.Append("<td>").Append(b.Time.ToString("HH:mm:ss")).Append("</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table></div>");
            sb.AppendLine("<div style=\"text-align:center;color:#aaa;font-size:12px;margin-top:12px;\">Minecraft 批量登录客户端 v7.0.0</div>");
            sb.AppendLine("</body></html>");

            File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(false));
        }

        private static void AppendKv(StringBuilder sb, string k, string v)
        {
            sb.Append("<div class=\"kv\"><div class=\"k\">")
              .Append(HtmlEscape(k))
              .Append("</div><div class=\"v\">")
              .Append(HtmlEscape(v ?? ""))
              .Append("</div></div>");
        }

        private static string EscapeCsv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        private static string HtmlEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;");
        }
    }
}