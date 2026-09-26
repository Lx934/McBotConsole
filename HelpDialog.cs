using System;
using System.Drawing;
using System.Windows.Forms;

namespace JBSS261A
{
    /// <summary>
    /// 中文帮助对话框：告诉用户能说什么、能点什么
    /// </summary>
    public class HelpDialog : Form
    {
        public HelpDialog()
        {
            this.Text = "❓ 使用帮助";
            this.Size = new Size(720, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Microsoft YaHei UI", 9.5F);
            this.BackColor = Color.FromArgb(250, 250, 250);
            this.MinimizeBox = false;

            var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(16, 6) };
            tabs.TabPages.Add(BuildQuickStartTab());
            tabs.TabPages.Add(BuildChineseCommandsTab());
            tabs.TabPages.Add(BuildConfigTab());
            tabs.TabPages.Add(BuildPluginsTab());
            tabs.TabPages.Add(BuildAdvancedTab());

            this.Controls.Add(tabs);

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            var btnClose = new Button
            {
                Text = "关闭",
                Size = new Size(100, 32),
                Location = new Point(580, 9),
                FlatStyle = FlatStyle.System
            };
            btnClose.Click += (s, e) => this.Close();
            bottom.Controls.Add(btnClose);
            this.Controls.Add(bottom);
        }

        private TabPage BuildQuickStartTab()
        {
            var page = new TabPage("🚀 快速上手") { BackColor = Color.White };
            var tb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Microsoft YaHei UI", 10F),
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            tb.Text =
                "【第一步：填服务器信息】\n" +
                "   在窗口顶部输入框里填上服务器 IP、端口、版本，\n" +
                "   或者点【⚙ 设置】按钮，在对话框里填（推荐）。\n" +
                "\n" +
                "【第二步：设置尝试次数】\n" +
                "   顶部【次数】填你想登录多少个假人。\n" +
                "   默认 100，建议先填 1 试一下。\n" +
                "\n" +
                "【第三步：点开始】\n" +
                "   点【▶ 开始】按钮。程序会开始连接服务器。\n" +
                "   成功和失败的数量会显示在底部状态栏。\n" +
                "\n" +
                "【想停下来？】\n" +
                "   【⏸ 暂停】临时停下，之后可以【⏵ 恢复】\n" +
                "   【⏹ 停止】彻底结束，断开所有连接\n" +
                "\n" +
                "【看不懂命令？】\n" +
                "   底部输入框可以直接输入中文，例如：\n" +
                "       改IP 192.168.1.100\n" +
                "       设置次数 50\n" +
                "       看状态\n" +
                "       插件列表\n" +
                "   点【📖 中文命令】看所有支持的写法。\n";
            page.Controls.Add(tb);
            return page;
        }

        private TabPage BuildChineseCommandsTab()
        {
            var page = new TabPage("📖 中文命令") { BackColor = Color.White };

            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Microsoft YaHei UI", 9.5F),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            dgv.Columns.Add("A", "你可以说");
            dgv.Columns.Add("B", "作用");
            dgv.Columns[0].FillWeight = 35;
            dgv.Columns[1].FillWeight = 65;

            void Add(string a, string b) => dgv.Rows.Add(a, b);

            Add("帮助 / 怎么用", "显示所有可用命令");
            Add("看状态", "显示当前运行状态");
            Add("暂停", "暂停运行（不退出）");
            Add("继续 / 恢复", "恢复运行");
            Add("退出 / 关闭", "退出程序");
            Add("清空 / 清屏", "清空日志窗口");

            Add("查看配置", "显示当前所有配置项");
            Add("改IP 192.168.1.1", "修改服务器地址");
            Add("改端口 25565", "修改端口");
            Add("设置版本 1.20.1", "修改游戏版本");
            Add("设置次数 50", "修改尝试次数");
            Add("设置并发 5", "修改并发数");
            Add("设置前缀 MyBot", "修改玩家名前缀");
            Add("保存配置", "把当前设置写入文件");

            Add("插件列表", "显示所有已加载插件");
            Add("重载 插件名", "重新加载某个插件");
            Add("重载所有插件", "重新加载全部插件");
            Add("卸载 插件名", "卸载某个插件");
            Add("权限 插件名 2", "设置插件权限等级（0-3）");

            Add("打包日志", "把所有日志打包成一个 zip");

            page.Controls.Add(dgv);
            return page;
        }

        private TabPage BuildConfigTab()
        {
            var page = new TabPage("⚙ 配置说明") { BackColor = Color.White };
            var tb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Microsoft YaHei UI", 10F),
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            tb.Text =
                "【服务器地址】\n" +
                "   要连接的服务器的 IP 或域名。内网测试一般填 127.0.0.1 或局域网 IP。\n" +
                "\n" +
                "【端口】\n" +
                "   Minecraft 服务器默认端口是 25565。\n" +
                "\n" +
                "【游戏版本】\n" +
                "   要和服务器版本一致，否则会被踢。例如 1.19.2、1.20.1。\n" +
                "\n" +
                "【尝试次数】\n" +
                "   一共登录多少个假人。\n" +
                "\n" +
                "【并发数】\n" +
                "   同时连接多少个。数字越大越快，但容易触发服务器限流。\n" +
                "   推荐从 1 开始试，稳定后再往上调。\n" +
                "\n" +
                "【连接间隔】\n" +
                "   每次开始连接之间等待多少毫秒。3000 = 3 秒。\n" +
                "\n" +
                "【连接超时】\n" +
                "   连接超过几秒还没登录成功，就判定失败。默认 5 秒。\n" +
                "\n" +
                "【重试延迟】\n" +
                "   失败后等多久再重试。默认 5000 = 5 秒。\n" +
                "\n" +
                "【名字前缀】\n" +
                "   假人名字的开头。留空则完全随机。例如 MyBot 会生成 MyBot_a3f2 之类。\n" +
                "\n" +
                "【保持连接】\n" +
                "   勾上：登录成功后不退出，挂在那里。\n" +
                "   不勾：登录成功立刻断开（用于测试登录成功率）。\n" +
                "\n" +
                "【自动断开】\n" +
                "   勾了保持连接后，多少秒后自动断开。0 表示永不。\n";
            page.Controls.Add(tb);
            return page;
        }

        private TabPage BuildPluginsTab()
        {
            var page = new TabPage("🔌 插件") { BackColor = Color.White };
            var tb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Microsoft YaHei UI", 10F),
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            tb.Text =
                "【插件在哪？】\n" +
                "   Node 插件：backend\\plugins\\\n" +
                "   Python 插件：backend\\plugins_py\\\n" +
                "\n" +
                "【怎么加插件？】\n" +
                "   在对应目录下建一个文件夹，里面放插件文件。\n" +
                "   程序启动时会自动加载。\n" +
                "\n" +
                "【权限等级】\n" +
                "   0 = 只能读信息、写日志\n" +
                "   1 = 可以发聊天、查背包\n" +
                "   2 = 可以启停 Bot、改全局配置\n" +
                "   3 = 可以执行系统命令（危险）\n" +
                "\n" +
                "【查看已加载的插件】\n" +
                "   输入：插件列表\n" +
                "   或者点【插件】菜单。\n" +
                "\n" +
                "【重载某个插件】\n" +
                "   输入：重载 插件名\n" +
                "\n" +
                "【卸载某个插件】\n" +
                "   输入：卸载 插件名\n";
            page.Controls.Add(tb);
            return page;
        }

        private TabPage BuildAdvancedTab()
        {
            var page = new TabPage("🔧 高级") { BackColor = Color.White };
            var tb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Microsoft YaHei UI", 10F),
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            tb.Text =
                "【原版英文命令也支持】\n" +
                "   如果你习惯用命令，底部输入框同样接受原始命令，例如：\n" +
                "       config ip 127.0.0.1\n" +
                "       config count 50\n" +
                "       reload MyPlugin\n" +
                "       status\n" +
                "\n" +
                "【配置文件在哪？】\n" +
                "   backend\\config.json\n" +
                "   用记事本就能打开。改完重启程序生效。\n" +
                "\n" +
                "【日志文件在哪？】\n" +
                "   backend\\logs\\bot.log\n" +
                "\n" +
                "【成功记录在哪？】\n" +
                "   backend\\success_log.json\n" +
                "\n" +
                "【日志太大怎么办？】\n" +
                "   超过 1MB 会自动压缩成 .gz 归档。\n" +
                "   输入【打包日志】可以把全部日志打成一个 zip。\n" +
                "\n" +
                "【程序出错怎么排查？】\n" +
                "   1. 看窗口里的红色 ERROR 行\n" +
                "   2. 打开 backend\\logs\\bot.log 查看\n" +
                "   3. 输入【打包日志】，把 zip 发给开发者\n";
            page.Controls.Add(tb);
            return page;
        }
    }
}