using System.Globalization;

namespace EasyButtonFix.Core
{
    /// <summary>
    /// Alle Texte der App auf Deutsch und Englisch.
    /// Sprache: "auto" (Windows-Sprache: Deutsch -> Deutsch, sonst Englisch), "de" oder "en".
    /// Neue Sprache = neue Spalte in T(...) bzw. eigene Variante – bewusst ohne .resx, damit alles an einer Stelle bleibt.
    /// </summary>
    public static class L
    {
        public static bool De { get; private set; }

        public static void Init(string setting)
        {
            switch (setting)
            {
                case "de": De = true; break;
                case "en": De = false; break;
                default: De = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de"; break;
            }
        }

        static string T(string de, string en) => De ? de : en;

        // ---------------- Tasten ----------------
        public static string ButtonName(string key)
        {
            switch (key)
            {
                case "Back": return T("Zurück-Taste", "Back button");
                case "Forward": return T("Vor-Taste", "Forward button");
                case "Left": return T("Linke Taste", "Left button");
                case "Right": return T("Rechte Taste", "Right button");
                default: return T("Mittlere Taste", "Middle button");
            }
        }

        public static string ButtonDescription(string key)
        {
            switch (key)
            {
                case "Back": return T("Seitentaste 1 (Daumen)", "Side button 1 (thumb)");
                case "Forward": return T("Seitentaste 2 (Daumen)", "Side button 2 (thumb)");
                case "Left": return T("Hauptklick", "Primary click");
                case "Right": return T("Kontextmenü", "Context menu");
                default: return T("Mausrad-Klick", "Wheel click");
            }
        }

        public static string BlockedSuffix(int n) => T($"{n:N0} blockiert", $"{n:N0} blocked");

        // ---------------- Status ----------------
        public static string ClicksBlocked => T("Klicks blockiert", "clicks blocked");
        public static string StatusOn => T("Schutz aktiv", "Protection on");
        public static string StatusPaused => T("Pausiert", "Paused");
        public static string StatusPausedText => T("Alle Klicks werden gerade unverändert durchgelassen.", "All clicks are currently passed through unchanged.");
        public static string StatusNoButton => T("Keine Taste ausgewählt – schalte unten die Taste ein, die doppelt klickt.",
                                                 "No button selected – turn on the button below that double-clicks.");
        public static string StatusWatching(int n) => De
            ? $"{n} {(n == 1 ? "Taste wird" : "Tasten werden")} überwacht. Ungewollte Doppelklicks werden automatisch blockiert."
            : $"{n} {(n == 1 ? "button is" : "buttons are")} being watched. Unwanted double clicks are blocked automatically.";

        // ---------------- Bereiche ----------------
        public static string Buttons => T("Tasten", "Buttons");
        public static string Calibrate => T("Kalibrieren", "Calibrate");
        public static string CalibrateTip => T("Misst, wie stark die Taste prellt, und schlägt eine Sperrzeit vor",
                                               "Measures how much the button bounces and suggests a block time");
        public static string SliderTip => T("Sperrzeit: so lange nach dem Loslassen werden neue Drücke ignoriert",
                                            "Block time: new presses within this time after releasing are ignored");
        public static string LiveActivity => T("Live-Aktivität", "Live activity");
        public static string GapHint => T("Abstand = Zeit seit dem letzten Loslassen", "Gap = time since the last release");
        public static string LiveEmpty => T("Drück eine Maustaste – hier siehst du live, was passiert.",
                                            "Press a mouse button – you'll see what happens here in real time.");
        public static string EventOk => "OK";
        public static string EventBlocked => T("Blockiert", "Blocked");
        public static string EventMeasuring => T("Messung", "Measuring");

        // ---------------- Einstellungen ----------------
        public static string SettingsHeader => T("Einstellungen", "Settings");
        public static string AutostartTitle => T("Mit Windows starten", "Start with Windows");
        public static string AutostartText => T("Startet unsichtbar im Tray, damit der Schutz immer aktiv ist",
                                                "Starts hidden in the tray so protection is always on");
        public static string LanguageTitle => T("Sprache", "Language");
        public static string LanguageText => T("Sprache der Oberfläche", "Language of the interface");
        public static string LanguageAuto => T("Automatisch (Windows)", "Automatic (Windows)");
        public static string ResetTitle => T("Statistik zurücksetzen", "Reset statistics");
        public static string ResetText => T("Setzt alle Zähler für blockierte Klicks auf 0", "Sets all blocked-click counters to 0");
        public static string ResetButton => T("Zurücksetzen", "Reset");
        public static string Footer(string version) => T(
            $"EasyButtonFix {version}  ·  Wenn du das Fenster schließt, läuft der Schutz im Tray weiter (unten rechts neben der Uhr).",
            $"EasyButtonFix {version}  ·  When you close this window, protection keeps running in the tray (bottom right, next to the clock).");

        // ---------------- Kalibrierung ----------------
        public static string CalTitle(string button) => T("Kalibrieren: ", "Calibrate: ") + button;
        public static string CalText(string button, int presses) => T(
            $"Drück die {button} jetzt {presses}-mal ganz normal – langsam, etwa einmal pro Sekunde. Während der Messung wird nichts blockiert.",
            $"Press the {button} {presses} times as you normally would – slowly, about once per second. Nothing is blocked during the measurement.");
        public static string CalBounces(int n) => De
            ? (n == 0 ? "noch keine Fehlauslöser" : n == 1 ? "1 Fehlauslöser erkannt" : $"{n} Fehlauslöser erkannt")
            : (n == 0 ? "no false triggers yet" : n == 1 ? "1 false trigger detected" : $"{n} false triggers detected");
        public static string CalNoBounce => T("Keine Fehlauslöser erkannt – die Taste wirkt gerade in Ordnung. Empfehlung: 50 ms als leichter Schutz.",
                                              "No false triggers detected – the button seems fine right now. Recommendation: 50 ms as light protection.");
        public static string CalResult(int count, int presses, int min, int max, int suggestion) => T(
            $"{count} Fehlauslöser bei {presses} Drücken erkannt (zwischen {min} und {max} ms). Empfehlung: {suggestion} ms.",
            $"{count} false triggers detected in {presses} presses (between {min} and {max} ms). Recommendation: {suggestion} ms.");
        public static string CalApply(int ms) => T($"{ms} ms übernehmen", $"Apply {ms} ms");
        public static string Cancel => T("Abbrechen", "Cancel");
        public static string Discard => T("Verwerfen", "Discard");

        // ---------------- Tray ----------------
        public static string TrayOpen => T("EasyButtonFix öffnen", "Open EasyButtonFix");
        public static string TrayPause => T("Pausieren", "Pause");
        public static string TrayExit => T("Beenden", "Exit");
        public static string TrayTip(bool on, int blocked) => T(
            $"EasyButtonFix – {(on ? "aktiv" : "pausiert")} · {blocked:N0} blockiert",
            $"EasyButtonFix – {(on ? "on" : "paused")} · {blocked:N0} blocked");
        public static string TrayHintTitle => T("EasyButtonFix läuft weiter", "EasyButtonFix is still running");
        public static string TrayHintText => T("Der Schutz bleibt im Hintergrund aktiv. Klick aufs Tray-Symbol, um das Fenster wieder zu öffnen.",
                                               "Protection stays active in the background. Click the tray icon to open the window again.");

        // ---------------- Fehler ----------------
        public static string HookFailed => T("Maus-Hook konnte nicht installiert werden: ", "Could not install the mouse hook: ");
    }
}
