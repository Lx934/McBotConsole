using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace JBSS261A
{
    public partial class SettingsDialog : Form
    {
        // ==================== 对外属性 ====================
        public string ServerIp { get; private set; }
        public int ServerPort { get; private set; }
        public string Version { get; private set; }
        public int Count { get; private set; }
        public int Concurrency { get; private set; }
        public int RetryDelay { get; private set; }
        public int ConnectionInterval { get; private set; }
        public int ConnectionTimeout { get; private set; }
        public string PlayerNamePrefix { get; private set; }
        public bool StayConnected { get; private set; }
        public int AutoDisconnectAfter { get; private set; }

        public string ProxyMode { get; private set; } = "none";
        public List<string> ProxyList { get; private set; } = new List<string>();
        public bool UseNameDict { get; set; } = false;

        public Action OpenPluginManager { get; set; }

        // ==================== 输入控件 ====================
        private TextBox txtIp, txtPort;
        private ComboBox cmbVersion;
        private CheckBox chkCustomVersion;
        private TextBox txtCustomVersion;
        private TextBox txtCount, txtConcurrency, txtConnInterval, txtTimeout, txtRetryDelay;
        private TextBox txtPrefix;
        private CheckBox chkStay;
        private TextBox txtAutoDisconnect;
        private CheckBox chkDict;

        private RadioButton rdoProxyNone, rdoProxySingle, rdoProxyRotate;
        private TextBox txtProxyList;
        private Label lblProxyCount;

        // 导航
        private ListBox navList;
        private Panel contentHost;
        private readonly Dictionary<string, Panel> pages = new Dictionary<string, Panel>();

        public SettingsDialog(
            string ip, int port, string version, int count, int concurrency,
            int retryDelay, int connInterval, int timeout,
            string prefix, bool stayConnected, int autoDisconnect)
        {
            InitializeUI();
            BindCurrentValues(ip, port, version, count, concurrency,
                              retryDelay, connInterval, timeout,
                              prefix, stayConnected, autoDisconnect);
            ShowPage("server");
        }

        // ============================================================
        // 主框架
        // ============================================================
        private void InitializeUI()
        {
            this.Text = "⚙ 设置";
            this.Size = new Size(900, 680);
            this.MinimumSize = new Size(820, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.BackColor = Color.FromArgb(245, 245, 245);
            this.Font = new Font("Microsoft YaHei UI", 9.5F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);

            // ---------- 底部按钮 ----------
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = Color.FromArgb(235, 235, 235),
                Padding = new Padding(0, 16, 20, 16)
            };

            var btnOk = new Button
            {
                Text = "确定",
                Size = new Size(90, 32),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.System
            };
            btnOk.Click += (s, e) => SaveAndClose();

            var btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(90, 32),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.System,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            bottomPanel.Controls.Add(btnOk);
            bottomPanel.Controls.Add(btnCancel);

            // ---------- 左侧导航 + 右侧内容 ----------
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 1,
                BackColor = Color.FromArgb(220, 220, 220)
            };
            split.FixedPanel = FixedPanel.Panel1;
            split.IsSplitterFixed = true;
            split.SplitterDistance = 180;

            navList = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(37, 37, 38),
                ForeColor = Color.FromArgb(230, 230, 230),
                Font = new Font("Microsoft YaHei UI", 10F),
                IntegralHeight = false,
                ItemHeight = 38,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            navList.DrawItem += NavList_DrawItem;
            navList.SelectedIndexChanged += (s, e) =>
            {
                if (navList.SelectedIndex < 0) return;
                ShowPage(navList.SelectedItem.ToString());
            };
            navList.Items.Add("server");
            navList.Items.Add("version");
            navList.Items.Add("connection");
            navList.Items.Add("player");
            navList.Items.Add("stay");
            navList.Items.Add("proxy");
            navList.Items.Add("plugins");
            split.Panel1.Controls.Add(navList);

            contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(24, 20, 24, 20)
            };
            split.Panel2.Controls.Add(contentHost);

            pages["server"] = BuildServerPage();
            pages["version"] = BuildVersionPage();
            pages["connection"] = BuildConnectionPage();
            pages["player"] = BuildPlayerPage();
            pages["stay"] = BuildStayPage();
            pages["proxy"] = BuildProxyPage();
            pages["plugins"] = BuildPluginsPage();

            foreach (var p in pages.Values)
            {
                p.Visible = false;
                p.Dock = DockStyle.Fill;
                contentHost.Controls.Add(p);
            }

            this.Controls.Add(split);
            this.Controls.Add(bottomPanel);
        }

        private readonly Dictionary<string, string> navLabels = new Dictionary<string, string>
        {
            { "server",     "🌐  服务器" },
            { "version",    "🎮  游戏版本" },
            { "connection", "🔗  连接参数" },
            { "player",     "👤  玩家名" },
            { "stay",       "⏱  保持连接" },
            { "proxy",      "🛡  代理" },
            { "plugins",    "🔌  插件" }
        };

        private void NavList_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            string key = navList.Items[e.Index].ToString();
            string text = navLabels.ContainsKey(key) ? navLabels[key] : key;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            using (var bg = new SolidBrush(selected ? Color.FromArgb(14, 99, 156) : Color.FromArgb(37, 37, 38)))
                e.Graphics.FillRectangle(bg, e.Bounds);

            using (var fg = new SolidBrush(selected ? Color.White : Color.FromArgb(220, 220, 220)))
            using (var font = new Font("Microsoft YaHei UI", 10F, selected ? FontStyle.Bold : FontStyle.Regular))
            {
                var rect = new Rectangle(e.Bounds.X + 18, e.Bounds.Y, e.Bounds.Width - 18, e.Bounds.Height);
                var sf = new StringFormat { LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(text, font, fg, rect, sf);
            }
        }

        private void ShowPage(string key)
        {
            foreach (var kv in pages) kv.Value.Visible = (kv.Key == key);
            if (navList.SelectedItem == null || navList.SelectedItem.ToString() != key)
            {
                int idx = navList.Items.IndexOf(key);
                if (idx >= 0) navList.SelectedIndex = idx;
            }
        }

        // ============================================================
        // 布局辅助
        // ============================================================
        /// <summary>
        /// 两列布局：左列标签右对齐，右列输入框自动拉伸
        /// </summary>
        private TableLayoutPanel MakeTwoColumn()
        {
            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return t;
        }

        private Label MakeSectionTitle(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 100, 160),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 14)
            };
        }

        private Label MakeHint(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.FromArgb(110, 110, 110),
                AutoSize = true,
                MaximumSize = new Size(580, 0),
                Margin = new Padding(0, 6, 0, 0)
            };
        }

        private void AddFullWidth(TableLayoutPanel t, ref int row, Control c)
        {
            t.Controls.Add(c, 0, row);
            t.SetColumnSpan(c, 2);
            row++;
        }

        /// <summary>
        /// 一行标签（右对齐、垂直居中）
        /// </summary>
        private Label MakeRowLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(3, 10, 12, 10)
            };
        }

        /// <summary>
        /// 一行：标签 + 输入框
        /// </summary>
        private TextBox AddTextRow(TableLayoutPanel t, ref int row, string label, string defaultText)
        {
            var lbl = MakeRowLabel(label);
            var box = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 10, 3, 10),
                Text = defaultText
            };
            t.Controls.Add(lbl, 0, row);
            t.Controls.Add(box, 1, row);
            row++;
            return box;
        }

        /// <summary>
        /// 一行：标签 + 输入框（窄）+ 单位说明
        /// </summary>
        private TextBox AddTextRowWithUnit(TableLayoutPanel t, ref int row, string label, string defaultText, string unit)
        {
            var lbl = MakeRowLabel(label);

            var cell = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 10, 3, 10),
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };
            cell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            cell.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var box = new TextBox
            {
                Width = 110,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0),
                Text = defaultText
            };

            var lblUnit = new Label
            {
                Text = unit,
                ForeColor = Color.FromArgb(110, 110, 110),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 0, 0)
            };

            cell.Controls.Add(box, 0, 0);
            cell.Controls.Add(lblUnit, 1, 0);

            t.Controls.Add(lbl, 0, row);
            t.Controls.Add(cell, 1, row);
            row++;
            return box;
        }

        /// <summary>
        /// 一行：标签 + 下拉框
        /// </summary>
        private ComboBox AddComboRow(TableLayoutPanel t, ref int row, string label, string[] items, string defaultItem)
        {
            var lbl = MakeRowLabel(label);
            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left,
                Width = 200,
                Margin = new Padding(0, 10, 3, 10)
            };
            combo.Items.AddRange(items);
            if (!string.IsNullOrEmpty(defaultItem) && combo.Items.Contains(defaultItem))
                combo.SelectedItem = defaultItem;
            else if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;

            t.Controls.Add(lbl, 0, row);
            t.Controls.Add(combo, 1, row);
            row++;
            return combo;
        }

        /// <summary>
        /// 一行：标签 + 复选框
        /// </summary>
        private CheckBox AddCheckRow(TableLayoutPanel t, ref int row, string label, string checkText, bool defaultChecked)
        {
            var lbl = MakeRowLabel(label);
            var chk = new CheckBox
            {
                Text = checkText,
                Checked = defaultChecked,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 10, 3, 10)
            };
            t.Controls.Add(lbl, 0, row);
            t.Controls.Add(chk, 1, row);
            row++;
            return chk;
        }

        // ============================================================
        // 各分类页面
        // ============================================================
        private Panel BuildServerPage()
        {
            var page = new Panel { BackColor = Color.White };
            var t = MakeTwoColumn();
            int row = 0;

            AddFullWidth(t, ref row, MakeSectionTitle("🌐 服务器"));
            AddFullWidth(t, ref row, MakeHint("填服务器地址（IP 或域名）和端口。\n如果是域名，程序会自动解析为 IP。"));

            txtIp = AddTextRow(t, ref row, "服务器地址：", "127.0.0.1");
            txtPort = AddTextRow(t, ref row, "端口：", "25565");

            page.Controls.Add(t);
            return page;
        }

        private Panel BuildVersionPage()
        {
            var page = new Panel { BackColor = Color.White };
            var t = MakeTwoColumn();
            int row = 0;

            AddFullWidth(t, ref row, MakeSectionTitle("🎮 游戏版本"));
            AddFullWidth(t, ref row, MakeHint("选择常用版本，或勾选「启用自定义版本」手动输入。"));

            cmbVersion = AddComboRow(t, ref row, "常用版本：", VersionList.Common, VersionList.Default);
            chkCustomVersion = AddCheckRow(t, ref row, "", "启用自定义版本", false);

            var lblCustom = MakeRowLabel("自定义版本：");
            var boxCustom = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 10, 3, 10),
                Enabled = false
            };
            PlaceholderHelper.SetPlaceholder(boxCustom, "例如 1.7.10 或 23w45a");
            t.Controls.Add(lblCustom, 0, row);
            t.Controls.Add(boxCustom, 1, row);
            row++;
            txtCustomVersion = boxCustom;

            chkCustomVersion.CheckedChanged += (s, e) =>
            {
                txtCustomVersion.Enabled = chkCustomVersion.Checked;
                cmbVersion.Enabled = !chkCustomVersion.Checked;
            };

            AddFullWidth(t, ref row, MakeHint("版本必须和服务器一致，否则会被踢出。"));

            page.Controls.Add(t);
            return page;
        }

        private Panel BuildConnectionPage()
        {
            var page = new Panel { BackColor = Color.White };
            var t = MakeTwoColumn();
            int row = 0;

            AddFullWidth(t, ref row, MakeSectionTitle("🔗 连接参数"));
            AddFullWidth(t, ref row, MakeHint("控制「登录多少个假人、同时连多少、失败后多久重试」。"));

            txtCount = AddTextRow(t, ref row, "尝试次数：", "100");
            txtConcurrency = AddTextRow(t, ref row, "并发数：", "1");
            txtConnInterval = AddTextRowWithUnit(t, ref row, "连接间隔：", "3000", "毫秒");
            txtTimeout = AddTextRowWithUnit(t, ref row, "连接超时：", "5", "秒");
            txtRetryDelay = AddTextRowWithUnit(t, ref row, "重试延迟：", "5000", "毫秒");

            AddFullWidth(t, ref row, MakeHint("并发数越大越快，但容易触发服务器限流。"));

            page.Controls.Add(t);
            return page;
        }

        private Panel BuildPlayerPage()
        {
            var page = new Panel { BackColor = Color.White };
            var t = MakeTwoColumn();
            int row = 0;

            AddFullWidth(t, ref row, MakeSectionTitle("👤 玩家名"));
            AddFullWidth(t, ref row, MakeHint("每个假人的名字都会从「前缀 + 随机字符」生成。"));

            txtPrefix = AddTextRow(t, ref row, "名字前缀：", "");
            AddFullWidth(t, ref row, MakeHint("示例：填 MyBot 会生成 MyBot_a3f2、MyBot_x9k1 之类。"));

            // ★ 字典模式复选框
            chkDict = AddCheckRow(t, ref row, "", "使用英文名字典（Peter、Emma 等常见英文名）", UseNameDict);
            chkDict.CheckedChanged += (s, e) => this.UseNameDict = chkDict.Checked;

            AddFullWidth(t, ref row, MakeHint(
                "推荐在遇到反机器人拦截时启用。\n" +
                "启用后假人名字形如 Peter42、Emma77、Tester_Luna88。\n" +
                "字典文件：backend\\names.txt（可自由编辑）。\n" +
                "默认关闭。"));

            page.Controls.Add(t);
            return page;
        }

        private Panel BuildStayPage()
        {
            var page = new Panel { BackColor = Color.White };
            var t = MakeTwoColumn();
            int row = 0;

            AddFullWidth(t, ref row, MakeSectionTitle("⏱ 保持连接"));
            AddFullWidth(t, ref row, MakeHint("勾选后，假人登录成功会挂着不走；不勾则登录成功立刻断开。"));

            chkStay = AddCheckRow(t, ref row, "", "登录成功后保持连接（不自动退出）", false);
            txtAutoDisconnect = AddTextRowWithUnit(t, ref row, "自动断开：", "10", "秒（0 表示永不）");

            page.Controls.Add(t);
            return page;
        }

        private Panel BuildProxyPage()
        {
            var page = new Panel { BackColor = Color.White };
            var t = MakeTwoColumn();
            int row = 0;

            AddFullWidth(t, ref row, MakeSectionTitle("🛡 代理"));
            AddFullWidth(t, ref row, MakeHint(
                "启用代理后，每个假人会通过代理建立 TCP 连接。\n" +
                "用途：模拟来自不同来源的连接、绕过 IP 限流。"));

            // 模式
            var modePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 10, 3, 10)
            };
            rdoProxyNone = new RadioButton { Text = "不使用代理（直连）", AutoSize = true, Checked = true, Margin = new Padding(0, 2, 0, 2) };
            rdoProxySingle = new RadioButton { Text = "全部走同一个代理", AutoSize = true, Margin = new Padding(0, 2, 0, 2) };
            rdoProxyRotate = new RadioButton { Text = "轮换代理池（每个假人用不同的代理）", AutoSize = true, Margin = new Padding(0, 2, 0, 2) };
            modePanel.Controls.Add(rdoProxyNone);
            modePanel.Controls.Add(rdoProxySingle);
            modePanel.Controls.Add(rdoProxyRotate);

            var lblMode = MakeRowLabel("代理模式：");
            t.Controls.Add(lblMode, 0, row);
            t.Controls.Add(modePanel, 1, row);
            row++;

            // 列表
            var lblList = MakeRowLabel("代理列表：");
            var listPanel = new Panel
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Height = 220,
                Margin = new Padding(0, 10, 3, 10)
            };

            txtProxyList = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = false,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9.5F)
            };
            PlaceholderHelper.SetPlaceholder(txtProxyList,
                "每行一个代理，例如：\nsocks5://127.0.0.1:1080\nsocks5://user:pass@1.2.3.4:1080\nhttp://5.6.7.8:8080");

            var listBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 38,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            var btnLoad = new Button { Text = "📂 从文件加载", AutoSize = true, Padding = new Padding(10, 4, 10, 4), FlatStyle = FlatStyle.System };
            btnLoad.Click += (s, e) => LoadProxyFile();

            var btnDedup = new Button { Text = "🧹 去重", AutoSize = true, Padding = new Padding(10, 4, 10, 4), FlatStyle = FlatStyle.System };
            btnDedup.Click += (s, e) => DedupProxyList();

            var btnClear = new Button { Text = "🗑 清空", AutoSize = true, Padding = new Padding(10, 4, 10, 4), FlatStyle = FlatStyle.System };
            btnClear.Click += (s, e) => { txtProxyList.Text = ""; UpdateProxyCount(); };

            listBtns.Controls.Add(btnLoad);
            listBtns.Controls.Add(btnDedup);
            listBtns.Controls.Add(btnClear);

            listPanel.Controls.Add(txtProxyList);
            listPanel.Controls.Add(listBtns);

            t.Controls.Add(lblList, 0, row);
            t.Controls.Add(listPanel, 1, row);
            row++;

            lblProxyCount = new Label
            {
                Text = "共 0 个代理",
                ForeColor = Color.FromArgb(110, 110, 110),
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 0)
            };
            AddFullWidth(t, ref row, lblProxyCount);

            txtProxyList.TextChanged += (s, e) => UpdateProxyCount();

            AddFullWidth(t, ref row, MakeHint(
                "支持格式：\n" +
                "  socks5://host:port\n" +
                "  socks5://user:pass@host:port\n" +
                "  socks4://host:port\n" +
                "  http://host:port\n" +
                "  http://user:pass@host:port\n" +
                "  也可以直接写 host:port（默认按 socks5 处理）"));

            page.Controls.Add(t);
            return page;
        }

        private void UpdateProxyCount()
        {
            if (lblProxyCount == null || txtProxyList == null) return;
            int cnt = 0;
            foreach (var line in txtProxyList.Lines)
                if (!string.IsNullOrWhiteSpace(line)) cnt++;
            lblProxyCount.Text = $"共 {cnt} 个代理";
        }

        private void LoadProxyFile()
        {
            using (var ofd = new OpenFileDialog
            {
                Filter = "文本文件|*.txt|所有文件|*.*",
                Title = "选择代理列表文件（每行一个）"
            })
            {
                if (ofd.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    var lines = System.IO.File.ReadAllLines(ofd.FileName);
                    txtProxyList.Text = string.Join("\r\n", lines);
                    UpdateProxyCount();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "加载失败：" + ex.Message);
                }
            }
        }

        private void DedupProxyList()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();
            foreach (var line in txtProxyList.Lines)
            {
                string s = line.Trim();
                if (string.IsNullOrEmpty(s)) continue;
                if (seen.Add(s)) result.Add(s);
            }
            txtProxyList.Text = string.Join("\r\n", result);
            UpdateProxyCount();
        }

        private Panel BuildPluginsPage()
        {
            var page = new Panel { BackColor = Color.White };
            var t = MakeTwoColumn();
            int row = 0;

            AddFullWidth(t, ref row, MakeSectionTitle("🔌 插件"));
            AddFullWidth(t, ref row, MakeHint("每个插件有自己的配置与权限。点击下方按钮打开【插件管理】窗口。"));

            var btnOpenPlugins = new Button
            {
                Text = "🔌 打开插件管理...",
                AutoSize = true,
                Padding = new Padding(20, 8, 20, 8),
                Margin = new Padding(0, 12, 0, 12),
                FlatStyle = FlatStyle.System
            };
            btnOpenPlugins.Click += (s, e) =>
            {
                try { OpenPluginManager?.Invoke(); }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "无法打开插件管理：" + ex.Message,
                        "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            var btnWrap = new Panel { AutoSize = true, Margin = new Padding(0) };
            btnWrap.Controls.Add(btnOpenPlugins);
            AddFullWidth(t, ref row, btnWrap);

            AddFullWidth(t, ref row, MakeHint(
                "插件文件位置：\n" +
                "  · Node 插件：backend\\plugins\\\n" +
                "  · Python 插件：backend\\plugins_py\\"));

            page.Controls.Add(t);
            return page;
        }

        // ============================================================
        // 绑定当前值
        // ============================================================
        private void BindCurrentValues(
            string ip, int port, string version, int count, int concurrency,
            int retryDelay, int connInterval, int timeout,
            string prefix, bool stayConnected, int autoDisconnect)
        {
            txtIp.Text = ip;
            txtPort.Text = port.ToString();

            if (string.IsNullOrEmpty(version)) version = VersionList.Default;
            bool isCommon = false;
            foreach (var v in VersionList.Common)
                if (v == version) { isCommon = true; break; }

            if (isCommon)
            {
                cmbVersion.SelectedItem = version;
                chkCustomVersion.Checked = false;
                txtCustomVersion.Text = "";
                txtCustomVersion.Enabled = false;
                cmbVersion.Enabled = true;
            }
            else
            {
                cmbVersion.SelectedIndex = 0;
                chkCustomVersion.Checked = true;
                txtCustomVersion.Text = version;
                txtCustomVersion.Enabled = true;
                cmbVersion.Enabled = false;
            }

            txtCount.Text = count.ToString();
            txtConcurrency.Text = concurrency.ToString();
            txtRetryDelay.Text = retryDelay.ToString();
            txtConnInterval.Text = connInterval.ToString();
            txtTimeout.Text = timeout.ToString();
            txtPrefix.Text = prefix;
            chkStay.Checked = stayConnected;
            txtAutoDisconnect.Text = autoDisconnect.ToString();

            if (chkDict != null) chkDict.Checked = UseNameDict;
        }

        // ============================================================
        // 保存
        // ============================================================
        private void SaveAndClose()
        {
            try
            {
                ServerIp = txtIp.Text.Trim();
                ServerPort = int.Parse(txtPort.Text.Trim());

                if (chkCustomVersion.Checked)
                {
                    Version = txtCustomVersion.Text.Trim();
                    if (string.IsNullOrEmpty(Version))
                        throw new Exception("启用了自定义版本，但版本号为空");
                }
                else
                {
                    Version = cmbVersion.Text.Trim();
                    if (string.IsNullOrEmpty(Version))
                        throw new Exception("请选择一个游戏版本");
                }

                Count = int.Parse(txtCount.Text.Trim());
                Concurrency = int.Parse(txtConcurrency.Text.Trim());
                RetryDelay = int.Parse(txtRetryDelay.Text.Trim());
                ConnectionInterval = int.Parse(txtConnInterval.Text.Trim());
                ConnectionTimeout = int.Parse(txtTimeout.Text.Trim());
                PlayerNamePrefix = txtPrefix.Text.Trim();
                StayConnected = chkStay.Checked;
                AutoDisconnectAfter = int.Parse(txtAutoDisconnect.Text.Trim());

                // 代理
                if (rdoProxySingle.Checked) ProxyMode = "single";
                else if (rdoProxyRotate.Checked) ProxyMode = "rotate";
                else ProxyMode = "none";

                ProxyList = new List<string>();
                foreach (var line in txtProxyList.Lines)
                {
                    string s = line.Trim();
                    if (!string.IsNullOrEmpty(s)) ProxyList.Add(s);
                }

                if (ProxyMode != "none" && ProxyList.Count == 0)
                    throw new Exception("已启用代理，但代理列表为空");

                // 字典模式
                UseNameDict = (chkDict != null && chkDict.Checked);

                if (string.IsNullOrEmpty(ServerIp))
                    throw new Exception("服务器地址不能为空");
                if (ServerPort < 1 || ServerPort > 65535)
                    throw new Exception("端口必须在 1 ~ 65535 之间");
                if (Count < 0)
                    throw new Exception("尝试次数不能为负数");
                if (Concurrency < 1 || Concurrency > 1000)
                    throw new Exception("并发数必须在 1 ~ 1000 之间");
                if (ConnectionTimeout < 1)
                    throw new Exception("连接超时至少 1 秒");
                if (AutoDisconnectAfter < 0)
                    throw new Exception("自动断开不能为负数");

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "输入有误：" + ex.Message, "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    internal static class TextBoxExt
    {
        public static void PlaceholderTextCompat(this TextBox box, string placeholder)
        {
            PlaceholderHelper.SetPlaceholder(box, placeholder);
        }
    }
}