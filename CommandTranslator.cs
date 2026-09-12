using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JBSS261A
{
    /// <summary>
    /// 把用户输入的中文/口语翻译成后端能识别的命令
    /// 翻译不了就原样返回，让高级用户继续用英文命令
    /// </summary>
    public static class CommandTranslator
    {
        // 中文关键词 → 命令模板（{0} 是参数占位）
        private static readonly List<(string[] Keywords, string Template)> Rules
            = new List<(string[], string)>
        {
            // ===== 基础控制 =====
            (new[] { "帮助", "怎么用", "help", "使用说明" }, "help"),
            (new[] { "退出", "关闭", "结束程序" }, "exit"),
            (new[] { "暂停" }, "stop"),
            (new[] { "继续", "恢复", "运行" }, "run"),
            (new[] { "状态", "看状态", "现在怎么样" }, "status"),
            (new[] { "清空", "清屏", "清空日志" }, "clear"),

            // ===== 配置 =====
            (new[] { "查看配置", "显示配置", "看配置" }, "config show"),
            (new[] { "保存配置", "保存设置" }, "config save"),
            (new[] { "设置IP", "改IP", "修改IP", "ip" }, "config ip {0}"),
            (new[] { "设置端口", "改端口", "修改端口", "端口" }, "config port {0}"),
            (new[] { "设置次数", "改次数", "连接次数", "次数" }, "config count {0}"),
            (new[] { "设置并发", "改并发", "并发数", "并发" }, "config concurrency {0}"),
            (new[] { "设置前缀", "改前缀", "玩家名前缀", "前缀" }, "config prefix {0}"),
            (new[] { "设置版本", "改版本", "游戏版本", "版本" }, "config version {0}"),

            // ===== 插件 =====
            (new[] { "插件列表", "看插件", "插件" }, "plugins"),
            (new[] { "重载所有插件", "重启所有插件", "重载全部" }, "reloadall"),

            // ===== 日志 =====
            (new[] { "打包日志", "日志打包", "导出日志" }, "packlog"),
        };

        /// <summary>
        /// 翻译输入。翻译不出来返回原字符串
        /// </summary>
        public static string Translate(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            string s = input.Trim();

            // 如果本身就是纯 ASCII 命令，直接放行
            if (Regex.IsMatch(s, @"^[a-zA-Z][a-zA-Z0-9_\- ]*$") &&
                !ContainsChinese(s))
            {
                return s;
            }

            foreach (var (keywords, template) in Rules)
            {
                foreach (var kw in keywords)
                {
                    // 完全匹配：直接返回
                    if (s.Equals(kw, StringComparison.OrdinalIgnoreCase))
                    {
                        return template.Contains("{0}") ? null : template;
                    }

                    // 前缀匹配：提取参数
                    if (s.StartsWith(kw, StringComparison.OrdinalIgnoreCase))
                    {
                        string rest = s.Substring(kw.Length).Trim(' ', '：', ':', '=', '的');
                        if (template.Contains("{0}"))
                        {
                            if (string.IsNullOrEmpty(rest)) return null; // 缺参数
                            return string.Format(template, rest);
                        }
                        // 模板不需要参数，但用户给了 → 也返回模板
                        return template;
                    }

                    // 中间带"设成/改成/设为/改为"的写法：例如 "IP 设成 1.2.3.4"
                    var m = Regex.Match(s,
                        kw + @"\s*(?:设成|改成|设为|改为|设置为|等于|=|:|：)\s*(.+)$",
                        RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        if (template.Contains("{0}"))
                            return string.Format(template, m.Groups[1].Value.Trim());
                        return template;
                    }
                }
            }

            // 特殊：重载某插件 / 卸载某插件
            var mReload = Regex.Match(s, @"^(?:重载|重启)\s*(?:插件)?\s*(.+)$");
            if (mReload.Success)
                return "reload " + mReload.Groups[1].Value.Trim();

            var mUnload = Regex.Match(s, @"^卸载\s*(?:插件)?\s*(.+)$");
            if (mUnload.Success)
                return "unload " + mUnload.Groups[1].Value.Trim();

            var mPerm = Regex.Match(s, @"^(?:权限|设置权限)\s*(.+?)\s+([0-3])$");
            if (mPerm.Success)
                return $"permission {mPerm.Groups[1].Value.Trim()} {mPerm.Groups[2].Value}";

            // 翻译不了 → 原样返回（后端会提示"未知命令"）
            return s;
        }

        /// <summary>
        /// 给"开始"等按钮用的中文命令映射（这些是发给后端控制器的，不是命令）
        /// </summary>
        public static bool IsExitCommand(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();
            return s == "退出" || s == "关闭" || s == "结束程序" || s == "exit";
        }

        private static bool ContainsChinese(string s)
        {
            foreach (char c in s)
                if (c >= 0x4E00 && c <= 0x9FFF) return true;
            return false;
        }
    }
}