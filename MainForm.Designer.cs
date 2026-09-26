namespace JBSS261A
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // ==================== 菜单 ====================
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem menuFile;
        private System.Windows.Forms.ToolStripMenuItem menuSettings;
        private System.Windows.Forms.ToolStripMenuItem menuPlugins;
        private System.Windows.Forms.ToolStripMenuItem menuExit;
        private System.Windows.Forms.ToolStripMenuItem menuLog;
        private System.Windows.Forms.ToolStripMenuItem menuStatus;
        private System.Windows.Forms.ToolStripMenuItem menuClear;
        private System.Windows.Forms.ToolStripMenuItem menuPackLog;
        private System.Windows.Forms.ToolStripMenuItem menuHelp;
        private System.Windows.Forms.ToolStripMenuItem menuUsageHelp;
        private System.Windows.Forms.ToolStripMenuItem menuCheckUpdate;   // ★ 新增
        private System.Windows.Forms.ToolStripMenuItem menuAbout;

        // ==================== 顶部工具栏 ====================
        private System.Windows.Forms.Panel toolbarPanel;
        private System.Windows.Forms.FlowLayoutPanel row1;
        private System.Windows.Forms.FlowLayoutPanel row2;
        private System.Windows.Forms.Label lblIp;
        private System.Windows.Forms.TextBox txtIp;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.ComboBox cmbVersion;
        private System.Windows.Forms.Label lblCount;
        private System.Windows.Forms.TextBox txtCount;
        private System.Windows.Forms.Label lblConcurrency;
        private System.Windows.Forms.TextBox txtConcurrency;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnPause;
        private System.Windows.Forms.Button btnResume;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnSettings;
        private System.Windows.Forms.Button btnTools;
        private System.Windows.Forms.Button btnReports;
        private System.Windows.Forms.Button btnPatches;
        private System.Windows.Forms.Button btnPlugins;
        private System.Windows.Forms.Button btnHelp;

        // ==================== 隐藏字段 ====================
        private System.Windows.Forms.TextBox txtRetryDelay;
        private System.Windows.Forms.TextBox txtConnInterval;
        private System.Windows.Forms.TextBox txtTimeout;
        private System.Windows.Forms.TextBox txtPrefix;
        private System.Windows.Forms.TextBox txtAutoDisconnect;
        private System.Windows.Forms.CheckBox chkStay;

        // ==================== 中间区域 ====================
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.ListBox lstPlugins;

        // ==================== 底部命令栏 ====================
        private System.Windows.Forms.Panel cmdPanel;
        private System.Windows.Forms.TableLayoutPanel cmdTable;
        private System.Windows.Forms.Label lblPrompt;
        private System.Windows.Forms.TextBox txtCommand;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Button btnHelpInline;

        // ==================== 状态栏 ====================
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripStatusLabel lblUpdate;   // ★ 新增

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.menuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.menuPlugins = new System.Windows.Forms.ToolStripMenuItem();
            this.menuExit = new System.Windows.Forms.ToolStripMenuItem();
            this.menuLog = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStatus = new System.Windows.Forms.ToolStripMenuItem();
            this.menuClear = new System.Windows.Forms.ToolStripMenuItem();
            this.menuPackLog = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.menuUsageHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.menuCheckUpdate = new System.Windows.Forms.ToolStripMenuItem();   // ★ 新增
            this.menuAbout = new System.Windows.Forms.ToolStripMenuItem();

            this.toolbarPanel = new System.Windows.Forms.Panel();
            this.row1 = new System.Windows.Forms.FlowLayoutPanel();
            this.row2 = new System.Windows.Forms.FlowLayoutPanel();
            this.lblIp = new System.Windows.Forms.Label();
            this.txtIp = new System.Windows.Forms.TextBox();
            this.lblPort = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.lblVersion = new System.Windows.Forms.Label();
            this.cmbVersion = new System.Windows.Forms.ComboBox();
            this.lblCount = new System.Windows.Forms.Label();
            this.txtCount = new System.Windows.Forms.TextBox();
            this.lblConcurrency = new System.Windows.Forms.Label();
            this.txtConcurrency = new System.Windows.Forms.TextBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnPause = new System.Windows.Forms.Button();
            this.btnResume = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnSettings = new System.Windows.Forms.Button();
            this.btnTools = new System.Windows.Forms.Button();
            this.btnReports = new System.Windows.Forms.Button();
            this.btnPatches = new System.Windows.Forms.Button();
            this.btnPlugins = new System.Windows.Forms.Button();
            this.btnHelp = new System.Windows.Forms.Button();

            this.txtRetryDelay = new System.Windows.Forms.TextBox();
            this.txtConnInterval = new System.Windows.Forms.TextBox();
            this.txtTimeout = new System.Windows.Forms.TextBox();
            this.txtPrefix = new System.Windows.Forms.TextBox();
            this.txtAutoDisconnect = new System.Windows.Forms.TextBox();
            this.chkStay = new System.Windows.Forms.CheckBox();

            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.lstPlugins = new System.Windows.Forms.ListBox();

            this.cmdPanel = new System.Windows.Forms.Panel();
            this.cmdTable = new System.Windows.Forms.TableLayoutPanel();
            this.lblPrompt = new System.Windows.Forms.Label();
            this.txtCommand = new System.Windows.Forms.TextBox();
            this.btnHelpInline = new System.Windows.Forms.Button();
            this.btnSend = new System.Windows.Forms.Button();

            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblUpdate = new System.Windows.Forms.ToolStripStatusLabel();   // ★ 新增

            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.menuStrip.SuspendLayout();
            this.toolbarPanel.SuspendLayout();
            this.row1.SuspendLayout();
            this.row2.SuspendLayout();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.cmdPanel.SuspendLayout();
            this.cmdTable.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();

            // ==================== menuStrip ====================
            this.menuStrip.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            this.menuStrip.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.menuFile, this.menuLog, this.menuHelp });

            this.menuFile.Text = "文件(&F)";
            this.menuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.menuSettings, this.menuPlugins, this.menuExit });
            this.menuSettings.Text = "⚙ 设置...";
            this.menuPlugins.Text = "🔌 插件管理...";
            this.menuExit.Text = "退出";

            this.menuLog.Text = "日志(&L)";
            this.menuLog.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.menuStatus, this.menuClear, this.menuPackLog });
            this.menuStatus.Text = "查看状态";
            this.menuClear.Text = "清空日志";
            this.menuPackLog.Text = "打包日志为 zip";

            this.menuHelp.Text = "帮助(&H)";
            this.menuHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.menuUsageHelp, this.menuCheckUpdate, this.menuAbout });
            this.menuUsageHelp.Text = "📖 使用帮助...";
            this.menuCheckUpdate.Text = "🔄 检查更新";               // ★ 新增
            this.menuAbout.Text = "ℹ 关于";

            // ==================== toolbarPanel ====================
            this.toolbarPanel.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            this.toolbarPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.toolbarPanel.Height = 100;
            this.toolbarPanel.MinimumSize = new System.Drawing.Size(0, 100);
            this.toolbarPanel.Controls.Add(this.row1);
            this.toolbarPanel.Controls.Add(this.row2);

            // ==================== row1 ====================
            this.row1.AutoSize = true;
            this.row1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.row1.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.row1.Location = new System.Drawing.Point(8, 6);
            this.row1.WrapContents = false;
            this.row1.Controls.Add(this.lblIp);
            this.row1.Controls.Add(this.txtIp);
            this.row1.Controls.Add(this.lblPort);
            this.row1.Controls.Add(this.txtPort);
            this.row1.Controls.Add(this.lblVersion);
            this.row1.Controls.Add(this.cmbVersion);
            this.row1.Controls.Add(this.lblCount);
            this.row1.Controls.Add(this.txtCount);
            this.row1.Controls.Add(this.lblConcurrency);
            this.row1.Controls.Add(this.txtConcurrency);

            this.lblIp.AutoSize = true;
            this.lblIp.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.lblIp.Margin = new System.Windows.Forms.Padding(0, 9, 4, 6);
            this.lblIp.Text = "服务器:";

            this.txtIp.BackColor = System.Drawing.Color.FromArgb(26, 26, 26);
            this.txtIp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtIp.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.txtIp.Margin = new System.Windows.Forms.Padding(0, 6, 16, 6);
            this.txtIp.Width = 140;
            this.txtIp.Text = "127.0.0.1";

            this.lblPort.AutoSize = true;
            this.lblPort.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.lblPort.Margin = new System.Windows.Forms.Padding(0, 9, 4, 6);
            this.lblPort.Text = "端口:";

            this.txtPort.BackColor = System.Drawing.Color.FromArgb(26, 26, 26);
            this.txtPort.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPort.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.txtPort.Margin = new System.Windows.Forms.Padding(0, 6, 16, 6);
            this.txtPort.Width = 70;
            this.txtPort.Text = "25565";

            this.lblVersion.AutoSize = true;
            this.lblVersion.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.lblVersion.Margin = new System.Windows.Forms.Padding(0, 9, 4, 6);
            this.lblVersion.Text = "版本:";

            this.cmbVersion.BackColor = System.Drawing.Color.FromArgb(26, 26, 26);
            this.cmbVersion.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.cmbVersion.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbVersion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbVersion.Margin = new System.Windows.Forms.Padding(0, 6, 16, 6);
            this.cmbVersion.Width = 100;

            this.lblCount.AutoSize = true;
            this.lblCount.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.lblCount.Margin = new System.Windows.Forms.Padding(0, 9, 4, 6);
            this.lblCount.Text = "尝试次数:";

            this.txtCount.BackColor = System.Drawing.Color.FromArgb(26, 26, 26);
            this.txtCount.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtCount.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.txtCount.Margin = new System.Windows.Forms.Padding(0, 6, 16, 6);
            this.txtCount.Width = 70;
            this.txtCount.Text = "100";

            this.lblConcurrency.AutoSize = true;
            this.lblConcurrency.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.lblConcurrency.Margin = new System.Windows.Forms.Padding(0, 9, 4, 6);
            this.lblConcurrency.Text = "并发数:";

            this.txtConcurrency.BackColor = System.Drawing.Color.FromArgb(26, 26, 26);
            this.txtConcurrency.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtConcurrency.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.txtConcurrency.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.txtConcurrency.Width = 60;
            this.txtConcurrency.Text = "1";

            // ==================== row2 ====================
            this.row2.AutoSize = true;
            this.row2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.row2.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.row2.Location = new System.Drawing.Point(8, 52);
            this.row2.WrapContents = false;
            this.row2.Controls.Add(this.btnStart);
            this.row2.Controls.Add(this.btnPause);
            this.row2.Controls.Add(this.btnResume);
            this.row2.Controls.Add(this.btnStop);
            this.row2.Controls.Add(this.btnSettings);
            this.row2.Controls.Add(this.btnTools);
            this.row2.Controls.Add(this.btnReports);
            this.row2.Controls.Add(this.btnPatches);
            this.row2.Controls.Add(this.btnPlugins);
            this.row2.Controls.Add(this.btnHelp);

            this.btnStart.BackColor = System.Drawing.Color.FromArgb(14, 99, 156);
            this.btnStart.FlatAppearance.BorderSize = 0;
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.ForeColor = System.Drawing.Color.White;
            this.btnStart.AutoSize = true;
            this.btnStart.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnStart.Padding = new System.Windows.Forms.Padding(16, 5, 16, 5);
            this.btnStart.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnStart.Text = "▶ 开始";
            this.btnStart.UseVisualStyleBackColor = false;

            this.btnPause.BackColor = System.Drawing.Color.FromArgb(138, 109, 26);
            this.btnPause.Enabled = false;
            this.btnPause.FlatAppearance.BorderSize = 0;
            this.btnPause.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPause.ForeColor = System.Drawing.Color.White;
            this.btnPause.AutoSize = true;
            this.btnPause.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnPause.Padding = new System.Windows.Forms.Padding(16, 5, 16, 5);
            this.btnPause.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnPause.Text = "⏸ 暂停";
            this.btnPause.UseVisualStyleBackColor = false;

            this.btnResume.BackColor = System.Drawing.Color.FromArgb(14, 99, 156);
            this.btnResume.Enabled = false;
            this.btnResume.FlatAppearance.BorderSize = 0;
            this.btnResume.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnResume.ForeColor = System.Drawing.Color.White;
            this.btnResume.AutoSize = true;
            this.btnResume.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnResume.Padding = new System.Windows.Forms.Padding(16, 5, 16, 5);
            this.btnResume.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnResume.Text = "⏵ 恢复";
            this.btnResume.UseVisualStyleBackColor = false;

            this.btnStop.BackColor = System.Drawing.Color.FromArgb(161, 38, 13);
            this.btnStop.FlatAppearance.BorderSize = 0;
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.ForeColor = System.Drawing.Color.White;
            this.btnStop.AutoSize = true;
            this.btnStop.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnStop.Padding = new System.Windows.Forms.Padding(16, 5, 16, 5);
            this.btnStop.Margin = new System.Windows.Forms.Padding(0, 0, 24, 0);
            this.btnStop.Text = "⏹ 停止";
            this.btnStop.UseVisualStyleBackColor = false;

            this.btnSettings.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnSettings.FlatAppearance.BorderSize = 0;
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettings.ForeColor = System.Drawing.Color.White;
            this.btnSettings.AutoSize = true;
            this.btnSettings.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnSettings.Padding = new System.Windows.Forms.Padding(16, 5, 16, 5);
            this.btnSettings.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnSettings.Text = "⚙ 设置";
            this.btnSettings.UseVisualStyleBackColor = false;

            this.btnTools.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnTools.FlatAppearance.BorderSize = 0;
            this.btnTools.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTools.ForeColor = System.Drawing.Color.White;
            this.btnTools.AutoSize = true;
            this.btnTools.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnTools.Padding = new System.Windows.Forms.Padding(16, 5, 16, 5);
            this.btnTools.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnTools.Text = "🔍 工具";
            this.btnTools.UseVisualStyleBackColor = false;

            this.btnReports.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnReports.FlatAppearance.BorderSize = 0;
            this.btnReports.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReports.ForeColor = System.Drawing.Color.White;
            this.btnReports.AutoSize = true;
            this.btnReports.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnReports.Padding = new System.Windows.Forms.Padding(16, 5, 16, 5);
            this.btnReports.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnReports.Text = "📊 报告";
            this.btnReports.UseVisualStyleBackColor = false;

            // ★ 补丁按钮
            this.btnPatches.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnPatches.FlatAppearance.BorderSize = 0;
            this.btnPatches.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPatches.ForeColor = System.Drawing.Color.White;
            this.btnPatches.AutoSize = true;
            this.btnPatches.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnPatches.Padding = new System.Windows.Forms.Padding(16, 5, 16, 5);
            this.btnPatches.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnPatches.Text = "🧩 补丁";
            this.btnPatches.UseVisualStyleBackColor = false;

            this.btnPlugins.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnPlugins.FlatAppearance.BorderSize = 0;
            this.btnPlugins.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPlugins.ForeColor = System.Drawing.Color.White;
            this.btnPlugins.AutoSize = true;
            this.btnPlugins.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnPlugins.Padding = new System.Windows.Forms.Padding(16, 5, 16, 5);
            this.btnPlugins.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnPlugins.Text = "🔌 插件管理";
            this.btnPlugins.UseVisualStyleBackColor = false;

            this.btnHelp.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnHelp.FlatAppearance.BorderSize = 0;
            this.btnHelp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHelp.ForeColor = System.Drawing.Color.White;
            this.btnHelp.AutoSize = true;
            this.btnHelp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnHelp.Padding = new System.Windows.Forms.Padding(16, 5, 16, 5);
            this.btnHelp.Margin = new System.Windows.Forms.Padding(0);
            this.btnHelp.Text = "❓ 帮助";
            this.btnHelp.UseVisualStyleBackColor = false;

            // ==================== 隐藏字段 ====================
            this.txtRetryDelay.Text = "5000";
            this.txtConnInterval.Text = "3000";
            this.txtTimeout.Text = "5";
            this.txtPrefix.Text = "";
            this.txtAutoDisconnect.Text = "10";
            this.chkStay.Checked = false;

            // ==================== splitContainer ====================
            this.splitContainer.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.splitContainer.SplitterWidth = 2;

            this.rtbLog.BackColor = System.Drawing.Color.FromArgb(26, 26, 26);
            this.rtbLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbLog.Font = new System.Drawing.Font("Microsoft YaHei UI", 9.5F);
            this.rtbLog.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.rtbLog.ReadOnly = true;
            this.rtbLog.WordWrap = true;
            this.splitContainer.Panel1.Controls.Add(this.rtbLog);

            this.lstPlugins.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.lstPlugins.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstPlugins.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstPlugins.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lstPlugins.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.lstPlugins.IntegralHeight = false;
            this.splitContainer.Panel2.Controls.Add(this.lstPlugins);

            // ==================== cmdPanel ====================
            this.cmdPanel.BackColor = System.Drawing.Color.FromArgb(37, 37, 38);
            this.cmdPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.cmdPanel.Height = 48;
            this.cmdPanel.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.cmdPanel.Controls.Add(this.cmdTable);

            this.cmdTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmdTable.ColumnCount = 4;
            this.cmdTable.RowCount = 1;
            this.cmdTable.BackColor = System.Drawing.Color.Transparent;
            this.cmdTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.cmdTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cmdTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.cmdTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.cmdTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

            this.lblPrompt.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblPrompt.ForeColor = System.Drawing.Color.FromArgb(78, 201, 176);
            this.lblPrompt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPrompt.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblPrompt.Text = ">";
            this.lblPrompt.Margin = new System.Windows.Forms.Padding(0);

            this.txtCommand.BackColor = System.Drawing.Color.FromArgb(26, 26, 26);
            this.txtCommand.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtCommand.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.txtCommand.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.txtCommand.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtCommand.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);

            this.btnHelpInline.BackColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnHelpInline.FlatAppearance.BorderSize = 0;
            this.btnHelpInline.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHelpInline.ForeColor = System.Drawing.Color.White;
            this.btnHelpInline.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnHelpInline.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnHelpInline.Text = "📖 命令示例";
            this.btnHelpInline.UseVisualStyleBackColor = false;

            this.btnSend.BackColor = System.Drawing.Color.FromArgb(14, 99, 156);
            this.btnSend.FlatAppearance.BorderSize = 0;
            this.btnSend.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSend.ForeColor = System.Drawing.Color.White;
            this.btnSend.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSend.Margin = new System.Windows.Forms.Padding(0);
            this.btnSend.Text = "发送";
            this.btnSend.UseVisualStyleBackColor = false;

            this.cmdTable.Controls.Add(this.lblPrompt, 0, 0);
            this.cmdTable.Controls.Add(this.txtCommand, 1, 0);
            this.cmdTable.Controls.Add(this.btnHelpInline, 2, 0);
            this.cmdTable.Controls.Add(this.btnSend, 3, 0);

            // ==================== statusStrip ====================
            this.statusStrip.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.statusStrip.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.lblStatus, this.lblUpdate });              // ★ 加 lblUpdate
            this.statusStrip.SizingGrip = false;

            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.lblStatus.Text = "状态: 启动中...";
            this.lblStatus.Spring = true;                            // ★ 占满左侧，把 lblUpdate 挤到右边

            // ★ lblUpdate：右侧徽标，默认空
            this.lblUpdate.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;   // 靠右
            this.lblUpdate.ForeColor = System.Drawing.Color.FromArgb(120, 220, 120);
            this.lblUpdate.IsLink = false;
            this.lblUpdate.Text = "";

            // ==================== MainForm ====================
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.ClientSize = new System.Drawing.Size(1280, 800);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.cmdPanel);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.toolbarPanel);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.MinimumSize = new System.Drawing.Size(1000, 640);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Minecraft 批量登录客户端 " + JBSS261A.VersionInfo.CURRENT_VERSION;   // ★ 动态版本

            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.toolbarPanel.ResumeLayout(false);
            this.row1.ResumeLayout(false);
            this.row1.PerformLayout();
            this.row2.ResumeLayout(false);
            this.row2.PerformLayout();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            this.splitContainer.ResumeLayout(false);
            this.cmdPanel.ResumeLayout(false);
            this.cmdTable.ResumeLayout(false);
            this.cmdTable.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}