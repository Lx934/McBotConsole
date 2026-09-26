using System;
using System.Text.RegularExpressions;

namespace JBSS261A
{
    public static class VersionInfo
    {
        /// <summary>
        /// ★ 当前程序版本。每次发布新版本改这里，重新编译。
        /// 格式：YYHn（例如 26H1、26H2）
        /// 不读系统时间，不读环境变量。
        /// </summary>
        public const string CURRENT_VERSION = "26H1";

        public class ParsedVersion : IComparable<ParsedVersion>
        {
            public int Year;      // 26
            public int Half;      // 1 或 2
            public int Update;    // 0 = 无补丁，1 = u1

            public override string ToString()
            {
                return Update > 0
                    ? $"{Year:D2}H{Half}u{Update}"
                    : $"{Year:D2}H{Half}";
            }

            public int CompareTo(ParsedVersion other)
            {
                if (other == null) return 1;
                if (Year != other.Year) return Year.CompareTo(other.Year);
                if (Half != other.Half) return Half.CompareTo(other.Half);
                return Update.CompareTo(other.Update);
            }
        }

        private static readonly Regex FullRegex =
            new Regex(@"^(\d{2})H([12])(?:u(\d+))?$", RegexOptions.Compiled);

        private static readonly Regex MainRegex =
            new Regex(@"^(\d{2})H([12])$", RegexOptions.Compiled);

        public static ParsedVersion Parse(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return null;
            var m = FullRegex.Match(version.Trim());
            if (!m.Success) return null;

            return new ParsedVersion
            {
                Year = int.Parse(m.Groups[1].Value),
                Half = int.Parse(m.Groups[2].Value),
                Update = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0
            };
        }

        /// <summary>是否是主版本（不带 u），例如 26H1 / 26H2。</summary>
        public static bool IsMainVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return false;
            return MainRegex.IsMatch(version.Trim());
        }

        /// <summary>
        /// 补丁目标版本是否与当前程序版本兼容。
        /// 规则：同年，且补丁的 H 号 ≤ 当前 H 号。跨年一律拒绝。
        /// </summary>
        public static bool IsPatchCompatible(string currentVersion, string patchTarget)
        {
            var cur = Parse(currentVersion);
            var tgt = Parse(patchTarget);
            if (cur == null || tgt == null) return false;
            if (cur.Year != tgt.Year) return false;
            if (tgt.Half > cur.Half) return false;
            return true;
        }

        /// <summary>latest 是否比 current 新。</summary>
        public static bool IsNewer(string currentVersion, string latestVersion)
        {
            var cur = Parse(currentVersion);
            var lat = Parse(latestVersion);
            if (cur == null || lat == null) return false;
            return lat.CompareTo(cur) > 0;
        }

        /// <summary>
        /// 从补丁 ID 里提取基础版本（不含 u 号）。
        /// "26H1u2" → "26H1"；"26H1" → "26H1"；无法识别 → null
        /// </summary>
        public static string ExtractBaseVersion(string patchId)
        {
            if (string.IsNullOrWhiteSpace(patchId)) return null;
            var m = Regex.Match(patchId, @"^(\d{2}H[12])");
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}