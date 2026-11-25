using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Wpf.Ui.Controls;

namespace EasyButtonFix.Core
{
    /// <summary>
    /// Globaler Low-Level-Maus-Hook in einem eigenen Thread.
    /// Ein Druck wird verschluckt, wenn er kürzer als ThresholdMs nach dem letzten
    /// Loslassen derselben Taste kommt (typisches "Prellen" eines verschlissenen Schalters).
    /// </summary>
    public sealed class MouseFilter : IDisposable
    {
        public readonly ButtonState Back    = new ButtonState("Back",    SymbolRegular.ArrowLeft24,   true,  100);
        public readonly ButtonState Forward = new ButtonState("Forward", SymbolRegular.ArrowRight24,  true,  100);
        public readonly ButtonState Left    = new ButtonState("Left",    SymbolRegular.CursorClick24, false, 60);
        public readonly ButtonState Right   = new ButtonState("Right",   SymbolRegular.CursorHover24, false, 60);
        public readonly ButtonState Middle  = new ButtonState("Middle",  SymbolRegular.ArrowMove24,   false, 60);

        public ButtonState[] All { get; }

        public MouseFilter() { All = new[] { Back, Forward, Left, Right, Middle }; }

        /// <summary>Globaler Schalter (Hauptschalter im Fenster / Tray).</summary>
        public volatile bool Active = true;

        /// <summary>Während der Kalibrierung wird diese Taste nur gemessen, nicht gefiltert.</summary>
        public volatile ButtonState Calibrating;

        /// <summary>
        /// Jeder physische Druck: (Taste, blockiert?, Abstand zum letzten Loslassen in ms oder -1).
        /// Wird im Hook-Thread ausgelöst – Handler müssen schnell sein und selbst ins UI marshallen.
        /// </summary>
        public event Action<ButtonState, bool, int> Pressed;

        Thread _thread;
        uint _threadId;
        IntPtr _hook;
        HookProc _proc; // Referenz halten, sonst sammelt der GC den Callback ein

        public void Start()
        {
            Exception error = null;
            using (var ready = new ManualResetEventSlim())
            {
                _thread = new Thread(() =>
                {
                    _threadId = GetCurrentThreadId();
                    PeekMessage(out _, IntPtr.Zero, 0, 0, 0); // Message-Queue anlegen
                    _proc = Callback;
                    _hook = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(null), 0);
                    if (_hook == IntPtr.Zero) error = new Win32Exception(Marshal.GetLastWin32Error());
                    ready.Set();
                    if (_hook == IntPtr.Zero) return;

                    while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0) { }
                    UnhookWindowsHookEx(_hook);
                    _hook = IntPtr.Zero;
                })
                {
                    IsBackground = true,
                    Name = "MouseHook",
                    Priority = ThreadPriority.AboveNormal
                };
                _thread.Start();
                ready.Wait();
            }
            if (error != null) throw new InvalidOperationException(L.HookFailed + error.Message, error);
        }

        IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var info = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                // Von Software erzeugte Klicks (Makros, Remote-Desktop, ...) nie anfassen
                if ((info.flags & LLMHF_INJECTED) == 0)
                {
                    var b = Identify(wParam.ToInt32(), info.mouseData, out bool isDown);
                    if (b != null && Handle(b, isDown, info.time))
                        return (IntPtr)1; // Ereignis verschlucken
                }
            }
            return CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        ButtonState Identify(int msg, uint mouseData, out bool isDown)
        {
            isDown = msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN || msg == WM_MBUTTONDOWN || msg == WM_XBUTTONDOWN;
            switch (msg)
            {
                case WM_LBUTTONDOWN: case WM_LBUTTONUP: return Left;
                case WM_RBUTTONDOWN: case WM_RBUTTONUP: return Right;
                case WM_MBUTTONDOWN: case WM_MBUTTONUP: return Middle;
                case WM_XBUTTONDOWN: case WM_XBUTTONUP:
                    int x = (int)(mouseData >> 16);
                    return x == 1 ? Back : x == 2 ? Forward : null;
            }
            return null;
        }

        /// <returns>true = Ereignis blockieren</returns>
        bool Handle(ButtonState b, bool isDown, uint time)
        {
            if (isDown)
            {
                int gap = b.hasUp ? unchecked((int)(time - b.lastUpTime)) : -1;
                bool block = Active && b.Enabled && Calibrating != b && gap >= 0 && gap < b.ThresholdMs;
                if (block)
                {
                    b.suppressNextUp = true;
                    Interlocked.Increment(ref b.blocked);
                }
                Pressed?.Invoke(b, block, gap);
                return block;
            }

            if (b.suppressNextUp)
            {
                // Das zum geblockten Drücken gehörende Loslassen auch verschlucken.
                // lastUpTime bleibt beim echten Loslassen -> Sperrzeit verlängert sich nicht endlos.
                b.suppressNextUp = false;
                return true;
            }
            b.lastUpTime = time;
            b.hasUp = true;
            return false;
        }

        public void Dispose()
        {
            if (_thread == null) return;
            PostThreadMessage(_threadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            _thread.Join(1000);
            _thread = null;
        }

        // ---------------- Win32 ----------------
        const int WH_MOUSE_LL = 14, WM_QUIT = 0x0012;
        const int WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202;
        const int WM_RBUTTONDOWN = 0x0204, WM_RBUTTONUP = 0x0205;
        const int WM_MBUTTONDOWN = 0x0207, WM_MBUTTONUP = 0x0208;
        const int WM_XBUTTONDOWN = 0x020B, WM_XBUTTONUP = 0x020C;
        const uint LLMHF_INJECTED = 0x01;

        [StructLayout(LayoutKind.Sequential)]
        struct MSLLHOOKSTRUCT { public int x, y; public uint mouseData, flags, time; public IntPtr dwExtraInfo; }

        [StructLayout(LayoutKind.Sequential)]
        struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam, lParam; public uint time; public int ptX, ptY; }

        delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)] static extern IntPtr SetWindowsHookEx(int idHook, HookProc fn, IntPtr hMod, uint threadId);
        [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] static extern int GetMessage(out MSG msg, IntPtr hWnd, uint min, uint max);
        [DllImport("user32.dll")] static extern bool PeekMessage(out MSG msg, IntPtr hWnd, uint min, uint max, uint remove);
        [DllImport("user32.dll")] static extern bool PostThreadMessage(uint threadId, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandle(string name);
    }
}
