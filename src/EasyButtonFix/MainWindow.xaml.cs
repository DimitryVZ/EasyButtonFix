using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using EasyButtonFix.Core;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace EasyButtonFix
{
    public partial class MainWindow : FluentWindow
    {
        const int MaxEvents = 8;

        static readonly Brush Green = Frozen(Color.FromRgb(34, 197, 94));
        static readonly Brush GreenSoft = Frozen(Color.FromArgb(40, 34, 197, 94));
        static readonly Brush Red = Frozen(Color.FromRgb(239, 68, 68));
        static readonly Brush RedSoft = Frozen(Color.FromArgb(40, 239, 68, 68));
        static readonly Brush Blue = Frozen(Color.FromRgb(96, 165, 250));
        static readonly Brush BlueSoft = Frozen(Color.FromArgb(40, 96, 165, 250));
        static readonly Brush ActiveBadge = FrozenGradient(Color.FromRgb(34, 197, 94), Color.FromRgb(16, 163, 74));
        static readonly Brush PausedBadge = FrozenGradient(Color.FromRgb(120, 120, 130), Color.FromRgb(82, 82, 91));

        readonly ObservableCollection<EventItem> _events = new ObservableCollection<EventItem>();
        readonly MouseFilter _filter = App.Filter;
        Calibrator _cal;
        bool _loading = true;

        public MainWindow()
        {
            ApplicationThemeManager.ApplySystemTheme();
            InitializeComponent();
            SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);

            ButtonList.ItemsSource = _filter.All;
            EventList.ItemsSource = _events;
            ActiveSwitch.IsChecked = _filter.Active;
            AutostartSwitch.IsChecked = Settings.Autostart;
            foreach (System.Windows.Controls.ComboBoxItem item in LanguageBox.Items)
                if ((string)item.Tag == Settings.Language) LanguageBox.SelectedItem = item;

            var v = Assembly.GetExecutingAssembly().GetName().Version;
            FooterText.Text = L.Footer($"{v.Major}.{v.Minor}.{v.Build}");

            foreach (var b in _filter.All) b.PropertyChanged += Button_PropertyChanged;
            App.Instance.ActiveChanged += OnActiveChanged;
            Closed += (s, e) =>
            {
                foreach (var b in _filter.All) b.PropertyChanged -= Button_PropertyChanged;
                App.Instance.ActiveChanged -= OnActiveChanged;
                _filter.Calibrating = null;
            };

            UpdateStatus();
            _loading = false;
        }

        // ---------------- Status ----------------

        void Button_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ButtonState.BlockedCount) || e.PropertyName == nameof(ButtonState.Enabled))
                UpdateStatus();
        }

        void OnActiveChanged()
        {
            _loading = true;
            ActiveSwitch.IsChecked = _filter.Active;
            _loading = false;
            UpdateStatus();
        }

        void UpdateStatus()
        {
            TotalText.Text = App.Instance.TotalBlocked.ToString("N0");
            int protectedCount = 0;
            foreach (var b in _filter.All) if (b.Enabled) protectedCount++;

            if (!_filter.Active)
            {
                StatusBadge.Background = PausedBadge;
                StatusIcon.Symbol = SymbolRegular.ShieldDismiss24;
                StatusTitle.Text = L.StatusPaused;
                StatusText.Text = L.StatusPausedText;
            }
            else
            {
                StatusBadge.Background = ActiveBadge;
                StatusIcon.Symbol = SymbolRegular.ShieldCheckmark24;
                StatusTitle.Text = L.StatusOn;
                StatusText.Text = protectedCount == 0 ? L.StatusNoButton : L.StatusWatching(protectedCount);
            }
        }

        void ActiveSwitch_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            App.Instance.SetActive(ActiveSwitch.IsChecked == true);
        }

        void Autostart_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            try { Settings.Autostart = AutostartSwitch.IsChecked == true; }
            catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "EasyButtonFix"); }
        }

        void Language_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_loading || !(LanguageBox.SelectedItem is System.Windows.Controls.ComboBoxItem item)) return;
            string lang = (string)item.Tag;
            // Fenster wird neu aufgebaut – erst nach Abschluss des aktuellen Ereignisses
            Dispatcher.BeginInvoke(new Action(() => App.Instance.SetLanguage(lang)));
        }

        void ResetStats_Click(object sender, RoutedEventArgs e)
        {
            foreach (var b in _filter.All) b.ResetStats();
            App.Instance.RequestSave();
            UpdateStatus();
        }

        // ---------------- Live-Ereignisse (vom App-Objekt im UI-Thread aufgerufen) ----------------

        internal void OnPressed(ButtonState b, bool blocked, int gap)
        {
            bool calibrating = _cal != null && _cal.Button == b && !_cal.Done;
            if (calibrating) CalibrationStep(gap);

            string status; Brush color, pill;
            if (blocked) { status = L.EventBlocked; color = Red; pill = RedSoft; }
            else if (calibrating) { status = L.EventMeasuring; color = Blue; pill = BlueSoft; }
            else { status = L.EventOk; color = Green; pill = GreenSoft; }

            _events.Insert(0, new EventItem
            {
                Name = b.DisplayName,
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Gap = gap < 0 || gap > 99999 ? "–" : gap + " ms",
                Status = status,
                Color = color,
                PillBackground = pill
            });
            while (_events.Count > MaxEvents) _events.RemoveAt(_events.Count - 1);
            EmptyHint.Visibility = Visibility.Collapsed;
        }

        // ---------------- Kalibrierung ----------------

        void Calibrate_Click(object sender, RoutedEventArgs e)
        {
            var b = (ButtonState)((FrameworkElement)sender).Tag;
            _cal = new Calibrator(b);
            _filter.Calibrating = b;

            CalTitle.Text = L.CalTitle(b.DisplayName);
            CalText.Text = L.CalText(b.DisplayName, Calibrator.TargetPresses);
            CalResultBox.Visibility = Visibility.Collapsed;
            CalApply.Visibility = Visibility.Collapsed;
            CalCancel.Content = L.Cancel;
            CalProgress.Maximum = Calibrator.TargetPresses;
            UpdateCalibrationUi();
            CalOverlay.Visibility = Visibility.Visible;
        }

        void CalibrationStep(int gap)
        {
            _cal.Add(gap);
            UpdateCalibrationUi();
            if (!_cal.Done) return;

            _filter.Calibrating = null;
            CalResult.Text = _cal.ResultText;
            CalResultBox.Visibility = Visibility.Visible;
            CalApply.Content = L.CalApply(_cal.Suggestion);
            CalApply.Visibility = Visibility.Visible;
            CalCancel.Content = L.Discard;
        }

        void UpdateCalibrationUi()
        {
            CalCount.Text = $"{_cal.RealPresses} / {Calibrator.TargetPresses}";
            CalProgress.Value = _cal.RealPresses;
            int n = _cal.Bounces.Count;
            CalBounces.Text = L.CalBounces(n);
            CalBounces.Foreground = n == 0 ? (Brush)FindResource("TextFillColorSecondaryBrush") : Red;
        }

        void CalApply_Click(object sender, RoutedEventArgs e)
        {
            _cal.Button.ThresholdMs = _cal.Suggestion;
            _cal.Button.Enabled = true;
            CloseCalibration();
        }

        void CalCancel_Click(object sender, RoutedEventArgs e) => CloseCalibration();

        void CloseCalibration()
        {
            _filter.Calibrating = null;
            _cal = null;
            CalOverlay.Visibility = Visibility.Collapsed;
        }

        // ---------------- Hilfen ----------------

        static Brush Frozen(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }

        static Brush FrozenGradient(Color a, Color b)
        {
            var g = new LinearGradientBrush(a, b, 45);
            g.Freeze();
            return g;
        }

        sealed class EventItem
        {
            public string Name { get; set; }
            public string Time { get; set; }
            public string Gap { get; set; }
            public string Status { get; set; }
            public Brush Color { get; set; }
            public Brush PillBackground { get; set; }
        }
    }
}
