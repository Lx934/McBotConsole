using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace JBSS261A
{
    public class ProbeResult
    {
        public string Method;
        public bool Success;
        public long ElapsedMs;
        public string Summary;
        public Dictionary<string, string> Details = new Dictionary<string, string>();
        public string Error;
    }

    public class McServerInfo
    {
        public bool Online;
        public long LatencyMs;
        public string VersionName;
        public int ProtocolVersion;
        public string Motd;
        public int PlayersOnline;
        public int PlayersMax;
        public List<string> PlayerSample = new List<string>();
        public byte[] FaviconPng;
        public List<string> Mods = new List<string>();
        public List<string> Plugins = new List<string>();
        public bool EnforcesSecureChat;
        public string RawJson;
    }

    public static class NetworkTool
    {
        // ==================== DNS ====================
        public static ProbeResult ProbeDns(string host)
        {
            var r = new ProbeResult { Method = "DNS 解析" };
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (IPAddress.TryParse(host, out var ip))
                {
                    r.Success = true;
                    r.Summary = $"已是 IP: {ip}";
                    r.Details["IP"] = ip.ToString();
                }
                else
                {
                    var addrs = Dns.GetHostAddresses(host);
                    var list = new List<string>();
                    foreach (var a in addrs) list.Add(a.ToString());
                    r.Success = addrs.Length > 0;
                    r.Details["解析结果"] = string.Join(", ", list);
                    r.Details["数量"] = addrs.Length.ToString();
                    r.Summary = r.Success ? $"解析到 {addrs.Length} 个 IP" : "无结果";
                }
            }
            catch (Exception ex)
            {
                r.Success = false;
                r.Error = ex.Message;
                r.Summary = "失败: " + ex.Message;
            }
            r.ElapsedMs = sw.ElapsedMilliseconds;
            return r;
        }

        // ==================== ICMP Ping ====================
        public static ProbeResult ProbePing(string host, int timeoutMs = 3000)
        {
            var r = new ProbeResult { Method = "PING (ICMP)" };
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send(host, timeoutMs);
                    if (reply.Status == IPStatus.Success)
                    {
                        r.Success = true;
                        r.Summary = $"可达，延迟 {reply.RoundtripTime}ms";
                        r.Details["IP"] = reply.Address.ToString();
                        if (reply.Options != null) r.Details["TTL"] = reply.Options.Ttl.ToString();
                        r.Details["往返"] = reply.RoundtripTime + " ms";
                    }
                    else
                    {
                        r.Success = false;
                        r.Summary = "不可达: " + reply.Status;
                        r.Error = reply.Status.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                r.Success = false;
                r.Error = ex.Message;
                r.Summary = "Ping 失败: " + ex.Message;
            }
            r.ElapsedMs = sw.ElapsedMilliseconds;
            return r;
        }

        // ==================== TCP 端口 ====================
        public static ProbeResult ProbeTcp(string host, int port, int timeoutMs = 3000)
        {
            var r = new ProbeResult { Method = "TCP 端口" };
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                using (var client = new TcpClient())
                {
                    var task = client.ConnectAsync(host, port);
                    bool ok = task.Wait(timeoutMs);
                    if (ok && client.Connected)
                    {
                        r.Success = true;
                        r.Summary = $"端口 {port} 开放";
                        r.Details["目标"] = $"{host}:{port}";
                    }
                    else
                    {
                        r.Success = false;
                        r.Summary = $"端口 {port} 未开放或超时";
                        r.Error = "ConnectFailed";
                    }
                }
            }
            catch (Exception ex)
            {
                r.Success = false;
                r.Error = ex.Message;
                r.Summary = "TCP 失败: " + ex.Message;
            }
            r.ElapsedMs = sw.ElapsedMilliseconds;
            return r;
        }

        // ==================== HTTP ====================
        public static ProbeResult ProbeHttp(string url, int timeoutMs = 5000)
        {
            var r = new ProbeResult { Method = "HTTP" };
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                    url = "http://" + url;

                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    ServerCertificateCustomValidationCallback = (a, b, c, d) => true
                };
                using (var http = new HttpClient(handler))
                {
                    http.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
                    var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.Add("User-Agent", "JBSS261A-ServerInfo/1.0");

                    var resp = http.SendAsync(req).Result;
                    r.Success = true;
                    r.Summary = $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}";
                    r.Details["状态码"] = ((int)resp.StatusCode).ToString();
                    if (!string.IsNullOrEmpty(resp.Headers.Server.ToString()))
                        r.Details["Server"] = resp.Headers.Server.ToString();
                    if (resp.Content.Headers.ContentType != null)
                        r.Details["Content-Type"] = resp.Content.Headers.ContentType.ToString();
                    if (resp.Content.Headers.ContentLength.HasValue)
                        r.Details["Content-Length"] = resp.Content.Headers.ContentLength.Value.ToString();
                }
            }
            catch (Exception ex)
            {
                r.Success = false;
                r.Error = ex.Message;
                r.Summary = "HTTP 失败: " + ex.Message;
            }
            r.ElapsedMs = sw.ElapsedMilliseconds;
            return r;
        }

        // ==================== MC SLP ====================
        public static McServerInfo ProbeMinecraftSlp(string host, int port, int timeoutMs = 5000)
        {
            var info = new McServerInfo();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            TcpClient client = null;
            try
            {
                client = new TcpClient();
                if (!client.ConnectAsync(host, port).Wait(timeoutMs))
                    throw new TimeoutException("连接超时");

                using (var stream = client.GetStream())
                {
                    stream.ReadTimeout = timeoutMs;
                    stream.WriteTimeout = timeoutMs;

                    using (var ms = new MemoryStream())
                    {
                        WriteVarInt(ms, 0x00);
                        WriteVarInt(ms, -1);
                        WriteString(ms, host);
                        WriteUShort(ms, (ushort)port);
                        WriteVarInt(ms, 1);
                        WritePacket(stream, ms.ToArray());
                    }

                    using (var ms = new MemoryStream())
                    {
                        WriteVarInt(ms, 0x00);
                        WritePacket(stream, ms.ToArray());
                    }

                    int len = ReadVarInt(stream);
                    if (len <= 0 || len > 1024 * 1024) throw new Exception("响应长度异常");
                    byte[] buf = ReadExactly(stream, len);
                    using (var ms = new MemoryStream(buf))
                    {
                        int pid = ReadVarInt(ms);
                        if (pid != 0x00) throw new Exception("意外的 packet id");
                        info.RawJson = ReadString(ms);
                    }

                    info.LatencyMs = sw.ElapsedMilliseconds;
                    info.Online = true;
                    ParseSlpJson(info);
                }
            }
            catch
            {
                info.Online = false;
            }
            finally
            {
                try { client?.Close(); } catch { }
            }
            return info;
        }

        private static void ParseSlpJson(McServerInfo info)
        {
            try
            {
                using (var doc = JsonDocument.Parse(info.RawJson))
                {
                    var root = doc.RootElement;

                    if (root.TryGetProperty("version", out var ver))
                    {
                        if (ver.TryGetProperty("name", out var vn)) info.VersionName = vn.GetString();
                        if (ver.TryGetProperty("protocol", out var vp)) info.ProtocolVersion = vp.GetInt32();
                    }

                    if (root.TryGetProperty("players", out var p))
                    {
                        if (p.TryGetProperty("online", out var po)) info.PlayersOnline = po.GetInt32();
                        if (p.TryGetProperty("max", out var pm)) info.PlayersMax = pm.GetInt32();
                        if (p.TryGetProperty("sample", out var ps) && ps.ValueKind == JsonValueKind.Array)
                            foreach (var s in ps.EnumerateArray())
                                if (s.TryGetProperty("name", out var n))
                                    info.PlayerSample.Add(n.GetString());
                    }

                    if (root.TryGetProperty("description", out var desc))
                        info.Motd = ExtractText(desc);

                    if (root.TryGetProperty("favicon", out var fav))
                    {
                        string favStr = fav.GetString();
                        if (!string.IsNullOrEmpty(favStr) && favStr.StartsWith("data:image/png;base64,"))
                        {
                            try
                            {
                                info.FaviconPng = Convert.FromBase64String(
                                    favStr.Substring("data:image/png;base64,".Length));
                            }
                            catch { }
                        }
                    }

                    if (root.TryGetProperty("enforcesSecureChat", out var esc) && esc.ValueKind == JsonValueKind.True)
                        info.EnforcesSecureChat = true;

                    // Forge/Fabric 老格式
                    if (root.TryGetProperty("modinfo", out var mi) &&
                        mi.TryGetProperty("modList", out var ml) && ml.ValueKind == JsonValueKind.Array)
                        foreach (var m in ml.EnumerateArray())
                            if (m.TryGetProperty("modid", out var mid))
                                info.Mods.Add(mid.GetString());

                    // Forge FML2 格式
                    if (root.TryGetProperty("forgeData", out var fd) &&
                        fd.TryGetProperty("mods", out var fm) && fm.ValueKind == JsonValueKind.Array)
                        foreach (var m in fm.EnumerateArray())
                            if (m.TryGetProperty("modId", out var mid))
                                info.Mods.Add(mid.GetString());
                }
            }
            catch { }
        }

        private static string ExtractText(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.String) return el.GetString();
            if (el.ValueKind == JsonValueKind.Array)
            {
                var sb = new StringBuilder();
                foreach (var c in el.EnumerateArray()) sb.Append(ExtractText(c));
                return sb.ToString();
            }
            if (el.ValueKind == JsonValueKind.Object)
            {
                var sb = new StringBuilder();
                if (el.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                    sb.Append(t.GetString());
                if (el.TryGetProperty("extra", out var ex))
                    sb.Append(ExtractText(ex));
                return sb.ToString();
            }
            return "";
        }

        // ==================== MC Query (UDP) ====================
        public static McServerInfo ProbeMinecraftQuery(string host, int port, int timeoutMs = 3000)
        {
            var info = new McServerInfo();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            UdpClient udp = null;
            try
            {
                var addrs = Dns.GetHostAddresses(host);
                if (addrs.Length == 0) throw new Exception("DNS 无结果");

                udp = new UdpClient();
                udp.Client.ReceiveTimeout = timeoutMs;
                udp.Connect(new IPEndPoint(addrs[0], port));

                int sessionId = new Random().Next(0, 0x7FFFFFFF);
                byte[] sessBytes = BitConverter.GetBytes(sessionId);
                if (BitConverter.IsLittleEndian) Array.Reverse(sessBytes);

                byte[] handshake = new byte[7];
                handshake[0] = 0xFE; handshake[1] = 0xFD; handshake[2] = 0x09;
                Array.Copy(sessBytes, 0, handshake, 3, 4);
                udp.Send(handshake, handshake.Length);

                var remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] resp = udp.Receive(ref remote);
                if (resp.Length < 5 || resp[0] != 0x09) throw new Exception("握手失败");

                byte[] stat = new byte[11];
                stat[0] = 0xFE; stat[1] = 0xFD; stat[2] = 0x00;
                Array.Copy(sessBytes, 0, stat, 3, 4);
                udp.Send(stat, stat.Length);

                byte[] data = udp.Receive(ref remote);
                if (data.Length < 16 || data[0] != 0x00) throw new Exception("响应异常");

                info.LatencyMs = sw.ElapsedMilliseconds;
                info.Online = true;

                int start = 5;
                var parts = new List<string>();
                for (int i = 5; i < data.Length; i++)
                {
                    if (data[i] == 0)
                    {
                        if (i == start)
                        {
                            if (i + 1 < data.Length && data[i + 1] == 0) break;
                            parts.Add("");
                            start = i + 1;
                            continue;
                        }
                        parts.Add(Encoding.UTF8.GetString(data, start, i - start));
                        start = i + 1;
                    }
                }

                // parts: hostname, gametype, game_id, version, plugins, map, numplayers, maxplayers, hostport, hostip
                if (parts.Count >= 5 && !string.IsNullOrEmpty(parts[4]))
                    foreach (var pl in parts[4].Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                        info.Plugins.Add(pl.Trim());
                if (parts.Count >= 4 && string.IsNullOrEmpty(info.VersionName))
                    info.VersionName = parts[3];
                if (parts.Count >= 8)
                {
                    if (int.TryParse(parts[6], out int on)) info.PlayersOnline = on;
                    if (int.TryParse(parts[7], out int mx)) info.PlayersMax = mx;
                }
            }
            catch
            {
                info.Online = false;
            }
            finally
            {
                try { udp?.Close(); } catch { }
            }
            return info;
        }

        // ==================== VarInt / 编解码 ====================
        private static void WriteVarInt(Stream s, int value)
        {
            unchecked
            {
                uint v = (uint)value;
                while ((v & ~0x7Fu) != 0)
                {
                    s.WriteByte((byte)((v & 0x7F) | 0x80));
                    v >>= 7;
                }
                s.WriteByte((byte)v);
            }
        }

        private static int ReadVarInt(Stream s)
        {
            int numRead = 0, result = 0;
            byte read;
            do
            {
                int b = s.ReadByte();
                if (b < 0) throw new EndOfStreamException();
                read = (byte)b;
                result |= (read & 0x7F) << (7 * numRead);
                numRead++;
                if (numRead > 5) throw new Exception("VarInt 过长");
            } while ((read & 0x80) != 0);
            return result;
        }

        private static void WriteString(Stream s, string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            WriteVarInt(s, bytes.Length);
            s.Write(bytes, 0, bytes.Length);
        }

        private static string ReadString(Stream s)
        {
            int len = ReadVarInt(s);
            if (len < 0 || len > 1024 * 1024) throw new Exception("String 过长");
            return Encoding.UTF8.GetString(ReadExactly(s, len));
        }

        private static void WriteUShort(Stream s, ushort v)
        {
            s.WriteByte((byte)(v >> 8));
            s.WriteByte((byte)(v & 0xFF));
        }

        private static byte[] ReadExactly(Stream s, int n)
        {
            byte[] buf = new byte[n];
            int off = 0;
            while (off < n)
            {
                int r = s.Read(buf, off, n - off);
                if (r <= 0) throw new EndOfStreamException();
                off += r;
            }
            return buf;
        }

        private static void WritePacket(Stream s, byte[] payload)
        {
            using (var hdr = new MemoryStream())
            {
                WriteVarInt(hdr, payload.Length);
                var hb = hdr.ToArray();
                s.Write(hb, 0, hb.Length);
                s.Write(payload, 0, payload.Length);
            }
        }
    }
}