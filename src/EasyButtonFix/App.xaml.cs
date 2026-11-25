using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using EasyButtonFix.Core;
using Forms = System.Windows.Forms;

namespace EasyButtonFix
{
    public partial class App : Application
    {
        public static MouseFilter Filter { get; private set; }
        public static App Instance => (App)Current;

        /// <summary>Hauptschalter hat sich geändert (Fenster oder Tray).</summary>
        public event Action ActiveChanged;

        Mutex _mutex;
        EventWaitHandle _showSignal;
        RegisteredWaitHandle _showWait, _exitWait;
        EventWaitHandle _exitSignal;
        Forms.NotifyIcon _tray;
        Forms.ToolStripMenuItem _pauseItem;
        Icon _iconActive, _iconPaused;
        DispatcherTimer _saveTimer;
        volatile MainWindow _window;

        const string MutexName = "EasyButtonFix_SingleInstance";
        const string ExitEventName = "EasyButtonFix_Exit";

        protected override void OnStartup(StartupEventArgs e)
        {
            // "--exit": laufende Instanz beenden (benutzt vom Installer/Deinstaller)
            if (e.Args.Contains("--exit"))
            {
                if (EventWaitHandle.TryOpenExisting(ExitEventName, out var exitEvent))
                {
                    exitEvent.Set();
                    if (Mutex.TryOpenExisting(MutexName, out var m))
                    {
                        try { m.WaitOne(5000); } catch (AbandonedMutexException) { }
                    }
                }
                Shutdown();
                return;
            }

            // Nur eine Instanz: ein zweiter Start holt einfach das Fenster der ersten nach vorne
            _mutex = new Mutex(true, MutexName, out bool first);
            _showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, "EasyButtonFix_Show");
            if (!first)
            {
                _showSignal.Set();
                Shutdown();
                return;
            }

            base.OnStartup(e);

            Filter = new MouseFilter();
            Settings.Load(Filter);
            L.Init(Settings.Language);
            try { Filter.Start(); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "EasyButtonFix", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }
            Filter.Pressed += OnPressedHookThread;

            _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            _saveTimer.Tick += (s, a) => { _saveTimer.Stop(); Settings.Save(Filter); };
            foreach (var b in Filter.All)
                b.PropertyChanged += (s, a) => { if (a.PropertyName != nameof(ButtonState.Subtitle)) RequestSave(); };

            _showWait = ThreadPool.RegisterWaitForSingleObject(_showSignal,
                (s, t) => Dispatcher.BeginInvoke(new Action(ShowMainWindow)), null, -1, false);
            _exitSignal = new EventWaitHandle(false, EventResetMode.AutoReset, ExitEventName);
            _exitWait = ThreadPool.RegisterWaitForSingleObject(_exitSignal,
                (s, t) => Dispatcher.BeginInvoke(new Action(ExitApp)), null, -1, true);

            CreateTray();

            if (e.Args.Contains("--tray")) TrimMemory();
            else ShowMainWindow();
        }

        // ---------------- Klick-Ereignisse ----------------

        void OnPressedHookThread(ButtonState b, bool blocked, int gap)
        {
            // Läuft im Hook-Thread: nichts Schweres hier, alles ans UI weiterreichen.
            if (blocked) Dispatcher.BeginInvoke(new Action(() => { b.RefreshStats(); UpdateTray(); }));
            var w = _window;
            if (w != null) Dispatcher.BeginInvoke(new Action(() => w.OnPressed(b, blocked, gap)));
        }

        public int TotalBlocked => Filter.All.Sum(b => b.BlockedCount);

        public void SetActive(bool active)
        {
            if (Filter.Active == active) return;
            Filter.Active = active;
            UpdateTray();
            RequestSave();
            ActiveChanged?.Invoke();
        }

        /// <summary>Sprache wechseln: speichern, Tray-Texte neu setzen, offenes Fenster neu aufbauen.</summary>
        public void SetLanguage(string language)
        {
            if (Settings.Language == language) return;
            Settings.Language = language;
            L.Init(language);
            Settings.Save(Filter);
            UpdateTrayTexts();
            var w = _window;
            if (w == null) return;
            _restoreBounds = new Rect(w.Left, w.Top, w.Width, w.Height);
            _reopening = true;
            w.Close();
            _reopening = false;
            ShowMainWindow();
        }

        bool _reopening;
        Rect? _restoreBounds;

        public void RequestSave()
        {
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        // ---------------- Fenster ----------------

        public void ShowMainWindow()
        {
            if (_window == null)
            {
                var w = new MainWindow();
                if (_restoreBounds is Rect r)
                {
                    // nach Sprachwechsel an gleicher Stelle wieder öffnen
                    w.WindowStartupLocation = WindowStartupLocation.Manual;
                    w.Left = r.Left; w.Top = r.Top; w.Width = r.Width; w.Height = r.Height;
                    _restoreBounds = null;
                }
                w.Closed += (s, a) =>
                {
                    _window = null;
                    if (_reopening) return;
                    if (!Settings.TrayHintShown)
                    {
                        Settings.TrayHintShown = true;
                        RequestSave();
                        _tray.ShowBalloonTip(3000, L.TrayHintTitle, L.TrayHintText, Forms.ToolTipIcon.Info);
                    }
                    TrimMemory();
                };
                _window = w;
                w.Show();
            }
            if (_window.WindowState == WindowState.Minimized) _window.WindowState = WindowState.Normal;
            _window.Activate();
        }

        public void ExitApp()
        {
            _window?.Close();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (Filter != null)
            {
                Filter.Dispose();
                Settings.Save(Filter);
            }
            _showWait?.Unregister(null);
            _exitWait?.Unregister(null);
            if (_tray != null) { _tray.Visible = false; _tray.Dispose(); }
            if (Filter != null) { try { _mutex?.ReleaseMutex(); } catch { } }
            _mutex?.Dispose();
            base.OnExit(e);
        }

        // ---------------- Tray ----------------

        void CreateTray()
        {
            using (var s = GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico")).Stream)
                _iconActive = new Icon(s, Forms.SystemInformation.SmallIconSize);
            _iconPaused = MakeGrayIcon(_iconActive);

            var menu = new Forms.ContextMenuStrip { RenderMode = Forms.ToolStripRenderMode.System };
            _openItem = new Forms.ToolStripMenuItem("", null, (s, a) => ShowMainWindow());
            _openItem.Font = new Font(menu.Font, System.Drawing.FontStyle.Bold);
            _pauseItem = new Forms.ToolStripMenuItem("", null, (s, a) => SetActive(!Filter.Active));
            _exitItem = new Forms.ToolStripMenuItem("", null, (s, a) => ExitApp());
            menu.Items.Add(_openItem);
            menu.Items.Add(_pauseItem);
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add(_exitItem);

            _tray = new Forms.NotifyIcon { ContextMenuStrip = menu, Visible = true };
            _tray.MouseClick += (s, a) => { if (a.Button == Forms.MouseButtons.Left) ShowMainWindow(); };
            UpdateTrayTexts();
        }

        Forms.ToolStripMenuItem _openItem, _exitItem;

        void UpdateTrayTexts()
        {
            _openItem.Text = L.TrayOpen;
            _pauseItem.Text = L.TrayPause;
            _exitItem.Text = L.TrayExit;
            UpdateTray();
        }

        void UpdateTray()
        {
            if (_tray == null) return;
            bool on = Filter.Active;
            _tray.Icon = on ? _iconActive : _iconPaused;
            _pauseItem.Checked = !on;
            string t = L.TrayTip(on, TotalBlocked);
            _tray.Text = t.Length > 63 ? t.Substring(0, 63) : t;
        }

        static Icon MakeGrayIcon(Icon src)
        {
            using (var bmp = src.ToBitmap())
            using (var gray = new Bitmap(bmp.Width, bmp.Height))
            {
                using (var g = Graphics.FromImage(gray))
                using (var attr = new ImageAttributes())
                {
                    attr.SetColorMatrix(new ColorMatrix(new[]
                    {
                        new[] { .3f, .3f, .3f, 0, 0 },
                        new[] { .59f, .59f, .59f, 0, 0 },
                        new[] { .11f, .11f, .11f, 0, 0 },
                        new[] { 0, 0, 0, .8f, 0 },
                        new[] { 0, 0, 0, 0, 1f }
                    }));
                    g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attr);
                }
                IntPtr h = gray.GetHicon();
                var icon = (Icon)Icon.FromHandle(h).Clone();
                DestroyIcon(h);
                return icon;
            }
        }

        // ---------------- Speicher ----------------

        /// <summary>Gibt nach dem Schließen des Fensters ungenutzten Speicher an Windows zurück.</summary>
        static void TrimMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, (IntPtr)(-1), (IntPtr)(-1));
        }

        [DllImport("kernel32.dll")] static extern bool SetProcessWorkingSetSize(IntPtr proc, IntPtr min, IntPtr max);
        [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);
    }
}
