using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Wpf.Ui.Controls;

namespace EasyButtonFix.Core
{
    /// <summary>
    /// Einstellungen und Laufzeitzustand für eine Maustaste.
    /// Wird vom Hook-Thread gelesen (Enabled/ThresholdMs) und von der Oberfläche gebunden.
    /// </summary>
    public sealed class ButtonState : INotifyPropertyChanged
    {
        public const int MinMs = 20, MaxMs = 300;

        public ButtonState(string key, SymbolRegular icon, bool enabled, int ms)
        {
            Key = key; Icon = icon;
            _enabled = enabled; _thresholdMs = ms;
        }

        public string Key { get; }
        public string DisplayName => L.ButtonName(Key);
        public string Description => L.ButtonDescription(Key);
        public SymbolRegular Icon { get; }

        volatile bool _enabled;
        public bool Enabled
        {
            get => _enabled;
            set { if (_enabled == value) return; _enabled = value; Changed(); Changed(nameof(Subtitle)); }
        }

        volatile int _thresholdMs;
        public int ThresholdMs
        {
            get => _thresholdMs;
            set
            {
                int v = (int)(Math.Round(value / 5.0) * 5);
                v = Math.Max(MinMs, Math.Min(MaxMs, v));
                if (_thresholdMs == v) return;
                _thresholdMs = v; Changed(); Changed(nameof(Subtitle));
            }
        }

        // Zähler wird im Hook-Thread erhöht, die Anzeige im UI-Thread aktualisiert
        internal int blocked;
        public int BlockedCount => blocked;

        public string Subtitle => !Enabled
            ? Description
            : $"{Description} · {L.BlockedSuffix(blocked)}";

        public void RefreshStats() { Changed(nameof(BlockedCount)); Changed(nameof(Subtitle)); }

        public void ResetStats() { blocked = 0; RefreshStats(); }

        // --- nur im Hook-Thread benutzt ---
        internal uint lastUpTime;
        internal bool hasUp;
        internal bool suppressNextUp;

        public event PropertyChangedEventHandler PropertyChanged;
        void Changed([CallerMemberName] string p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
