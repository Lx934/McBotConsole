using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace JBSS261A
{
    public class PluginRow
    {
        public string Type;
        public string Name;
        public string Version;
        public string Author;
        public string Description;
        public int Permission;
    }

    public partial class PluginDialog : Form
    {
        private readonly Func<List<PluginRow>> _getPlugins;
        private readonly Action<string> _sendCommand;

        private ListView _listView;
        private Button _btnRefresh;
        private Button _btnReload;
        private Button _btnUnload;
        private ComboBox _cmbPermission;
        private Button _btnApplyPerm;
        private Label _lblDetail;
        private Timer _refreshTimer;

        public PluginDialog(Func<List<PluginRow>> getPlugins, Action<string> sendCommand)
        {
            _getPlugins = getPlugins;
            _sendCommand = sendCommand;

            InitializeUI();
            RefreshList();

            _refreshTimer = new Timer { Interval = 800 };
            _refreshTimer.Tick += (s, e) => RefreshList();
            _refreshTimer.Start();

            this.FormClosed += (s, e) => { _refreshTimer?.Stop(); _refreshTimer?.Dispose(); };
        }

        private void InitializeUI()
        {
            this.Text = "🔌 插件管理";
            this.Size = new Size(880, 620);
            this.MinimumSize = new Size(640, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.BackColor = Color.FromArgb(245, 245, 245);
            this.Font = new Font("Microsoft YaHei UI", 9.5F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);

            // ============== 顶部提示 ==============
            var lblTip = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(14, 0, 14, 0),
                Text = "小提示：双击插件名可快速重载，右键可打开更多操作。",
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(110, 110, 110),
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            this.Controls.Add(lblTip);

            // ============== 底部按钮区 ==============
            var bottomPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                Padding = new Padding(14, 14, 14, 14),
                BackColor = Color.FromArgb(235, 235, 235),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = false
            };

            _btnRefresh = MakeButton("🔄 刷新");
            _btnRefresh.Click += (s, e) => { _sendCommand("plugins"); RefreshList(); };

            _btnReload = MakeButton("🔁 重载");
            _btnReload.Enabled = false;
            _btnReload.Click += (s, e) => RunOnSelected(name =>
            {
                _sendCommand($"reload {name}");
                _sendCommand("plugins");
            });

            _btnUnload = MakeButton("🗑 卸载");
            _btnUnload.Enabled = false;
            _btnUnload.Click += (s, e) => RunOnSelected(name => ConfirmUnload(name));

            var spacer1 = new Panel { Width = 24, Height = 10 };

            var lblPerm = new Label
            {
                Text = "权限等级:",
                AutoSize = true,
                Margin = new Padding(0, 8, 6, 0),
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            _cmbPermission = new ComboBox
            {
                Width = 170,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,
                Margin = new Padding(0, 4, 6, 0)
            };
            _cmbPermission.Items.AddRange(new object[]
            {
                "0 - 只读",
                "1 - 可聊天",
                "2 - 可控制",
                "3 - 系统级（危险）"
            });

            _btnApplyPerm = MakeButton("应用权限");
            _btnApplyPerm.Enabled = false;
            _btnApplyPerm.Click += (s, e) => RunOnSelected(name =>
            {
                int level = _cmbPermission.SelectedIndex;
                if (level < 0) return;
                _sendCommand($"permission {name} {level}");
                // 主动拉一次最新列表，避免 800ms 定时器把旧值刷回来
                _sendCommand("plugins");
            });

            bottomPanel.Controls.Add(_btnRefresh);
            bottomPanel.Controls.Add(_btnReload);
            bottomPanel.Controls.Add(_btnUnload);
            bottomPanel.Controls.Add(spacer1);
            bottomPanel.Controls.Add(lblPerm);
            bottomPanel.Controls.Add(_cmbPermission);
            bottomPanel.Controls.Add(_btnApplyPerm);
            this.Controls.Add(bottomPanel);

            // ============== 详情区 ==============
            _lblDetail = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 74,
                Padding = new Padding(14, 8, 14, 8),
                BackColor = Color.FromArgb(250, 250, 250),
                ForeColor = Color.FromArgb(60, 60, 60),
                Text = "选中一个插件查看详情",
                AutoEllipsis = true
            };
            this.Controls.Add(_lblDetail);

            // ============== 列表 ==============
            _listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                GridLines = true,
                Font = new Font("Microsoft YaHei UI", 9F),
                BackColor = Color.White,
                HideSelection = false
            };
            _listView.Columns.Add("类型", 70);
            _listView.Columns.Add("名字", 200);
            _listView.Columns.Add("版本", 90);
            _listView.Columns.Add("作者", 130);
            _listView.Columns.Add("权限", 60);
            _listView.Columns.Add("说明", 260);

            _listView.SelectedIndexChanged += (s, e) => UpdateButtons();
            _listView.DoubleClick += (s, e) => RunOnSelected(name =>
            {
                _sendCommand($"reload {name}");
                _sendCommand("plugins");
            });
            _listView.Resize += (s, e) => AutoFitColumns();

            BuildContextMenu();

            this.Controls.Add(_listView);
            _listView.BringToFront();
            lblTip.BringToFront();
        }

        private void BuildContextMenu()
        {
            var contextMenu = new ContextMenuStrip();

            var miReload = new ToolStripMenuItem("🔁 重载");
            miReload.Click += (s, e) => RunOnSelected(name =>
            {
                _sendCommand($"reload {name}");
                _sendCommand("plugins");
            });
            contextMenu.Items.Add(miReload);

            var miUnload = new ToolStripMenuItem("🗑 卸载");
            miUnload.Click += (s, e) => RunOnSelected(name => ConfirmUnload(name));
            contextMenu.Items.Add(miUnload);

            contextMenu.Items.Add(new ToolStripSeparator());

            var miPerm = new ToolStripMenuItem("设置权限");
            string[] permLabels = {
                "0 - 只读",
                "1 - 可聊天",
                "2 - 可控制",
                "3 - 系统级（危险）"
            };
            for (int lv = 0; lv <= 3; lv++)
            {
                int captured = lv;
                var mi = new ToolStripMenuItem(permLabels[captured]);
                mi.Click += (s, e) => RunOnSelected(name =>
                {
                    _sendCommand($"permission {name} {captured}");
                    _sendCommand("plugins");  // 主动刷新
                });
                miPerm.DropDownItems.Add(mi);
            }
            contextMenu.Items.Add(miPerm);

            contextMenu.Items.Add(new ToolStripSeparator());

            var miDetail = new ToolStripMenuItem("查看详情");
            miDetail.Click += (s, e) => UpdateButtons();
            contextMenu.Items.Add(miDetail);

            _listView.ContextMenuStrip = contextMenu;

            // 右键时先选中鼠标下的那一行
            _listView.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    var hit = _listView.HitTest(e.Location);
                    if (hit.Item != null)
                    {
                        _listView.SelectedItems.Clear();
                        hit.Item.Selected = true;
                        hit.Item.Focused = true;
                    }
                }
            };
        }

        private void ConfirmUnload(string name)
        {
            var r = MessageBox.Show(this,
                $"确定要卸载插件「{name}」吗？", "确认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r == DialogResult.Yes)
            {
                _sendCommand($"unload {name}");
                _sendCommand("plugins");
            }
        }

        private Button MakeButton(string text)
        {
            return new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12, 4, 12, 4),
                Margin = new Padding(0, 0, 8, 0),
                FlatStyle = FlatStyle.System
            };
        }

        private void AutoFitColumns()
        {
            if (_listView == null || _listView.Columns.Count == 0) return;
            int totalFixed = 0;
            for (int i = 0; i < _listView.Columns.Count - 1; i++)
                totalFixed += _listView.Columns[i].Width;
            int rest = _listView.ClientSize.Width - totalFixed - 4;
            if (rest > 100)
                _listView.Columns[_listView.Columns.Count - 1].Width = rest;
        }

        private void RefreshList()
        {
            if (this.IsDisposed) return;
            var plugins = _getPlugins?.Invoke() ?? new List<PluginRow>();

            string selectedName = null;
            if (_listView.SelectedItems.Count > 0)
                selectedName = _listView.SelectedItems[0].SubItems[1].Text;

            _listView.BeginUpdate();
            _listView.Items.Clear();
            foreach (var p in plugins)
            {
                var item = new ListViewItem(p.Type);
                item.SubItems.Add(p.Name);
                item.SubItems.Add(p.Version ?? "");
                item.SubItems.Add(p.Author ?? "");
                item.SubItems.Add(p.Permission.ToString());
                item.SubItems.Add(p.Description ?? "");
                item.Tag = p;
                _listView.Items.Add(item);

                if (p.Name == selectedName)
                    item.Selected = true;
            }
            _listView.EndUpdate();

            if (plugins.Count == 0)
            {
                _lblDetail.Text = "当前没有已加载的插件。\r\n把插件放在 backend\\plugins\\（Node）或 backend\\plugins_py\\（Python）里，重启程序即可。";
            }
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            bool hasSel = _listView.SelectedItems.Count > 0;
            _btnReload.Enabled = hasSel;
            _btnUnload.Enabled = hasSel;
            _cmbPermission.Enabled = hasSel;
            _btnApplyPerm.Enabled = hasSel;

            if (!hasSel)
            {
                _lblDetail.Text = "选中一个插件查看详情";
                _cmbPermission.SelectedIndex = -1;
                return;
            }

            var item = _listView.SelectedItems[0];
            if (item.Tag is PluginRow p)
            {
                _lblDetail.Text =
                    $"名字: {p.Name}    类型: {p.Type}    版本: {p.Version}\r\n" +
                    $"作者: {p.Author}    当前权限: {p.Permission}\r\n" +
                    $"说明: {p.Description}";

                // ★ 关键：下拉框正在被用户操作时，不要覆盖用户的选择
                if (!_cmbPermission.Focused && p.Permission >= 0 && p.Permission <= 3)
                    _cmbPermission.SelectedIndex = p.Permission;
            }
        }

        private void RunOnSelected(Action<string> action)
        {
            if (_listView.SelectedItems.Count == 0) return;
            var item = _listView.SelectedItems[0];
            if (item.Tag is PluginRow p)
                action(p.Name);
        }
    }
}