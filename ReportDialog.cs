using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace JBSS261A
{
    public class ReportDialog : Form
    {
        private readonly string _backendDir;
        private ListView _listReports;
        private RichTextBox _rtbDetail;
        private Button _btnRefresh;
        private Button _btnOpenFolder;
        private Button _btnExportCsv;
        private Button _btnExportHtml;
        private Button _btnDelete;
        private Button _btnClose;
        private List<RunReport> _reports = new List<RunReport>();

        public ReportDialog(string backendDir)
        {
            _backendDir = backendDir;
            InitializeUI();
            RefreshList();
        }

        private void InitializeUI()
        {
            this.Text = "📊 运行报告";
            this.Size = new Size(1020, 680);
            this.MinimumSize = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Microsoft YaHei UI", 9.5F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.BackColor = Color.FromArgb(245, 245, 245);

            // 顶部按钮
            var top = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.FromArgb(235, 235, 235),
                Padding = new Padding(14, 12, 14, 12)
            };

            _btnRefresh = MakeButton("🔄 刷新");
            _btnRefresh.Location = new Point(14, 12);
            _btnRefresh.Click += (s, e) => RefreshList();

            _btnOpenFolder = MakeButton("📂 打开报告文件夹");
            _btnOpenFolder.Location = new Point(116, 12);
            _btnOpenFolder.Click += (s, e) =>
            {
                try
                {
                    string dir = ReportManager.GetReportsDir(_backendDir);
                    Process.Start("explorer.exe", dir);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "无法打开文件夹：" + ex.Message);
                }
            };

            _btnExportCsv = MakeButton("📄 导出 CSV");
            _btnExportCsv.Location = new Point(268, 12);
            _btnExportCsv.Click += (s, e) => Export("csv");

            _btnExportHtml = MakeButton("🌐 导出 HTML");
            _btnExportHtml.Location = new Point(378, 12);
            _btnExportHtml.Click += (s, e) => Export("html");

            _btnDelete = MakeButton("🗑 删除");
            _btnDelete.BackColor = Color.FromArgb(200, 80, 60);
            _btnDelete.ForeColor = Color.White;
            _btnDelete.Location = new Point(500, 12);
            _btnDelete.Click += (s, e) => DeleteSelected();

            top.Controls.Add(_btnRefresh);
            top.Controls.Add(_btnOpenFolder);
            top.Controls.Add(_btnExportCsv);
            top.Controls.Add(_btnExportHtml);
            top.Controls.Add(_btnDelete);

            // 底部关闭
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

            // 中间 SplitContainer
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 2,
                SplitterDistance = 380
            };

            _listReports = new ListView
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
            _listReports.Columns.Add("开始时间", 140);
            _listReports.Columns.Add("服务器", 140);
            _listReports.Columns.Add("成功/总数", 90);
            _listReports.Columns.Add("成功率", 70);
            _listReports.SelectedIndexChanged += (s, e) => ShowDetail();
            split.Panel1.Controls.Add(_listReports);

            _rtbDetail = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(26, 26, 26),
                ForeColor = Color.FromArgb(230, 230, 230),
                Font = new Font("Consolas", 10F),
                WordWrap = false
            };
            split.Panel2.Controls.Add(_rtbDetail);

            this.Controls.Add(split);
            this.Controls.Add(bottom);
            this.Controls.Add(top);
        }

        private Button MakeButton(string text)
        {
            return new Button
            {
                Text = text,
                Size = new Size(96, 32),
                FlatStyle = FlatStyle.System
            };
        }

        private void RefreshList()
        {
            _reports = ReportManager.LoadAll(_backendDir);

            _listReports.BeginUpdate();
            _listReports.Items.Clear();
            foreach (var r in _reports)
            {
                var item = new ListViewItem(r.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                item.SubItems.Add(r.Server ?? "");
                item.SubItems.Add($"{r.SuccessCount}/{r.TotalAttempts}");
                item.SubItems.Add(r.SuccessRate.ToString("F1") + "%");
                item.Tag = r;
                _listReports.Items.Add(item);
            }
            _listReports.EndUpdate();

            if (_listReports.Items.Count > 0)
                _listReports.Items[0].Selected = true;
            else
                _rtbDetail.Text = "暂无报告。运行一次任务后会自动保存。";
        }

        private void ShowDetail()
        {
            if (_listReports.SelectedItems.Count == 0) return;
            var r = _listReports.SelectedItems[0].Tag as RunReport;
            if (r == null) return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"  运行报告  {r.Id}");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"开始时间:   {r.StartedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"结束时间:   {r.FinishedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"服务器:     {r.Server}");
            sb.AppendLine($"游戏版本:   {r.Version}");
            sb.AppendLine($"总尝试数:   {r.TotalAttempts}");
            sb.AppendLine($"成功:       {r.SuccessCount}");
            sb.AppendLine($"失败:       {r.FailureCount}");
            sb.AppendLine($"成功率:     {r.SuccessRate:F2}%");
            sb.AppendLine($"平均耗时:   {r.AvgLatencyMs:F0} ms");
            sb.AppendLine();
            sb.AppendLine("─────────── 详细记录 ───────────");
            sb.AppendLine($"{"#",-4} {"玩家名",-18} {"结果",-6} {"耗时",-10} {"重连",-6} {"错误",-20}");
            sb.AppendLine(new string('─', 78));

            foreach (var b in r.Bots)
            {
                string result = b.Success ? "成功" : "失败";
                string name = (b.Username ?? "").PadRight(18);
                if (name.Length > 18) name = name.Substring(0, 18);
                string dur = b.DurationMs + "ms";
                string recon = b.ReconnectCount > 0 ? b.ReconnectCount.ToString() : "-";
                string err = b.ErrorType ?? "";
                if (err.Length > 20) err = err.Substring(0, 20);

                sb.AppendLine($"{b.BotId,-4} {name,-18} {result,-6} {dur,-10} {recon,-6} {err,-20}");
            }

            _rtbDetail.Text = sb.ToString();
            _rtbDetail.SelectionStart = 0;
        }

        private void Export(string format)
        {
            if (_listReports.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "请先选择一份报告", "提示");
                return;
            }
            var r = _listReports.SelectedItems[0].Tag as RunReport;
            if (r == null) return;

            string ext = format == "csv" ? "csv" : "html";
            using (var sfd = new SaveFileDialog
            {
                Filter = format == "csv" ? "CSV 文件|*.csv" : "HTML 文件|*.html",
                FileName = $"report_{r.StartedAt:yyyyMMdd_HHmmss}.{ext}"
            })
            {
                if (sfd.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    if (format == "csv")
                        ReportManager.ExportCsv(r, sfd.FileName);
                    else
                        ReportManager.ExportHtml(r, sfd.FileName);

                    var result = MessageBox.Show(this,
                        "导出成功！\n\n要打开文件吗？",
                        "完成", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                        Process.Start(sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "导出失败：" + ex.Message,
                        "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DeleteSelected()
        {
            if (_listReports.SelectedItems.Count == 0) return;
            var r = _listReports.SelectedItems[0].Tag as RunReport;
            if (r == null) return;

            if (MessageBox.Show(this,
                $"确定要删除这份报告吗？\n{r.StartedAt:yyyy-MM-dd HH:mm:ss}  {r.Server}",
                "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            ReportManager.Delete(r, _backendDir);
            RefreshList();
        }
    }
}