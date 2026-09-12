using System;
using System.Drawing;
using System.Windows.Forms;

namespace JBSS261A
{
    /// <summary>
    /// 给 .NET Framework 的 TextBox 模拟占位提示（模拟 PlaceholderText）
    /// </summary>
    public static class PlaceholderHelper
    {
        public static void SetPlaceholder(TextBox box, string placeholder)
        {
            if (box == null) return;

            Color originalColor = box.ForeColor;

            // 初始状态显示占位文本
            box.Text = placeholder;
            box.ForeColor = Color.Gray;
            box.Tag = new PlaceholderState
            {
                IsPlaceholder = true,
                Placeholder = placeholder,
                NormalColor = originalColor
            };

            box.Enter += (s, e) =>
            {
                var st = box.Tag as PlaceholderState;
                if (st != null && st.IsPlaceholder)
                {
                    box.Text = "";
                    box.ForeColor = st.NormalColor;
                    st.IsPlaceholder = false;
                }
            };

            box.Leave += (s, e) =>
            {
                var st = box.Tag as PlaceholderState;
                if (st != null && string.IsNullOrEmpty(box.Text))
                {
                    box.Text = st.Placeholder;
                    box.ForeColor = Color.Gray;
                    st.IsPlaceholder = true;
                }
            };
        }

        /// <summary>
        /// 读用户实际输入（忽略占位文本）
        /// </summary>
        public static string GetRealText(TextBox box)
        {
            if (box == null) return "";
            var st = box.Tag as PlaceholderState;
            if (st != null && st.IsPlaceholder) return "";
            return box.Text;
        }

        /// <summary>
        /// 清空输入框并恢复占位文本
        /// </summary>
        public static void ClearToPlaceholder(TextBox box)
        {
            if (box == null) return;
            var st = box.Tag as PlaceholderState;
            if (st != null)
            {
                box.Text = st.Placeholder;
                box.ForeColor = Color.Gray;
                st.IsPlaceholder = true;
            }
            else
            {
                box.Clear();
            }
        }

        private class PlaceholderState
        {
            public bool IsPlaceholder;
            public string Placeholder;
            public Color NormalColor;
        }
    }
}