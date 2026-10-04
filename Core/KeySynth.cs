using System;
using System.Runtime.InteropServices;

namespace KeyboardChatterBlocker
{
    /// <summary>
    /// 合成键盘事件（长按救援用）。
    /// <para>
    /// 刻意使用**扫描码**而不是虚拟键码：DirectInput / Raw Input 类游戏读的是扫描码，
    /// 只带 vkCode 的合成事件在那些游戏里会被忽略。扫描码与扩展位都取自真实的按下的那个事件，
    /// 因此合成出来的事件与真实硬件事件等价。
    /// </para>
    /// </summary>
    public static class KeySynth
    {
        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        /// <summary>
        /// 三个成员共用一个 union。必须显式布局，且 union 的大小由最大的 MOUSEINPUT 决定 ——
        /// 只放 KEYBDINPUT 会让整个 INPUT 变短，SendInput 会以 ERROR_INVALID_PARAMETER 失败。
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        /// <summary>合成一次「按下」。成功返回 true。</summary>
        public static bool KeyDown(uint scanCode, bool extended)
        {
            return Send(scanCode, extended, false);
        }

        /// <summary>合成一次「抬起」。成功返回 true。</summary>
        public static bool KeyUp(uint scanCode, bool extended)
        {
            return Send(scanCode, extended, true);
        }

        private static bool Send(uint scanCode, bool extended, bool up)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wScan = (ushort)scanCode;
            inputs[0].u.ki.dwFlags = KEYEVENTF_SCANCODE
                | (extended ? KEYEVENTF_EXTENDEDKEY : 0)
                | (up ? KEYEVENTF_KEYUP : 0);
            uint sent = SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
            return sent == 1;
        }

        /// <summary>INPUT 结构体的实际大小，供自检与排查用。</summary>
        public static int InputStructSize => Marshal.SizeOf(typeof(INPUT));
    }
}
