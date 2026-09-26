using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace JBSS261A
{
    public class UpdateDialog : Form
    {
        private readonly UpdateInfo _info;

        public UpdateDialog(UpdateInfo info)
        {
            _info = info;
            InitializeUI();
            RenderContent();
        }

        private void InitializeUI()
        {
            this.Text = "检查更新";
            this.Size = new Size(520, 460);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.Font = new Font("Microsoft YaHei UI", 9F);
        }

        private void RenderContent()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(30, 30, 30)
            };
            this.Controls.Add(panel);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                BackColor = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // 标题
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // 版本行
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // 补丁提示
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 说明
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // 底部提示
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // 按钮

            // 标题
            string title;
            Color titleColor;
            if (!string.IsNullOrEmpty(_info.Error))
            {
                title = "检查更新失败";
                titleColor = Color.FromArgb(255, 120, 120);
            }
            else if (_info.HasNewVersion)
            {
                title = "发现新版本";
                titleColor = Color.FromArgb(120, 220, 120);
            }
            else
            {
                title = "当前已是最新版本";
                titleColor = Color.FromArgb(180, 180, 180);
            }

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold),
                ForeColor = titleColor,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };
            layout.Controls.Add(lblTitle, 0, 0);

            // 版本行
            var lblVersions = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                Margin = new Padding(0, 0, 0, 8)
            };
            if (!string.IsNullOrEmpty(_info.Error))
            {
                lblVersions.Text = $"当前版本：{_info.CurrentVersion}\n\n{_info.Error}";
            }
            else
            {
                string pub = "";
                if (!string.IsNullOrEmpty(_info.PublishedAt) &&
                    DateTime.TryParse(_info.PublishedAt, out var dt))
                {
                    pub = $"\n发布时间：{dt.ToLocalTime():yyyy-MM-dd HH:mm}";
                }
                lblVersions.Text =
                    $"当前版本：{_info.CurrentVersion}\n" +
                    $"最新版本：{_info.LatestVersion}{pub}";
            }
            layout.Controls.Add(lblVersions, 0, 1);

            // 补丁提示
            if (_info.HasNewVersion && _info.AppliedPatches != null && _info.AppliedPatches.Count > 0)
            {
                var patchLines = string.Join("\n  · ", _info.AppliedPatches);
                var lblPatch = new Label
                {
                    Text = $"⚠️ 已检测到以下补丁：\n  · {patchLines}\n" +
                           "新版可能已包含这些修复，旧补丁可能失效。",
                    ForeColor = Color.FromArgb(255, 200, 100),
                    AutoSize = true,
                    MaximumSize = new Size(460, 0),
                    Margin = new Padding(0, 0, 0, 8)
                };
                layout.Controls.Add(lblPatch, 0, 2);
            }
            else
            {
                layout.Controls.Add(new Label { Text = "", AutoSize = true }, 0, 2);
            }

            // 更新说明
            if (_info.HasNewVersion && !string.IsNullOrEmpty(_info.ReleaseBody))
            {
                var txtBody = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = Color.FromArgb(230, 230, 230),
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Consolas", 9F),
                    Text = _info.ReleaseBody
                };
                layout.Controls.Add(txtBody, 0, 3);
            }
            else
            {
                layout.Controls.Add(new Label { Text = "", AutoSize = true }, 0, 3);
            }

            // 底部提示
            var lblHint = new Label
            {
                Text = _info.HasNewVersion
                    ? "提示：大版本更新请下载完整包手动替换，不要用补丁覆盖。\n" +
                      "config.json / plugins / patches 等用户文件会保留。"
                    : "",
                ForeColor = Color.FromArgb(160, 160, 160),
                AutoSize = true,
                MaximumSize = new Size(460, 0),
                Margin = new Padding(0, 8, 0, 8)
            };
            layout.Controls.Add(lblHint, 0, 4);

            // 按钮行
            var btnPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(0)
            };

            var btnClose = new Button
            {
                Text = "关闭",
                Size = new Size(90, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                DialogResult = DialogResult.Cancel
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            btnClose.Click += (s, e) => this.Close();

            var btnOpen = new Button
            {
                Text = "打开下载页",
                Size = new Size(120, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Enabled = !string.IsNullOrEmpty(_info.HtmlUrl)
            };
            btnOpen.FlatAppearance.BorderColor = Color.FromArgb(0, 140, 235);
            btnOpen.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _info.HtmlUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                        "无法打开浏览器：" + ex.Message,
                        "错误",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };

            btnPanel.Controls.Add(btnClose);
            btnPanel.Controls.Add(btnOpen);
            layout.Controls.Add(btnPanel, 0, 5);

            panel.Controls.Add(layout);
        }
    }
}