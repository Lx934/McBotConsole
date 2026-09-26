using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JBSS261A
{
    public class ServerInfoDialog : Form
    {
        private readonly string _defaultVersion;

        private TextBox txtHost;
        private TextBox txtPort;
        private TextBox txtHttpUrl;
        private CheckBox chkDns, chkPing, chkTcp, chkHttp, chkMcSlp, chkMcQuery;
        private Button btnProbe;
        private RichTextBox rtbResult;
        private PictureBox picFavicon;
        private Label lblFaviconHint;

        // 探测插件
        private Button btnProbePlugins;
        private Timer _probeTimer;
        private string _probingHost;
        private int _probingPort;

        // 由 MainForm 注入
        public Action<string> SendBackendCommand { get; set; }
        public Func<GuiMessage> GetLastProbeResult { get; set; }

        public ServerInfoDialog(string defaultHost, int defaultPort, string defaultVersion = "1.19.2")
        {
            _defaultVersion = string.IsNullOrEmpty(defaultVersion) ? "1.19.2" : defaultVersion;
            InitializeUI();
            txtHost.Text = string.IsNullOrEmpty(defaultHost) ? "mc.example.com" : defaultHost;
            txtPort.Text = defaultPort.ToString();
            txtHttpUrl.Text = "http://" + (string.IsNullOrEmpty(defaultHost) ? "mc.example.com" : defaultHost);
        }

        private void InitializeUI()
        {
            this.Text = "🔍 网址 / 服务器信息解析器";
            this.Size = new Size(980, 720);
            this.MinimumSize = new Size(800, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Microsoft YaHei UI", 9.5F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.BackColor = Color.FromArgb(245, 245, 245);

            // ============== 顶部面板 ==============
            var top = new Panel
            {
                Dock = DockStyle.Top,
                Height = 148,
                Padding = new Padding(16, 12, 16, 8),
                BackColor = Color.FromArgb(235, 235, 235)
            };

            var lblHost = new Label { Text = "主机:", Location = new Point(16, 18), AutoSize = true };
            txtHost = new TextBox { Location = new Point(70, 15), Width = 220 };

            var lblPort = new Label { Text = "端口:", Location = new Point(306, 18), AutoSize = true };
            txtPort = new TextBox { Location = new Point(350, 15), Width = 70 };

            var lblHttp = new Label { Text = "HTTP:", Location = new Point(440, 18), AutoSize = true };
            txtHttpUrl = new TextBox
            {
                Location = new Point(490, 15),
                Width = 420,
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };

            chkDns = new CheckBox { Text = "DNS 解析", Checked = true, Location = new Point(16, 55), AutoSize = true };
            chkPing = new CheckBox { Text = "PING", Checked = false, Location = new Point(120, 55), AutoSize = true };
            chkTcp = new CheckBox { Text = "TCP 端口", Checked = true, Location = new Point(200, 55), AutoSize = true };
            chkMcSlp = new CheckBox { Text = "MC 服务器信息(SLP)", Checked = true, Location = new Point(300, 55), AutoSize = true };
            chkMcQuery = new CheckBox { Text = "MC Query(查插件)", Checked = false, Location = new Point(500, 55), AutoSize = true };
            chkHttp = new CheckBox { Text = "HTTP", Checked = false, Location = new Point(680, 55), AutoSize = true };

            btnProbe = new Button
            {
                Text = "🔍 开始解析",
                Location = new Point(16, 92),
                Size = new Size(130, 34),
                FlatStyle = FlatStyle.System
            };
            btnProbe.Click += (s, e) => RunProbes();

            var btnClose = new Button
            {
                Text = "关闭",
                Location = new Point(156, 92),
                Size = new Size(60, 34),
                FlatStyle = FlatStyle.System
            };
            btnClose.Click += (s, e) => this.Close();

            btnProbePlugins = new Button
            {
                Text = "🔌 探测插件列表",
                Location = new Point(226, 92),
                Size = new Size(140, 34),
                FlatStyle = FlatStyle.System
            };
            btnProbePlugins.Click += (s, e) => ProbePluginsViaBackend();

            var lblHint = new Label
            {
                Text = "（探测插件需登录服务器，约 10~20 秒）",
                Location = new Point(376, 100),
                AutoSize = true,
                ForeColor = Color.FromArgb(120, 120, 120)
            };

            top.Controls.Add(lblHost);
            top.Controls.Add(txtHost);
            top.Controls.Add(lblPort);
            top.Controls.Add(txtPort);
            top.Controls.Add(lblHttp);
            top.Controls.Add(txtHttpUrl);
            top.Controls.Add(chkDns);
            top.Controls.Add(chkPing);
            top.Controls.Add(chkTcp);
            top.Controls.Add(chkMcSlp);
            top.Controls.Add(chkMcQuery);
            top.Controls.Add(chkHttp);
            top.Controls.Add(btnProbe);
            top.Controls.Add(btnClose);
            top.Controls.Add(btnProbePlugins);
            top.Controls.Add(lblHint);

            // ============== 结果区域 ==============
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 2,
                SplitterDistance = 740
            };

            rtbResult = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(26, 26, 26),
                ForeColor = Color.FromArgb(230, 230, 230),
                Font = new Font("Consolas", 10F)
            };
            split.Panel1.Controls.Add(rtbResult);

            var right = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(12)
            };

            lblFaviconHint = new Label
            {
                Text = "服务器图标",
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            picFavicon = new PictureBox
            {
                Dock = DockStyle.Top,
                Height = 80,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.FixedSingle
            };

            right.Controls.Add(picFavicon);
            right.Controls.Add(lblFaviconHint);
            split.Panel2.Controls.Add(right);

            this.Controls.Add(split);
            this.Controls.Add(top);

            // 探测超时清理
            this.FormClosed += (s, e) =>
            {
                try { _probeTimer?.Stop(); _probeTimer?.Dispose(); } catch { }
            };
        }

        // ============================================================
        // 常规探测（DNS / Ping / TCP / HTTP / SLP / Query）
        // ============================================================
        private void RunProbes()
        {
            string host = txtHost.Text.Trim();
            int port = int.TryParse(txtPort.Text, out int p) ? p : 25565;
            string httpUrl = txtHttpUrl.Text.Trim();

            if (string.IsNullOrEmpty(host))
            {
                MessageBox.Show(this, "请填写主机名", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            rtbResult.Clear();
            picFavicon.Image = null;
            lblFaviconHint.Text = "服务器图标";
            btnProbe.Enabled = false;

            Task.Run(() =>
            {
                Action<string> append = s =>
                {
                    BeginInvoke(new Action(() =>
                    {
                        rtbResult.SelectionStart = rtbResult.TextLength;
                        rtbResult.SelectionColor = Color.FromArgb(230, 230, 230);
                        rtbResult.AppendText(s);
                        rtbResult.SelectionStart = rtbResult.TextLength;
                        rtbResult.ScrollToCaret();
                    }));
                };

                var sw = System.Diagnostics.Stopwatch.StartNew();
                append($"=== 解析 {host}:{port} ===\n");
                append($"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n");

                if (chkDns.Checked) AppendSection(append, NetworkTool.ProbeDns(host));
                if (chkPing.Checked) AppendSection(append, NetworkTool.ProbePing(host));
                if (chkTcp.Checked) AppendSection(append, NetworkTool.ProbeTcp(host, port));
                if (chkHttp.Checked && !string.IsNullOrEmpty(httpUrl))
                    AppendSection(append, NetworkTool.ProbeHttp(httpUrl));

                if (chkMcSlp.Checked)
                {
                    append("=== MC 服务器信息 (SLP) ===\n");
                    var info = NetworkTool.ProbeMinecraftSlp(host, port);
                    if (!info.Online)
                    {
                        append("  ❌ 无法获取 SLP 信息（非 MC 服务器或已离线）\n\n");
                    }
                    else
                    {
                        append($"  ✅ 在线，延迟 {info.LatencyMs}ms\n");
                        append($"  版本: {info.VersionName} (protocol {info.ProtocolVersion})\n");
                        append($"  玩家: {info.PlayersOnline}/{info.PlayersMax}\n");

                        if (!string.IsNullOrEmpty(info.Motd))
                            append($"  MOTD: {SanitizeMotd(info.Motd)}\n");

                        append($"  正版验证: {(info.EnforcesSecureChat ? "已开启" : "未指明（多数未开启）")}\n");
                        append($"  服务端类型: {(info.Mods.Count > 0 ? $"模组服 ({info.Mods.Count} 个 mod)" : "原版或未报告 mod")}\n");

                        if (info.PlayerSample.Count > 0)
                        {
                            append($"  在线玩家样例 ({info.PlayerSample.Count}):\n");
                            foreach (var n in info.PlayerSample)
                                append($"    · {n}\n");
                        }

                        if (info.Mods.Count > 0)
                        {
                            append($"  Mod 列表 ({info.Mods.Count}):\n");
                            foreach (var m in info.Mods)
                                append($"    · {m}\n");
                        }

                        if (info.FaviconPng != null && info.FaviconPng.Length > 0)
                        {
                            try
                            {
                                using (var ms = new MemoryStream(info.FaviconPng))
                                {
                                    var img = Image.FromStream(ms);
                                    BeginInvoke(new Action(() =>
                                    {
                                        picFavicon.Image = new Bitmap(img);
                                        lblFaviconHint.Text = $"服务器图标 ({img.Width}×{img.Height})";
                                    }));
                                }
                                append($"  图标: 有 ({info.FaviconPng.Length / 1024} KB)\n");
                            }
                            catch { }
                        }
                        else
                        {
                            append("  图标: 无\n");
                        }

                        append("\n");
                    }
                }

                if (chkMcQuery.Checked)
                {
                    append("=== MC Query (插件列表) ===\n");
                    append("  说明: Query 是独立于游戏端口的查询协议，默认关闭。\n");
                    append("        只有服务端 server.properties 设了 enable-query=true 才会响应。\n");
                    append("        如果拿不到插件，可试试上面的【探测插件列表】按钮。\n");

                    var info = NetworkTool.ProbeMinecraftQuery(host, port);
                    if (!info.Online)
                    {
                        append("  ❌ Query 未响应\n");
                        append("     原因: 服务端未启用 Query（默认如此，属正常现象）。\n");
                        append("     影响: 拿不到插件列表。SLP 信息不受影响。\n");
                        append("     解决: 如果你是服主，在 server.properties 设 enable-query=true 后重启。\n\n");
                    }
                    else
                    {
                        append($"  ✅ 响应，延迟 {info.LatencyMs}ms\n");
                        if (info.Plugins.Count > 0)
                        {
                            append($"  插件列表 ({info.Plugins.Count}):\n");
                            foreach (var pl in info.Plugins)
                                append($"    · {pl}\n");
                        }
                        else
                        {
                            append("  插件列表: 空（原版或未启用插件）\n");
                        }
                        append("\n");
                    }
                }

                sw.Stop();
                append($"=== 完成，总耗时 {sw.ElapsedMilliseconds}ms ===\n");
                BeginInvoke(new Action(() => btnProbe.Enabled = true));
            });
        }

        private void AppendSection(Action<string> append, ProbeResult r)
        {
            append($"=== {r.Method} ===\n");
            append($"  {(r.Success ? "✅" : "❌")} {r.Summary}  ({r.ElapsedMs}ms)\n");
            foreach (var kv in r.Details)
                append($"  · {kv.Key}: {kv.Value}\n");
            if (!string.IsNullOrEmpty(r.Error))
                append($"  错误: {r.Error}\n");
            append("\n");
        }

        private static string SanitizeMotd(string motd)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < motd.Length; i++)
            {
                if (motd[i] == '§' && i + 1 < motd.Length) { i++; continue; }
                sb.Append(motd[i]);
            }
            return sb.ToString().Trim();
        }

        // ============================================================
        // 插件探测（通过后端登录 + Tab 补全）
        // ============================================================
        private void ProbePluginsViaBackend()
        {
            if (SendBackendCommand == null || GetLastProbeResult == null)
            {
                MessageBox.Show(this,
                    "此功能需要主程序运行中，请从主界面打开工具。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string host = txtHost.Text.Trim();
            if (string.IsNullOrWhiteSpace(host))
            {
                MessageBox.Show(this, "请先填写主机名", "提示");
                return;
            }
            int port = int.TryParse(txtPort.Text, out int p) ? p : 25565;

            _probingHost = host;
            _probingPort = port;

            AppendProbeText($"\n=== 插件探测（登录 + Tab 补全）===\n");
            AppendProbeText($"  已向后台发送探测请求: {host}:{port} 版本 {_defaultVersion}\n");
            AppendProbeText($"  此过程需登录服务器，约 10~20 秒，请等待...\n");

            SendBackendCommand($"probe-plugins {host} {port} {_defaultVersion}");

            // 启动轮询定时器
            if (_probeTimer == null)
            {
                _probeTimer = new Timer { Interval = 500 };
                _probeTimer.Tick += (s, e) => PollProbeResult();
            }
            _probeTimer.Start();
        }

        private void PollProbeResult()
        {
            try
            {
                var result = GetLastProbeResult?.Invoke();
                if (result == null) return;
                if (result.Type != "plugin-probe-result") return;
                if (!string.Equals(result.Host, _probingHost, StringComparison.OrdinalIgnoreCase)) return;
                if (result.Port.HasValue && result.Port.Value != _probingPort) return;

                // 有结果了，停止轮询
                _probeTimer.Stop();
                DisplayProbeResult(result);
            }
            catch { }
        }

        private void DisplayProbeResult(GuiMessage result)
        {
            if (result == null) return;

            if (result.ProbeSuccess != true)
            {
                AppendProbeText($"  ❌ 探测失败: {result.ProbeError ?? "未知错误"}\n\n");
                return;
            }

            var plugins = result.Plugins ?? new string[0];
            int cmdCount = result.CmdCount ?? 0;

            AppendProbeText($"  ✅ 探测完成\n");
            AppendProbeText($"     扫描命令数: {cmdCount}\n");
            AppendProbeText($"     检测到插件: {plugins.Length} 个\n");

            if (plugins.Length == 0)
            {
                AppendProbeText("     （未检测到插件命名空间，可能：服务器原版 / 版本低于 1.13 / 过滤了补全）\n\n");
            }
            else
            {
                AppendProbeText("\n");
                foreach (var pl in plugins)
                    AppendProbeText($"     · {pl}\n");
                AppendProbeText("\n");
            }
        }

        private void AppendProbeText(string s)
        {
            if (rtbResult.IsDisposed) return;
            if (rtbResult.InvokeRequired)
            {
                rtbResult.BeginInvoke(new Action(() => AppendProbeText(s)));
                return;
            }
            rtbResult.SelectionStart = rtbResult.TextLength;
            rtbResult.SelectionColor = Color.FromArgb(180, 220, 180);
            rtbResult.AppendText(s);
            rtbResult.SelectionStart = rtbResult.TextLength;
            rtbResult.ScrollToCaret();
        }
    }
}