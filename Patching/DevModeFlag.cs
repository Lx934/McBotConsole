using System;
using System.Runtime.InteropServices;

namespace JBSS261A.Patching
{
    /// <summary>
    /// 开发者模式内存标志
    ///
    /// 用 Cheat Engine 修改此内存区域来激活开发者模式：
    ///   1. CE 附加到 JBSS261A.exe
    ///   2. 搜索 4 字节值：11111111（十六进制）
    ///   3. 找到后改成：22222222
    ///
    /// 激活后：补丁加载时跳过 GitHub 官方验证
    /// 重启程序自动失效
    /// </summary>
    public static class DevModeFlag
    {
        // ============ 特征值（CE 搜索用） ============
        // 十六进制  十进制               说明
        // 11111111  286331153   未激活
        // 22222222  572662306   已激活
        private const int MAGIC_INACTIVE = 0x11111111;
        private const int MAGIC_ACTIVE   = 0x22222222;

        // 校验位，防止误改
        private const int GUARD_VALUE    = 0x55AA55AA;

        private static IntPtr _ptr = IntPtr.Zero;
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// 初始化内存区域。程序启动时调用一次。
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized) return;

                try
                {
                    // 分配 8 字节非托管内存（两个 int）
                    // 布局：[0..3] = MAGIC     [4..7] = GUARD
                    _ptr = Marshal.AllocHGlobal(8);
                    Marshal.WriteInt32(_ptr, MAGIC_INACTIVE);
                    Marshal.WriteInt32(IntPtr.Add(_ptr, 4), GUARD_VALUE);
                    _initialized = true;

                    // 保持内存不被释放（进程退出时由系统自动回收）
                    // 用 GCHandle 钉住也行，但 AllocHGlobal 已经稳定
                }
                catch
                {
                    _initialized = false;
                }
            }
        }

        /// <summary>
        /// 检查开发者模式是否激活
        /// </summary>
        public static bool IsActive()
        {
            if (!_initialized) return false;

            try
            {
                int magic = Marshal.ReadInt32(_ptr);
                int guard = Marshal.ReadInt32(IntPtr.Add(_ptr, 4));

                // 校验位必须正确（防止 CE 搜到的其他地址被误改）
                if (guard != GUARD_VALUE)
                    return false;

                return magic == MAGIC_ACTIVE;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 返回当前内存地址（仅调试用，可以打日志看地址）
        /// </summary>
        public static long GetAddress()
        {
            return _ptr.ToInt64();
        }
    }
}