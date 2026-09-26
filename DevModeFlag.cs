using System;
using System.Runtime.InteropServices;

namespace JBSS261A.Patching
{
    public static class DevModeFlag
    {
        // 未激活 / 激活 标记（CE 用）
        private const int DEV_OFF = 20130923;   // 未激活
        private const int DEV_ON = 20130924;    // 激活（CE 改这里）
        private const int DEV_CHECK = 0x55AA55AA; // 校验位

        private static IntPtr _p = IntPtr.Zero;

        public static void Initialize()
        {
            if (_p != IntPtr.Zero) return;
            _p = Marshal.AllocHGlobal(8);
            Marshal.WriteInt32(_p, 0, DEV_OFF);
            Marshal.WriteInt32(_p, 4, DEV_CHECK);
        }

        public static bool IsActive()
        {
            if (_p == IntPtr.Zero) return false;
            if (Marshal.ReadInt32(_p, 4) != DEV_CHECK) return false;
            return Marshal.ReadInt32(_p, 0) == DEV_ON;
        }

        // 供日志查看当前值
        public static int CurrentValue()
        {
            if (_p == IntPtr.Zero) return -1;
            return Marshal.ReadInt32(_p, 0);
        }
    }
}