using System;
using System.Windows.Forms;

namespace JBSS261A
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // 注意：已移除单实例锁，允许用户同时打开多个程序实例
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}