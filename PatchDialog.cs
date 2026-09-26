using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace JBSS261A.Patching
{
    public class PatchDialog : Form
    {
        private readonly PatchManager _manager;
        private ListView _listView;
        private RichTextBox _rtbDetail;
        private Button _btnRefresh;
        private Button _btnApply;
        private Button _btnRollback;
        private Button _btnOpenFolder;
        private Button _btnClose;

        public PatchDialog(PatchManager manager)
        {
            _manager = manager;
            InitializeUI();
            RefreshPatches();
        }

        private void InitializeUI()
        {
            this.Text = "🧩 补丁管理";
            this.Size = new Size(1000, 660);
            this.MinimumSize = new Size(820, 540);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Microsoft YaHei UI", 9.5F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);

            var top = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.FromArgb(235, 235, 235),
                Padding = new Padding(14, 12, 14, 12)
            };

            _btnRefresh = MakeButton("🔄 刷新", 14, 96);
            _btnRefresh.Click += (s, e) => RefreshPatches();

            _btnOpenFolder = MakeButton("📂 打开补丁文件夹", 116, 150);
            _btnOpenFolder.Click += (s, e) =>
            {
                try { System.Diagnostics.Process.Start("explorer.exe", _manager.PatchesDir); }
                catch { }
            };

            _btnApply = MakeButton("✅ 应用", 276, 96);
            _btnApply.Enabled = false;
            _btnApply.Click += (s, e) => ApplySelected();

            _btnRollback = MakeButton("↩ 回滚", 378, 96);
            _btnRollback.Enabled = false;
            _btnRollback.Click += (s, e) => RollbackSelected();

            top.Controls.Add(_btnRefresh);
            top.Controls.Add(_btnOpenFolder);
            top.Controls.Add(_btnApply);
            top.Controls.Add(_btnRollback);

            var bottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.FromArgb(235, 235, 235),
                Padding = new Padding(0, 12, 20, 12)
            };

            _btnClose = new Button
            {
                Text = "关闭",
                Size = new Size(90, 32),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.System
            };
            _btnClose.Click += (s, e) => this.Close();
            bottom.Controls.Add(_btnClose);

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 2,
                SplitterDistance = 420
            };

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
            _listView.Columns.Add("状态", 90);
            _listView.Columns.Add("补丁 ID", 180);
            _listView.Columns.Add("名称", 200);
            _listView.Columns.Add("版本", 60);
            _listView.SelectedIndexChanged += (s, e) => OnSelectionChanged();
            split.Panel1.Controls.Add(_listView);

            _rtbDetail = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(26, 26, 26),
                ForeColor = Color.FromArgb(230, 230, 230),
                Font = new Font("Consolas", 10F),
                WordWrap = true
            };
            split.Panel2.Controls.Add(_rtbDetail);

            this.Controls.Add(split);
            this.Controls.Add(bottom);
            this.Controls.Add(top);
        }

        private Button MakeButton(string text, int x, int width)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, 12),
                Size = new Size(width, 32),
                FlatStyle = FlatStyle.System
            };
        }

        private async void RefreshPatches()
        {
            _listView.Items.Clear();
            _rtbDetail.Text = "正在扫描补丁...\n";

            var files = _manager.ScanPatchFiles();
            if (files.Count == 0)
            {
                _rtbDetail.Text = "没有找到补丁文件。\n\n把 .mcpatch 文件放到：\n" + _manager.PatchesDir;
                UpdateButtons();
                return;
            }

            _rtbDetail.Clear();
            foreach (var file in files)
            {
                AppendDetail($"验证中: {Path.GetFileName(file)} ...");
                var result = await _manager.LoadAndVerifyAsync(file);

                var item = new ListViewItem();
                switch (result.Status)
                {
                    case PatchStatus.Official:
                        item.Text = "✅ 官方";
                        item.ForeColor = Color.FromArgb(0, 130, 0);
                        break;
                    case PatchStatus.NetworkError:
                        item.Text = "⚠️ 待验证";
                        item.ForeColor = Color.FromArgb(180, 130, 0);
                        break;
                    case PatchStatus.Unofficial:
                        item.Text = "❌ 非官方";
                        item.ForeColor = Color.FromArgb(200, 50, 50);
                        break;
                    case PatchStatus.SignatureInvalid:
                        item.Text = "❌ 签名错";
                        item.ForeColor = Color.FromArgb(200, 50, 50);
                        break;
                    case PatchStatus.ManifestInvalid:
                        item.Text = "❌ 校验错";
                        item.ForeColor = Color.FromArgb(200, 50, 50);
                        break;
                    case PatchStatus.AlreadyApplied:
                        item.Text = "✔ 已应用";
                        item.ForeColor = Color.FromArgb(100, 100, 100);
                        break;
                    default:
                        item.Text = "❓ 未知";
                        item.ForeColor = Color.FromArgb(150, 150, 150);
                        break;
                }

                item.SubItems.Add(result.Info?.Id ?? "(未知)");
                item.SubItems.Add(result.Info?.Name ?? "(未知)");
                item.SubItems.Add(result.Info?.Version ?? "?");
                item.Tag = result;
                _listView.Items.Add(item);
            }

            AppendDetail("");
            AppendDetail($"扫描完成，共 {files.Count} 个补丁文件。");
            UpdateButtons();
        }

        private void AppendDetail(string s)
        {
            _rtbDetail.SelectionStart = _rtbDetail.TextLength;
            _rtbDetail.SelectionColor = Color.FromArgb(200, 200, 200);
            _rtbDetail.AppendText(s + "\n");
            _rtbDetail.SelectionStart = _rtbDetail.TextLength;
            _rtbDetail.ScrollToCaret();
        }

        private void OnSelectionChanged()
        {
            if (_listView.SelectedItems.Count == 0) { UpdateButtons(); return; }
            var result = _listView.SelectedItems[0].Tag as PatchLoadResult;
            if (result == null) { UpdateButtons(); return; }

            var sb = new System.Text.StringBuilder();
            var info = result.Info;

            sb.AppendLine("═══════════════════════════════════════════");
            if (result.Status == PatchStatus.Official)
                sb.AppendLine("  ✅ 官方认证补丁");
            else if (result.Status == PatchStatus.NetworkError)
                sb.AppendLine("  ⚠️ 网络问题，未能验证官方来源");
            else if (result.Status == PatchStatus.Unofficial)
                sb.AppendLine("  ❌ 非官方补丁，已拒绝加载");
            else if (result.Status == PatchStatus.SignatureInvalid)
                sb.AppendLine("  ❌ 签名验证失败");
            else if (result.Status == PatchStatus.ManifestInvalid)
                sb.AppendLine("  ❌ 清单校验失败");
            else if (result.Status == PatchStatus.AlreadyApplied)
                sb.AppendLine("  ✔ 已应用过");
            else
                sb.AppendLine("  ❓ 未知状态");
            sb.AppendLine("═══════════════════════════════════════════");

            if (info != null)
            {
                sb.AppendLine($"补丁 ID:   {info.Id}");
                sb.AppendLine($"名称:      {info.Name}");
                sb.AppendLine($"版本:      {info.Version}");
                sb.AppendLine($"作者:      {info.Author}");
                sb.AppendLine($"风险等级:  {info.Risk}");
                sb.AppendLine($"目标版本:  {info.TargetVersion}");
                sb.AppendLine($"Nonce:     {Shorten(info.Nonce, 24)}");
                sb.AppendLine($"创建时间:  {UnixToLocal(info.CreatedAt)}");
                sb.AppendLine();
                sb.AppendLine("【发行说明】");
                sb.AppendLine(info.ReleaseNotes ?? "(无)");
                sb.AppendLine();
            }

            if (result.Manifest != null && result.Manifest.Files != null)
            {
                sb.AppendLine("【将替换的文件】");
                foreach (var f in result.Manifest.Files)
                    sb.AppendLine($"  · {f.Path}  ({FormatSize(f.Size)})");
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(result.OfficialTag))
            {
                sb.AppendLine($"官方标签: {result.OfficialTag}");
                sb.AppendLine($"提交哈希: {Shorten(result.OfficialCommitSha, 16)}");
                sb.AppendLine();
            }

            sb.AppendLine("【验证结果】");
            sb.AppendLine(result.Message ?? "");

            _rtbDetail.Text = sb.ToString();
            UpdateButtons();
        }

        private string Shorten(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }

        private string UnixToLocal(long unix)
        {
            if (unix <= 0) return "(未知)";
            return DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
            return (bytes / 1024.0 / 1024.0).ToString("F2") + " MB";
        }

        private void UpdateButtons()
        {
            if (_listView.SelectedItems.Count == 0)
            {
                _btnApply.Enabled = false;
                _btnRollback.Enabled = false;
                return;
            }

            var result = _listView.SelectedItems[0].Tag as PatchLoadResult;
            if (result == null)
            {
                _btnApply.Enabled = false;
                _btnRollback.Enabled = false;
                return;
            }

            bool canApply = (result.Status == PatchStatus.Official) ||
                            (result.Status == PatchStatus.NetworkError && result.CanProceedWithSignatureOnly);
            _btnApply.Enabled = canApply;
            _btnRollback.Enabled = result.Status == PatchStatus.AlreadyApplied;
        }

        private void ApplySelected()
        {
            if (_listView.SelectedItems.Count == 0) return;
            var result = _listView.SelectedItems[0].Tag as PatchLoadResult;
            if (result == null) return;

            string warn = "即将应用补丁：\n\n" +
                          $"名称: {result.Info.Name}\n" +
                          $"版本: {result.Info.Version}\n" +
                          $"作者: {result.Info.Author}\n\n";

            if (result.Status == PatchStatus.NetworkError)
                warn += "⚠️ 该补丁未通过官方来源验证（网络原因）。\n" +
                        "仅通过了本地签名验证。\n\n";

            if (result.Info.ReplaceSelf)
                warn += "⚠️ 该补丁会替换主程序，程序将自动关闭并重启。\n\n";

            warn += "确认应用？";

            var r = MessageBox.Show(this, warn, "应用补丁",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) return;

            bool ok = _manager.ApplyPatch(result);
            if (!ok)
            {
                MessageBox.Show(this, "应用失败，请查看日志。", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Exit();
        }

        private void RollbackSelected()
        {
            if (_listView.SelectedItems.Count == 0) return;
            var result = _listView.SelectedItems[0].Tag as PatchLoadResult;
            if (result == null || result.Info == null) return;

            var r = MessageBox.Show(this,
                $"确定回滚补丁 {result.Info.Name} 吗？\n" +
                "程序将恢复被替换之前的文件。\n\n" +
                "建议回滚后重启程序。",
                "回滚补丁",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) return;

            bool ok = _manager.RollbackPatch(result.Info.Id);
            if (ok)
            {
                MessageBox.Show(this, "回滚成功。请重启程序。", "完成",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshPatches();
            }
            else
            {
                MessageBox.Show(this, "回滚失败，请查看日志。", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}