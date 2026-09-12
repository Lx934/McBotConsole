using System;
using System.Drawing;
using System.Windows.Forms;

namespace JBSS261A
{
    /// <summary>
    /// 工具选择对话框：列出所有工具，双击打开
    /// </summary>
    public class ToolsDialog : Form
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _version;
        private readonly Action<string> _sendBackendCommand;
        private readonly Func<GuiMessage> _getLastProbeResult;

        public ToolsDialog(string host, int port, string version,
                           Action<string> sendBackendCommand,
                           Func<GuiMessage> getLastProbeResult)
        {
            _host = host;
            _port = port;
            _version = version;
            _sendBackendCommand = sendBackendCommand;
            _getLastProbeResult = getLastProbeResult;
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "🔧 工具";
            this.Size = new Size(620, 440);
            this.MinimumSize = new Size(520, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Microsoft YaHei UI", 9.5F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.BackColor = Color.FromArgb(245, 245, 245);

            // 顶部提示
            var lblTip = new Label
            {
                Dock = DockStyle.Top,
                Height = 32,
                Padding = new Padding(16, 0, 16, 0),
                Text = "选择要使用的工具（双击打开）",
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.FromArgb(235, 235, 235),
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            this.Controls.Add(lblTip);

            // 底部关闭
            var bottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.FromArgb(235, 235, 235),
                Padding = new Padding(0, 12, 20, 12)
            };
            var btnClose = new Button
            {
                Text = "关闭",
                Size = new Size(90, 32),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.System
            };
            btnClose.Click += (s, e) => this.Close();
            bottom.Controls.Add(btnClose);
            this.Controls.Add(bottom);

            // 工具列表
            var listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                GridLines = true,
                Font = new Font("Microsoft YaHei UI", 10F),
                BackColor = Color.White,
                HideSelection = false
            };
            listView.Columns.Add("工具", 180);
            listView.Columns.Add("说明", 380);

            // 工具 1：解析器
            var itemServerInfo = new ListViewItem("🔍 服务器信息解析器");
            itemServerInfo.SubItems.Add("DNS / Ping / TCP / SLP / Query / 插件探测");
            itemServerInfo.Tag = "serverinfo";
            listView.Items.Add(itemServerInfo);

            // 工具 2：调试命令行
            var itemDebug = new ListViewItem("💻 调试命令行");
            itemDebug.SubItems.Add("直接向后端发送调试指令，查看内部状态");
            itemDebug.Tag = "debug";
            listView.Items.Add(itemDebug);

            listView.DoubleClick += (s, e) =>
            {
                if (listView.SelectedItems.Count == 0) return;
                OpenTool(listView.SelectedItems[0].Tag as string);
            };
            listView.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && listView.SelectedItems.Count > 0)
                {
                    OpenTool(listView.SelectedItems[0].Tag as string);
                    e.SuppressKeyPress = true;
                }
            };

            this.Controls.Add(listView);
            listView.BringToFront();
            lblTip.BringToFront();

            if (listView.Items.Count > 0)
                listView.Items[0].Selected = true;
        }

        private void OpenTool(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            switch (key)
            {
                case "serverinfo":
                    using (var dlg = new ServerInfoDialog(_host, _port, _version))
                    {
                        dlg.SendBackendCommand = _sendBackendCommand;
                        dlg.GetLastProbeResult = _getLastProbeResult;
                        dlg.ShowDialog(this);
                    }
                    break;

                case "debug":
                    using (var dlg = new DebugConsoleDialog(_sendBackendCommand))
                    {
                        dlg.ShowDialog(this);
                    }
                    break;
            }
        }
    }
}