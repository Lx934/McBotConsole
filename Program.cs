using System;
using System.Windows.Forms;

namespace JBSS261A
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // ★ 初始化开发者模式标志
            try
            {
                JBSS261A.Patching.DevModeFlag.Initialize();
            }
            catch { }

            Application.Run(new MainForm());
        }
    }
}