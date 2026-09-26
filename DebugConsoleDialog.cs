using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace JBSS261A
{
    /// <summary>
    /// 调试命令行：只能输入英文指令和 CMD 风格命令
    /// </summary>
    public class DebugConsoleDialog : Form
    {
        private readonly Action<string> _sendBackendCommand;

        private RichTextBox _rtbOutput;
        private TextBox _txtInput;
        private Button _btnSend;
        private Button _btnClear;
        private Button _btnExport;
        private Label _lblHint;

        public DebugConsoleDialog(Action<string> sendBackendCommand)
        {
            _sendBackendCommand = sendBackendCommand;
            InitializeUI();
            PrintWelcome();
        }

        private void InitializeUI()
        {
            this.Text = "💻 调试命令行";
            this.Size = new Size(900, 640);
            this.MinimumSize = new Size(700, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Consolas", 10F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.BackColor = Color.FromArgb(20, 20, 20);

            // 顶部提示
            var top = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(45, 45, 48)
            };
            _lblHint = new Label
            {
                Dock = DockStyle.Fill,
                Text = "  只能输入英文指令。输入 help 查看所有命令。",
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            top.Controls.Add(_lblHint);

            // 底部输入
            var bottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Color.FromArgb(37, 37, 38),
                Padding = new Padding(8)
            };

            var lblPrompt = new Label
            {
                Text = "dbg>",
                Location = new Point(12, 15),
                AutoSize = true,
                ForeColor = Color.FromArgb(78, 201, 176),
                Font = new Font("Consolas", 11F, FontStyle.Bold)
            };

            _txtInput = new TextBox
            {
                Location = new Point(56, 12),
                Height = 26,
                Width = 700,
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(26, 26, 26),
                BorderStyle = BorderStyle.FixedSingle,
                ForeColor = Color.FromArgb(230, 230, 230),
                Font = new Font("Consolas", 10F)
            };
            _txtInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    SendInput();
                    e.SuppressKeyPress = true;
                }
            };

            _btnSend = new Button
            {
                Text = "执行",
                Location = new Point(766, 11),
                Size = new Size(60, 28),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                FlatStyle = FlatStyle.System
            };
            _btnSend.Click += (s, e) => SendInput();

            bottom.Controls.Add(lblPrompt);
            bottom.Controls.Add(_txtInput);
            bottom.Controls.Add(_btnSend);

            // 输出区
            _rtbOutput = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.FromArgb(220, 220, 220),
                Font = new Font("Consolas", 10F),
                WordWrap = false
            };

            // 工具按钮栏
            var toolsBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(8, 4, 8, 4)
            };

            var btnHelp = MakeToolButton("❓ 帮助");
            btnHelp.Click += (s, e) => { _txtInput.Text = "help"; SendInput(); };

            _btnClear = MakeToolButton("🗑 清空");
            _btnClear.Location = new Point(100, 4);
            _btnClear.Click += (s, e) => { _rtbOutput.Clear(); PrintWelcome(); };

            _btnExport = MakeToolButton("💾 导出日志");
            _btnExport.Location = new Point(200, 4);
            _btnExport.Click += (s, e) => ExportLog();

            toolsBar.Controls.Add(btnHelp);
            toolsBar.Controls.Add(_btnClear);
            toolsBar.Controls.Add(_btnExport);

            this.Controls.Add(_rtbOutput);
            this.Controls.Add(toolsBar);
            this.Controls.Add(bottom);
            this.Controls.Add(top);

            _rtbOutput.BringToFront();
        }

        private Button MakeToolButton(string text)
        {
            return new Button
            {
                Text = text,
                Location = new Point(8, 4),
                Size = new Size(88, 28),
                FlatStyle = FlatStyle.System
            };
        }

        private void PrintWelcome()
        {
            AppendLine("════════════════════════════════════════════════", Color.FromArgb(86, 156, 214));
            AppendLine("  调试命令行 v1.0", Color.FromArgb(86, 156, 214));
            AppendLine("  输入 help 查看所有命令。只能输入英文指令。", Color.FromArgb(180, 180, 180));
            AppendLine("════════════════════════════════════════════════", Color.FromArgb(86, 156, 214));
            AppendLine("");
        }

        private void SendInput()
        {
            string text = _txtInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // 只允许 ASCII 英文
            if (ContainsNonAscii(text))
            {
                AppendLine("⚠️ 只允许输入英文指令（ASCII 字符）", Color.FromArgb(220, 180, 100));
                AppendLine("");
                return;
            }

            AppendLine("dbg> " + text, Color.FromArgb(78, 201, 176));

            // 本地命令
            string lower = text.ToLower();
            if (lower == "clear" || lower == "cls")
            {
                _rtbOutput.Clear();
                PrintWelcome();
                _txtInput.Clear();
                return;
            }
            if (lower == "exit" || lower == "quit")
            {
                this.Close();
                return;
            }

            // 发给后端
            if (_sendBackendCommand != null)
            {
                _sendBackendCommand("debug " + text);
            }
            else
            {
                AppendLine("⚠️ 后端未连接，无法执行", Color.FromArgb(220, 180, 100));
                AppendLine("");
            }

            _txtInput.Clear();
        }

        private bool ContainsNonAscii(string s)
        {
            foreach (char c in s)
                if (c > 127) return true;
            return false;
        }

        // 由 MainForm 调用，把后端返回的结果追加到这里
        public void AppendDebugResult(string text, string level)
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => AppendDebugResult(text, level)));
                return;
            }

            Color color = Color.FromArgb(220, 220, 220);
            switch ((level ?? "").ToLower())
            {
                case "error": color = Color.FromArgb(244, 135, 113); break;
                case "warn": color = Color.FromArgb(220, 220, 170); break;
                case "info": color = Color.FromArgb(220, 220, 220); break;
                case "ok": color = Color.FromArgb(120, 200, 120); break;
            }

            AppendLine(text, color);
        }

        public void FinishDebugRequest()
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(FinishDebugRequest));
                return;
            }
            AppendLine("");
        }

        /// <summary>
        /// 追加一行。color 不传则用默认浅灰色。
        /// </summary>
        private void AppendLine(string text, Color? color = null)
        {
            Color c = color ?? Color.FromArgb(220, 220, 220);
            _rtbOutput.SelectionStart = _rtbOutput.TextLength;
            _rtbOutput.SelectionLength = 0;
            _rtbOutput.SelectionColor = c;
            _rtbOutput.AppendText(text + "\n");
            _rtbOutput.SelectionStart = _rtbOutput.TextLength;
            _rtbOutput.ScrollToCaret();
        }

        private void ExportLog()
        {
            using (var sfd = new SaveFileDialog
            {
                Filter = "文本文件|*.txt",
                FileName = $"debug_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            })
            {
                if (sfd.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    System.IO.File.WriteAllText(sfd.FileName, _rtbOutput.Text, Encoding.UTF8);
                    MessageBox.Show(this, "导出成功：" + sfd.FileName, "完成",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "导出失败：" + ex.Message, "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}