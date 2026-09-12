using System;
using System.Net;
using System.Net.Sockets;

namespace JBSS261A
{
    /// <summary>
    /// 纯 DNS 解析。不使用 ping / ICMP，只查域名对应的 IP。
    /// </summary>
    public static class DnsHelper
    {
        /// <summary>
        /// 解析主机名。若传入的已是 IP，则原样返回。
        /// 支持 "host:port" 形式，会自动剥掉端口部分。
        /// </summary>
        /// <param name="hostOrIp">域名或 IP（可带 :port）</param>
        /// <param name="error">失败时的错误信息</param>
        /// <returns>成功返回 IPv4 字符串（优先）或 IPv6；失败返回 null</returns>
        public static string ResolveHost(string hostOrIp, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(hostOrIp))
            {
                error = "主机名为空";
                return null;
            }

            // 剥掉端口（"mc.example.com:25565" → "mc.example.com"）
            string host = hostOrIp.Trim();
            int colon = host.IndexOf(':');
            // IPv6 地址形如 [::1]:25565，单独处理
            if (host.StartsWith("["))
            {
                int end = host.IndexOf(']');
                if (end > 0) host = host.Substring(1, end - 1);
            }
            else if (colon > 0 && host.IndexOf(':') == host.LastIndexOf(':'))
            {
                host = host.Substring(0, colon);
            }

            // 已经是 IP？
            if (IPAddress.TryParse(host, out var directIp))
                return directIp.ToString();

            try
            {
                var addrs = Dns.GetHostAddresses(host);
                if (addrs == null || addrs.Length == 0)
                {
                    error = "DNS 返回空结果";
                    return null;
                }

                // 优先 IPv4
                foreach (var a in addrs)
                    if (a.AddressFamily == AddressFamily.InterNetwork)
                        return a.ToString();

                // 退回 IPv6
                return addrs[0].ToString();
            }
            catch (SocketException ex)
            {
                error = $"DNS 解析失败: {ex.Message}";
                return null;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// 判断一个字符串是不是纯 IP（不是域名）
        /// </summary>
        public static bool IsIpAddress(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            string host = s.Trim();
            int colon = host.IndexOf(':');
            if (colon > 0 && host.IndexOf(':') == host.LastIndexOf(':'))
                host = host.Substring(0, colon);
            return IPAddress.TryParse(host, out _);
        }
    }
}