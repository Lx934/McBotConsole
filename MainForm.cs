using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JBSS261A
{
    public partial class MainForm : Form
    {
        private NodeBackend _backend;
        private bool _backendReady = false;
        private bool _hasShownReady = false;
        private bool _configLoaded = false;

        private readonly List<PluginRow> _cachedPlugins = new List<PluginRow>();
        private readonly object _pluginLock = new object();

        private RunReport _currentRun = null;
        private string _backendDir = null;

        private string _proxyMode = "none";
        private List<string> _proxyList = new List<string>();

        private bool _useNameDict = false;
        private bool _usePlayerPool = false;               // ★ 玩家池
        private string _playerPoolExhausted = "stop";      // ★ 耗尽行为

        private GuiMessage _lastProbeResult = null;

        // ★ 当前打开的调试控制台
        private DebugConsoleDialog _debugDialog = null;

        // ★ 补丁管理器
        private JBSS261A.Patching.PatchManager _patchManager;

        // ★ 检查更新
        private bool _startupCheckDone = false;
        private bool _updateChecking = false;

        public MainForm()
        {
            InitializeComponent();
            WireEvents();
            CreateBackend();
        }

        private void WireEvents()
        {
            this.Load += MainForm_Load;
            this.Shown += MainForm_Shown;
            this.FormClosing += MainForm_FormClosing;

            btnStart.Click += BtnStart_Click;
            btnPause.Click += (s, e) => SendCmd("stop");
            btnResume.Click += (s, e) => SendCmd("run");
            btnStop.Click += (s, e) => ConfirmAndExit();

            btnSettings.Click += BtnSettings_Click;
            btnTools.Click += BtnTools_Click;
            btnReports.Click += BtnReports_Click;

            btnPatches.Click += BtnPatches_Click;

            btnPlugins.Click += BtnPlugins_Click;
            btnHelp.Click += (s, e) => { using (var d = new HelpDialog()) d.ShowDialog(this); };

            btnSend.Click += (s, e) => SendFromInput();
            btnHelpInline.Click += (s, e) => { using (var d = new HelpDialog()) d.ShowDialog(this); };
            txtCommand.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { SendFromInput(); e.SuppressKeyPress = true; }
            };

            menuSettings.Click += BtnSettings_Click;
            menuPlugins.Click += BtnPlugins_Click;
            menuExit.Click += (s, e) => ConfirmAndExit();
            menuClear.Click += (s, e) => rtbLog.Clear();
            menuPackLog.Click += (s, e) => SendCmd("packlog");
            menuStatus.Click += (s, e) => SendCmd("status");

            menuAbout.Click += (s, e) => ShowAbout();
            menuCheckUpdate.Click += BtnCheckUpdate_Click;
            lblUpdate.Click += BtnCheckUpdate_Click;
        }

        private void ShowAbout()
        {
            string text =
                "Minecraft 批量登录客户端\n" +
                $"版本: {VersionInfo.CURRENT_VERSION}\n" +
                "状态: 正式版\n\n" +
                "项目仓库:\n" +
                "https://github.com/Lx934/McBotConsole\n\n" +
                "是否打开仓库页面？";

            var result = MessageBox.Show(this,
                text,
                "关于",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://github.com/Lx934/McBotConsole",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                        "无法打开浏览器: " + ex.Message,
                        "错误",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }

        private void CreateBackend()
        {
            _backend = new NodeBackend();
            _backend.MessageReceived += OnBackendMessage;
            _backend.ErrorOccurred += OnBackendError;
            _backend.Exited += OnBackendExited;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (cmbVersion.Items.Count == 0)
            {
                foreach (var v in VersionList.Common)
                    cmbVersion.Items.Add(v);
            }
            int idx = cmbVersion.Items.IndexOf(VersionList.Default);
            if (idx >= 0) cmbVersion.SelectedIndex = idx;
            else if (cmbVersion.Items.Count > 0) cmbVersion.SelectedIndex = 0;

            PlaceholderHelper.SetPlaceholder(txtCommand,
                "可以直接输入中文命令，例如：改IP 192.168.1.1 / 设置次数 50 / 看状态 / 插件列表");

            _backendDir = FindBackendDir();
            AppendLog("INFO", $"后台目录: {_backendDir}");

            // ★ 初始化补丁管理器
            try
            {
                _patchManager = new JBSS261A.Patching.PatchManager(
                    AppDomain.CurrentDomain.BaseDirectory,
                    msg => AppendLog("INFO", "[补丁] " + msg));
            }
            catch (Exception ex)
            {
                AppendLog("WARN", "补丁管理器初始化失败: " + ex.Message);
            }

            AppendLog("INFO", "如需帮助，点右上角【帮助】。");

            if (!_backend.Start(_backendDir))
            {
                AppendLog("ERROR", "后台启动失败。请确认 backend\\index.js 存在，或已打包 backend.exe。");
            }
            else
            {
                _backendReady = true;
                AppendLog("INFO", "后台已启动，正在初始化...");
            }
        }

        // ============== ★ 检查更新 ==============

        private async void MainForm_Shown(object sender, EventArgs e)
        {
            if (_startupCheckDone) return;
            _startupCheckDone = true;

            await Task.Delay(3000);

            try
            {
                var info = await UpdateChecker.CheckAsync();
                if (info.Checked && info.HasNewVersion)
                {
                    SetUpdateBadge($"🔔 发现新版本 {info.LatestVersion}，点击查看");
                    AppendLog("INFO",
                        $"发现新版本 {info.LatestVersion}（当前 {VersionInfo.CURRENT_VERSION}），" +
                        "可在【帮助】菜单里点【检查更新】查看详情。");
                }
            }
            catch
            {
                // 静默失败
            }
        }

        private void SetUpdateBadge(string text)
        {
            if (lblUpdate == null) return;
            lblUpdate.Text = text ?? "";
            lblUpdate.IsLink = !string.IsNullOrEmpty(text);
            lblUpdate.LinkBehavior = LinkBehavior.HoverUnderline;
        }

        private async void BtnCheckUpdate_Click(object sender, EventArgs e)
        {
            if (_updateChecking) return;
            _updateChecking = true;

            string oldMenuText = menuCheckUpdate != null ? menuCheckUpdate.Text : null;
            if (menuCheckUpdate != null)
            {
                menuCheckUpdate.Enabled = false;
                menuCheckUpdate.Text = "🔄 检查中...";
            }

            try
            {
                UpdateChecker.ClearCache();
                var info = await UpdateChecker.CheckAsync(forceRefresh: true);

                using (var dlg = new UpdateDialog(info))
                {
                    dlg.ShowDialog(this);
                }

                if (info.Checked && info.HasNewVersion)
                {
                    SetUpdateBadge($"🔔 发现新版本 {info.LatestVersion}，点击查看");
                }
                else if (info.Checked && !info.HasNewVersion)
                {
                    SetUpdateBadge("");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "检查更新失败：" + ex.Message,
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _updateChecking = false;
                if (menuCheckUpdate != null)
                {
                    menuCheckUpdate.Enabled = true;
                    if (oldMenuText != null) menuCheckUpdate.Text = oldMenuText;
                }
            }
        }

        // ============== 后端 & 其余逻辑 ==============

        private string FindBackendDir()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] probes = new[]
            {
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\backend")),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\backend")),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\backend")),
                Path.GetFullPath(Path.Combine(baseDir, "backend")),
            };
            foreach (var p in probes)
            {
                if (Directory.Exists(p) &&
                    (File.Exists(Path.Combine(p, "index.js")) ||
                     File.Exists(Path.Combine(p, "mc-bot-backend.exe"))))
                    return p;
            }
            return probes[0];
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try { _backend?.SendExit(); System.Threading.Thread.Sleep(150); }
            catch { }
            _backend?.Dispose();
        }

        // ============== UI → 后端 ==============
        private void SendFromInput()
        {
            string text = PlaceholderHelper.GetRealText(txtCommand).Trim();
            if (string.IsNullOrEmpty(text)) return;

            if (text.StartsWith("解析 ") || text.StartsWith("解析　") ||
                text.StartsWith("serverinfo", StringComparison.OrdinalIgnoreCase))
            {
                string args = "";
                int sp = text.IndexOf(' ');
                if (sp >= 0) args = text.Substring(sp + 1).Trim();

                string host = txtIp.Text.Trim();
                int port = ParseInt(txtPort.Text, 25565);

                if (!string.IsNullOrEmpty(args))
                {
                    var parts = args.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 1) host = parts[0];
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int p)) port = p;
                }

                PlaceholderHelper.ClearToPlaceholder(txtCommand);
                OpenServerInfoDialog(host, port);
                return;
            }

            string translated = CommandTranslator.Translate(text);

            if (translated == null)
            {
                AppendLog("WARN", $"「{text}」不够具体，试试「改IP 127.0.0.1」这种写法。点【📖 命令示例】看示例。");
                return;
            }

            if (CommandTranslator.IsExitCommand(translated))
            {
                PlaceholderHelper.ClearToPlaceholder(txtCommand);
                ConfirmAndExit();
                return;
            }

            SendCmd(translated);
            PlaceholderHelper.ClearToPlaceholder(txtCommand);
        }

        private void SendCmd(string command)
        {
            if (!_backendReady || !_backend.IsRunning)
            {
                AppendLog("WARN", "后台还未就绪，稍等一下。");
                return;
            }
            AppendLog("CMD", "> " + command);
            _backend.SendCmd(command);
        }

        private void OpenServerInfoDialog(string host, int port)
        {
            using (var dlg = new ServerInfoDialog(host, port, cmbVersion.Text.Trim()))
            {
                dlg.SendBackendCommand = cmd => SendCmd(cmd);
                dlg.GetLastProbeResult = () => _lastProbeResult;
                _lastProbeResult = null;
                dlg.ShowDialog(this);
            }
        }

        private void BtnTools_Click(object sender, EventArgs e)
        {
            using (var dlg = new ToolsDialog(
                txtIp.Text.Trim(),
                ParseInt(txtPort.Text, 25565),
                cmbVersion.Text.Trim(),
                cmd => SendCmd(cmd),
                () => _lastProbeResult))
            {
                dlg.ShowDialog(this);
            }
        }

        private void BtnReports_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_backendDir))
            {
                AppendLog("WARN", "后台目录未初始化。");
                return;
            }
            using (var dlg = new ReportDialog(_backendDir))
            {
                dlg.ShowDialog(this);
            }
        }

        private void BtnPatches_Click(object sender, EventArgs e)
        {
            if (_patchManager == null)
            {
                AppendLog("ERROR", "补丁管理器未初始化");
                return;
            }

            using (var dlg = new JBSS261A.Patching.PatchDialog(_patchManager))
            {
                dlg.ShowDialog(this);
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (!_backendReady || !_backend.IsRunning)
            {
                AppendLog("ERROR", "后台未运行，无法启动。");
                return;
            }

            string host = txtIp.Text.Trim();
            string realHost = ResolveHostIfNeeded(host);

            string version = cmbVersion.Text;
            if (string.IsNullOrEmpty(version))
            {
                AppendLog("ERROR", "请先在顶部选择游戏版本（或去【⚙ 设置】里自定义）。");
                return;
            }

            int port = ParseInt(txtPort.Text, 25565);
            int count = ParseInt(txtCount.Text, 100);
            int concurrency = ParseInt(txtConcurrency.Text, 1);

            _currentRun = new RunReport
            {
                Id = Guid.NewGuid().ToString("N").Substring(0, 8),
                StartedAt = DateTime.Now,
                Server = $"{realHost}:{port}",
                Version = version,
                Bots = new List<BotRecord>()
            };

            var parameters = new
            {
                ip = realHost,
                port = port,
                version = version,
                count = count,
                concurrency = concurrency,
                proxyMode = _proxyMode,
                proxyList = _proxyList,
                useNameDict = _useNameDict,
                usePlayerPool = _usePlayerPool,                        // ★ 新增
                playerPoolExhausted = _playerPoolExhausted,            // ★ 新增
                stayConnected = chkStay.Checked,
                autoDisconnectAfter = ParseInt(txtAutoDisconnect.Text, 10)
            };

            AppendLog("INFO", $"启动任务：{parameters.ip}:{parameters.port} 版本 {parameters.version}，" +
                              $"次数 {parameters.count}，并发 {parameters.concurrency}" +
                              (_proxyMode != "none" ? $"，代理 {_proxyMode} ({_proxyList.Count}个)" : "") +
                              (_usePlayerPool ? "，玩家池" :
                               (_useNameDict ? "，英文名字典" : "")));
            _backend.SendStart(parameters);
        }

        private string ResolveHostIfNeeded(string host)
        {
            if (string.IsNullOrWhiteSpace(host)) return host;

            if (DnsHelper.IsIpAddress(host))
            {
                AppendLog("INFO", $"目标: {host} (IP)");
                return host;
            }

            AppendLog("INFO", $"正在解析域名 {host} ...");
            string resolved = DnsHelper.ResolveHost(host, out string err);

            if (resolved == null)
            {
                AppendLog("WARN", $"域名 {host} 解析失败：{err}。仍将原样交给后端尝试。");
                return host;
            }

            if (resolved == host)
            {
                AppendLog("INFO", $"目标: {host}");
                return host;
            }

            AppendLog("INFO", $"域名 {host} 解析为 IP: {resolved}");
            return host;
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            string currentVersion = cmbVersion.Text;
            if (string.IsNullOrEmpty(currentVersion)) currentVersion = VersionList.Default;

            using (var dlg = new SettingsDialog(
                txtIp.Text.Trim(),
                ParseInt(txtPort.Text, 25565),
                currentVersion,
                ParseInt(txtCount.Text, 100),
                ParseInt(txtConcurrency.Text, 1),
                ParseInt(txtRetryDelay.Text, 5000),
                ParseInt(txtConnInterval.Text, 3000),
                ParseInt(txtTimeout.Text, 5),
                txtPrefix.Text.Trim(),
                chkStay.Checked,
                ParseInt(txtAutoDisconnect.Text, 10)))
            {
                dlg.OpenPluginManager = () => BtnPlugins_Click(null, EventArgs.Empty);
                dlg.UseNameDict = _useNameDict;
                dlg.UsePlayerPool = _usePlayerPool;                  // ★ 新增
                dlg.PlayerPoolExhausted = _playerPoolExhausted;      // ★ 新增

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                txtIp.Text = dlg.ServerIp;
                txtPort.Text = dlg.ServerPort.ToString();

                if (!cmbVersion.Items.Contains(dlg.Version))
                    cmbVersion.Items.Add(dlg.Version);
                cmbVersion.SelectedItem = dlg.Version;

                txtCount.Text = dlg.Count.ToString();
                txtConcurrency.Text = dlg.Concurrency.ToString();
                txtRetryDelay.Text = dlg.RetryDelay.ToString();
                txtConnInterval.Text = dlg.ConnectionInterval.ToString();
                txtTimeout.Text = dlg.ConnectionTimeout.ToString();
                txtPrefix.Text = dlg.PlayerNamePrefix;
                chkStay.Checked = dlg.StayConnected;
                txtAutoDisconnect.Text = dlg.AutoDisconnectAfter.ToString();

                _proxyMode = dlg.ProxyMode ?? "none";
                _proxyList = dlg.ProxyList ?? new List<string>();
                _useNameDict = dlg.UseNameDict;
                _usePlayerPool = dlg.UsePlayerPool;                   // ★ 新增
                _playerPoolExhausted = dlg.PlayerPoolExhausted;       // ★ 新增

                if (_backendReady && _backend.IsRunning)
                {
                    _backend.SendCmd($"config ip {dlg.ServerIp}");
                    _backend.SendCmd($"config port {dlg.ServerPort}");
                    _backend.SendCmd($"config version {dlg.Version}");
                    _backend.SendCmd($"config count {dlg.Count}");
                    _backend.SendCmd($"config concurrency {dlg.Concurrency}");
                    _backend.SendCmd($"config prefix {dlg.PlayerNamePrefix}");
                    _backend.SendCmd($"config retry-delay {dlg.RetryDelay}");
                    _backend.SendCmd($"config connection-interval {dlg.ConnectionInterval}");
                    _backend.SendCmd($"config connection-timeout {dlg.ConnectionTimeout}");
                    _backend.SendCmd($"config stay-connected {(dlg.StayConnected ? "on" : "off")}");
                    _backend.SendCmd($"config auto-disconnect {dlg.AutoDisconnectAfter}");
                    _backend.SendCmd($"config name-dict {(_useNameDict ? "on" : "off")}");
                    _backend.SendCmd($"config player-pool {(_usePlayerPool ? "on" : "off")}");                       // ★ 新增
                    _backend.SendCmd($"config player-pool-exhausted {_playerPoolExhausted}");                       // ★ 新增
                    _backend.SendCmd("config save");
                }
                else
                {
                    AppendLog("WARN", "后台未运行，设置只保留在当前界面，未写入文件。");
                }

                if (_proxyMode != "none")
                    AppendLog("INFO", $"代理已启用: {_proxyMode}，共 {_proxyList.Count} 个");
                else
                    AppendLog("INFO", "代理未启用");

                if (_usePlayerPool)
                {
                    string ex = _playerPoolExhausted == "loop" ? "循环" : "停止";
                    AppendLog("INFO", $"玩家池模式已启用（耗尽后{ex}）");
                }

                if (!string.IsNullOrWhiteSpace(dlg.ServerIp) && !DnsHelper.IsIpAddress(dlg.ServerIp))
                {
                    string resolved = DnsHelper.ResolveHost(dlg.ServerIp, out string err);
                    if (resolved != null)
                        AppendLog("INFO", $"设置中的域名 {dlg.ServerIp} 解析为 IP: {resolved}");
                    else
                        AppendLog("WARN", $"设置中的域名 {dlg.ServerIp} 解析失败：{err}");
                }

                AppendLog("INFO", "设置已保存。");
            }
        }

        private void BtnPlugins_Click(object sender, EventArgs e)
        {
            if (!_backendReady || !_backend.IsRunning)
            {
                AppendLog("WARN", "后台还未就绪，无法打开插件管理。");
                return;
            }

            SendCmd("plugins");

            using (var dlg = new PluginDialog(
                () => { lock (_pluginLock) return new List<PluginRow>(_cachedPlugins); },
                cmd => SendCmd(cmd)))
            {
                dlg.ShowDialog(this);
            }
        }

        private void ConfirmAndExit()
        {
            var r = MessageBox.Show(this,
                "确定要退出程序吗？\n所有正在运行的连接会被断开。",
                "退出确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;

            try { _backend?.SendExit(); } catch { }
            System.Threading.Thread.Sleep(200);
            _backend?.Dispose();
            Application.Exit();
        }

        private int ParseInt(string s, int fallback)
        {
            return int.TryParse(s, out int v) ? v : fallback;
        }

        // ============== 后端 → UI ==============
        private void OnBackendMessage(GuiMessage msg)
        {
            if (msg == null || string.IsNullOrEmpty(msg.Type)) return;

            BeginInvoke(new Action(() =>
            {
                switch (msg.Type)
                {
                    case "log": AppendLog(msg.Level ?? "INFO", msg.Message ?? ""); break;
                    case "status": UpdateStatus(msg); break;
                    case "plugin-list": UpdatePluginList(msg); break;
                    case "bot-result": HandleBotResult(msg); break;
                    case "run-finished": HandleRunFinished(msg); break;
                    // case "antibot-detected": ★ 已删除
                    case "config-state": ApplyConfigState(msg.Cfg); break;
                    case "debug-result": HandleDebugResult(msg); break;
                    case "debug-done": _debugDialog?.FinishDebugRequest(); break;
                    case "plugin-probe-result":
                        _lastProbeResult = msg;
                        int cnt = msg.Plugins?.Length ?? 0;
                        if (msg.ProbeSuccess == true)
                            AppendLog("INFO", $"插件探测完成: {cnt} 个插件（{msg.Host}:{msg.Port}）");
                        else
                            AppendLog("WARN", $"插件探测失败: {msg.ProbeError ?? "未知错误"}");
                        break;
                    case "ready":
                        _backendReady = true;
                        if (!_hasShownReady)
                        {
                            _hasShownReady = true;
                            AppendLog("INFO", "后台准备就绪。可以点【▶ 开始】运行，或点【❓ 帮助】了解怎么用。");
                        }
                        break;
                    case "clear": rtbLog.Clear(); break;
                }
            }));
        }

        private void HandleDebugResult(GuiMessage msg)
        {
            string text = msg.Message ?? "";
            string level = msg.Level ?? "info";

            if (_debugDialog != null && !_debugDialog.IsDisposed)
            {
                _debugDialog.AppendDebugResult(text, level);
            }
            else
            {
                AppendLog(level == "error" ? "ERROR" : (level == "warn" ? "WARN" : "INFO"),
                          "[debug] " + text);
            }
        }

        private void ApplyConfigState(ConfigState cfg)
        {
            if (cfg == null) return;

            if (!string.IsNullOrEmpty(cfg.Ip)) txtIp.Text = cfg.Ip;
            if (cfg.Port.HasValue) txtPort.Text = cfg.Port.Value.ToString();

            if (!string.IsNullOrEmpty(cfg.Version))
            {
                if (!cmbVersion.Items.Contains(cfg.Version))
                    cmbVersion.Items.Add(cfg.Version);
                cmbVersion.SelectedItem = cfg.Version;
            }

            if (cfg.Count.HasValue) txtCount.Text = cfg.Count.Value.ToString();
            if (cfg.Concurrency.HasValue) txtConcurrency.Text = cfg.Concurrency.Value.ToString();
            if (cfg.RetryDelay.HasValue) txtRetryDelay.Text = cfg.RetryDelay.Value.ToString();
            if (cfg.ConnectionInterval.HasValue) txtConnInterval.Text = cfg.ConnectionInterval.Value.ToString();
            if (cfg.ConnectionTimeout.HasValue) txtTimeout.Text = cfg.ConnectionTimeout.Value.ToString();
            if (cfg.Prefix != null) txtPrefix.Text = cfg.Prefix;
            if (cfg.StayConnected.HasValue) chkStay.Checked = cfg.StayConnected.Value;
            if (cfg.AutoDisconnectAfter.HasValue) txtAutoDisconnect.Text = cfg.AutoDisconnectAfter.Value.ToString();

            if (!string.IsNullOrEmpty(cfg.ProxyMode)) _proxyMode = cfg.ProxyMode;
            if (cfg.ProxyList != null) _proxyList = new List<string>(cfg.ProxyList);
            if (cfg.UseNameDict.HasValue) _useNameDict = cfg.UseNameDict.Value;
            if (cfg.UsePlayerPool.HasValue) _usePlayerPool = cfg.UsePlayerPool.Value;                 // ★ 新增
            if (!string.IsNullOrEmpty(cfg.PlayerPoolExhausted)) _playerPoolExhausted = cfg.PlayerPoolExhausted;  // ★ 新增

            if (!_configLoaded)
            {
                _configLoaded = true;
                AppendLog("INFO", $"已加载上次保存的配置（服务器 {txtIp.Text}:{txtPort.Text}，版本 {cmbVersion.Text}，" +
                                  $"次数 {txtCount.Text}，并发 {txtConcurrency.Text}" +
                                  (_proxyMode != "none" ? $"，代理 {_proxyMode}" : "") +
                                  (_usePlayerPool ? "，玩家池" :
                                   (_useNameDict ? "，字典模式" : "")) + "）");
            }
        }

        // ★ HandleAntiBotDetected 方法已删除

        private void HandleBotResult(GuiMessage msg)
        {
            if (_currentRun == null) return;
            _currentRun.Bots.Add(new BotRecord
            {
                BotId = msg.BotId ?? 0,
                Username = msg.Username ?? "",
                Success = msg.BotSuccess ?? false,
                ErrorType = msg.BotErrorType ?? "",
                Error = msg.BotError ?? "",
                DurationMs = msg.BotDurationMs ?? 0,
                ReconnectCount = msg.ReconnectCount ?? 0,
                Time = DateTime.Now
            });
        }

        private void HandleRunFinished(GuiMessage msg)
        {
            if (_currentRun == null) return;

            _currentRun.FinishedAt = DateTime.Now;
            _currentRun.TotalAttempts = msg.TotalAttempts ?? _currentRun.Bots.Count;
            _currentRun.SuccessCount = msg.SuccessCount ?? 0;
            _currentRun.FailureCount = msg.FailureCount ?? 0;
            if (_currentRun.TotalAttempts > 0)
                _currentRun.SuccessRate = (_currentRun.SuccessCount * 100.0) / _currentRun.TotalAttempts;

            long totalDur = 0;
            int cnt = 0;
            foreach (var b in _currentRun.Bots)
                if (b.DurationMs > 0) { totalDur += b.DurationMs; cnt++; }
            if (cnt > 0) _currentRun.AvgLatencyMs = (double)totalDur / cnt;

            try
            {
                string saved = ReportManager.Save(_currentRun, _backendDir);
                if (!string.IsNullOrEmpty(saved))
                    AppendLog("INFO", $"📊 运行报告已保存: {Path.GetFileName(saved)}");
                else
                    AppendLog("WARN", "报告保存失败");
            }
            catch (Exception ex)
            {
                AppendLog("ERROR", "报告保存异常: " + ex.Message);
            }

            _currentRun = null;
        }

        private void OnBackendError(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            BeginInvoke(new Action(() => AppendLog("ERROR", "[后台错误] " + line)));
        }

        private void OnBackendExited(int code)
        {
            BeginInvoke(new Action(() =>
            {
                _backendReady = false;
                AppendLog("WARN", $"后台已退出（代码 {code}）");
                btnStart.Enabled = false;
                btnPause.Enabled = false;
                btnResume.Enabled = false;
            }));
        }

        private void AppendLog(string level, string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}\n";

            Color color = Color.FromArgb(230, 230, 230);
            switch (level)
            {
                case "ERROR": color = Color.FromArgb(244, 135, 113); break;
                case "WARN": color = Color.FromArgb(220, 220, 170); break;
                case "DEBUG": color = Color.FromArgb(140, 140, 140); break;
                case "CMD": color = Color.FromArgb(86, 156, 214); break;
                case "INFO": color = Color.FromArgb(230, 230, 230); break;
            }

            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor = color;
            rtbLog.AppendText(line);

            if (rtbLog.Lines.Length > 5000)
            {
                int cut = rtbLog.GetFirstCharIndexFromLine(1000);
                if (cut > 0)
                {
                    rtbLog.Select(0, cut);
                    rtbLog.SelectedText = "";
                }
            }

            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.ScrollToCaret();
        }

        private void UpdateStatus(GuiMessage msg)
        {
            string state = msg.Stopped == true ? "已停止"
                : msg.Paused == true ? "暂停"
                : msg.Started == true ? "运行中" : "就绪";

            string modeInfo = "";
            if (msg.PlayerPoolEnabled == true) modeInfo = "  |  玩家池: 开";
            else if (msg.NameDictEnabled == true) modeInfo = "  |  字典模式: 开";

            string devInfo = JBSS261A.Patching.DevModeFlag.IsActive()
                ? "  |  ⚠️ 开发者模式" : "";

            lblStatus.Text =
                $"状态: {state}  |  成功: {msg.Success ?? 0}  |  失败: {msg.Failure ?? 0}  |  " +
                $"尝试: {msg.Attempts ?? 0}  |  活跃: {msg.Active ?? 0}  |  " +
                $"重试延迟: {msg.RetryDelay ?? 5000}ms  |  连接间隔: {msg.ConnectionInterval ?? 3000}ms" +
                modeInfo + devInfo;

            bool running = msg.Started == true && msg.Stopped != true;
            btnStart.Enabled = _backendReady && !running;
            btnPause.Enabled = running && msg.Paused != true;
            btnResume.Enabled = msg.Paused == true;
        }

        private void UpdatePluginList(GuiMessage msg)
        {
            lock (_pluginLock)
            {
                _cachedPlugins.Clear();
                if (msg.Node != null)
                    foreach (var p in msg.Node)
                        _cachedPlugins.Add(new PluginRow
                        {
                            Type = "Node",
                            Name = p.Name,
                            Version = p.Version,
                            Author = p.Author,
                            Description = p.Description,
                            Permission = p.Permission
                        });
                if (msg.Python != null)
                    foreach (var p in msg.Python)
                        _cachedPlugins.Add(new PluginRow
                        {
                            Type = "Python",
                            Name = p.Name,
                            Version = p.Version,
                            Author = p.Author,
                            Description = p.Description,
                            Permission = p.Permission
                        });
            }

            lstPlugins.BeginUpdate();
            lstPlugins.Items.Clear();
            lock (_pluginLock)
            {
                foreach (var p in _cachedPlugins)
                    lstPlugins.Items.Add($"[{p.Type}] {p.Name} v{p.Version} 权限 {p.Permission}");
            }
            if (lstPlugins.Items.Count == 0)
                lstPlugins.Items.Add("(暂无插件)");
            lstPlugins.EndUpdate();
        }
    }
}